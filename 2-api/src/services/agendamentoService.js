const db = require('../config/database');
const ErroHttp = require('../utils/ErroHttp');
const NotificacaoModel = require('../models/notificacaoModel');
const {
  paraMinutos,
  paraHHMM,
  diaDaSemana,
  formatarDataBR,
  minutoMinimoParaData,
  gerarHorarios,
} = require('../utils/horarios');

/**
 * Horários de início livres de um profissional em uma data.
 * Considera expediente, agendamentos ativos, bloqueios e horários que já passaram.
 * `conexao` pode ser o pool ou uma conexão de transação.
 * `ignorarAgendamentoId` desconsidera um agendamento (o que está sendo reagendado).
 */
async function horariosDisponiveis(conexao, { profissionalId, duracaoMin, data, ignorarAgendamentoId = 0 }) {
  const minutoMinimo = minutoMinimoParaData(data);
  if (minutoMinimo === Infinity) return [];

  const [janelas] = await conexao.query(
    `SELECT hora_inicio AS horaInicio, hora_fim AS horaFim
     FROM horarios_disponiveis WHERE profissional_id = ? AND dia_semana = ?`,
    [profissionalId, diaDaSemana(data)]
  );

  const [agendados] = await conexao.query(
    `SELECT hora_inicio AS horaInicio, hora_fim AS horaFim
     FROM agendamentos
     WHERE profissional_id = ? AND data = ? AND status IN ('pendente', 'confirmado') AND id <> ?`,
    [profissionalId, data, ignorarAgendamentoId]
  );

  const [bloqueios] = await conexao.query(
    `SELECT hora_inicio AS horaInicio, hora_fim AS horaFim
     FROM bloqueios_agenda WHERE profissional_id = ? AND data = ?`,
    [profissionalId, data]
  );

  return gerarHorarios({
    janelas,
    ocupados: [...agendados, ...bloqueios],
    duracaoMin,
    minutoMinimo,
  });
}

/**
 * Cria um agendamento sem permitir conflito de horário.
 * A linha do profissional é travada (FOR UPDATE) durante a transação, então duas
 * requisições simultâneas para o mesmo profissional são processadas uma por vez
 * e a segunda enxerga o horário já ocupado.
 */
async function criarAgendamento({ clienteId, profissionalId, servicoId, data, horaInicio }) {
  const conexao = await db.getConnection();

  try {
    await conexao.beginTransaction();

    const [[profissional]] = await conexao.query(
      'SELECT id, usuario_id AS usuarioId FROM profissionais WHERE id = ? FOR UPDATE',
      [profissionalId]
    );
    if (!profissional) throw new ErroHttp(404, 'Profissional não encontrado.');

    const [[servico]] = await conexao.query(
      `SELECT s.id, s.nome, s.duracao_minutos AS duracaoMin
       FROM servicos s
       JOIN profissional_servicos ps ON ps.servico_id = s.id
       WHERE s.id = ? AND ps.profissional_id = ? AND s.ativo = TRUE`,
      [servicoId, profissionalId]
    );
    if (!servico) throw new ErroHttp(400, 'Este profissional não realiza o serviço escolhido.');

    const livres = await horariosDisponiveis(conexao, {
      profissionalId,
      duracaoMin: servico.duracaoMin,
      data,
    });
    if (!livres.includes(horaInicio)) {
      throw new ErroHttp(409, 'Este horário não está mais disponível. Escolha outro.');
    }

    const horaFim = paraHHMM(paraMinutos(horaInicio) + servico.duracaoMin);

    const [resultado] = await conexao.query(
      `INSERT INTO agendamentos (cliente_id, profissional_id, servico_id, data, hora_inicio, hora_fim, status)
       VALUES (?, ?, ?, ?, ?, ?, 'pendente')`,
      [clienteId, profissionalId, servicoId, data, horaInicio, horaFim]
    );

    const [[cliente]] = await conexao.query('SELECT nome FROM usuarios WHERE id = ?', [clienteId]);
    await NotificacaoModel.criar(
      {
        usuarioId: profissional.usuarioId,
        agendamentoId: resultado.insertId,
        titulo: 'Novo agendamento',
        mensagem: `${cliente.nome} agendou ${servico.nome} para ${formatarDataBR(data)} às ${horaInicio}. Confirme o atendimento.`,
      },
      conexao
    );

    await conexao.commit();
    return resultado.insertId;
  } catch (erro) {
    await conexao.rollback();
    throw erro;
  } finally {
    conexao.release();
  }
}

/**
 * Muda data/horário de um agendamento do cliente, com as mesmas garantias da criação:
 * trava o profissional, recalcula os horários livres (ignorando o próprio agendamento)
 * e volta o status para "pendente" para o profissional confirmar de novo.
 */
async function reagendarAgendamento({ agendamentoId, clienteId, data, horaInicio }) {
  const conexao = await db.getConnection();

  try {
    await conexao.beginTransaction();

    const [[previo]] = await conexao.query(
      'SELECT profissional_id AS profissionalId FROM agendamentos WHERE id = ? AND cliente_id = ?',
      [agendamentoId, clienteId]
    );
    if (!previo) throw new ErroHttp(404, 'Agendamento não encontrado.');

    // Mesma ordem de travas da criação (profissional primeiro) para evitar deadlock
    const [[profissional]] = await conexao.query(
      'SELECT id, usuario_id AS usuarioId FROM profissionais WHERE id = ? FOR UPDATE',
      [previo.profissionalId]
    );

    const [[agendamento]] = await conexao.query(
      `SELECT a.id, a.status, s.nome AS servicoNome, s.duracao_minutos AS duracaoMin
       FROM agendamentos a
       JOIN servicos s ON s.id = a.servico_id
       WHERE a.id = ? FOR UPDATE`,
      [agendamentoId]
    );
    if (!['pendente', 'confirmado'].includes(agendamento.status)) {
      throw new ErroHttp(409, 'Este agendamento não pode ser reagendado.');
    }

    const livres = await horariosDisponiveis(conexao, {
      profissionalId: profissional.id,
      duracaoMin: agendamento.duracaoMin,
      data,
      ignorarAgendamentoId: agendamentoId,
    });
    if (!livres.includes(horaInicio)) {
      throw new ErroHttp(409, 'Este horário não está mais disponível. Escolha outro.');
    }

    const horaFim = paraHHMM(paraMinutos(horaInicio) + agendamento.duracaoMin);
    await conexao.query(
      `UPDATE agendamentos SET data = ?, hora_inicio = ?, hora_fim = ?, status = 'pendente' WHERE id = ?`,
      [data, horaInicio, horaFim, agendamentoId]
    );

    const [[cliente]] = await conexao.query('SELECT nome FROM usuarios WHERE id = ?', [clienteId]);
    await NotificacaoModel.criar(
      {
        usuarioId: profissional.usuarioId,
        agendamentoId,
        titulo: 'Agendamento reagendado',
        mensagem: `${cliente.nome} reagendou ${agendamento.servicoNome} para ${formatarDataBR(data)} às ${horaInicio}. Confirme o atendimento.`,
      },
      conexao
    );

    await conexao.commit();
  } catch (erro) {
    await conexao.rollback();
    throw erro;
  } finally {
    conexao.release();
  }
}

module.exports = { horariosDisponiveis, criarAgendamento, reagendarAgendamento };
