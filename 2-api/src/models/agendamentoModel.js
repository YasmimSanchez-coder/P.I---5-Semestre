const db = require('../config/database');

/** Agendamento ativo (pendente/confirmado) que ainda não terminou. */
const FUTURO = "(a.status IN ('pendente', 'confirmado') AND TIMESTAMP(a.data, a.hora_fim) >= NOW())";

const FILTRO_PERIODO = {
  proximos: FUTURO,
  historico: `NOT ${FUTURO}`,
  todos: '1 = 1',
};

const ORDEM_PERIODO = {
  proximos: 'a.data ASC, a.hora_inicio ASC',
  historico: 'a.data DESC, a.hora_inicio DESC',
  // próximos primeiro (do mais perto ao mais distante), depois o histórico (do mais recente ao mais antigo)
  todos: `${FUTURO} DESC,
          CASE WHEN ${FUTURO} THEN TIMESTAMP(a.data, a.hora_inicio) END ASC,
          CASE WHEN NOT ${FUTURO} THEN TIMESTAMP(a.data, a.hora_inicio) END DESC`,
};

const CAMPOS_LISTAGEM = `
  a.id, a.data,
  DATE_FORMAT(a.hora_inicio, '%H:%i') AS horaInicio,
  DATE_FORMAT(a.hora_fim, '%H:%i') AS horaFim,
  a.status, s.nome AS servicoNome, s.preco AS servicoPreco`;

const AgendamentoModel = {
  /** Agendamentos do cliente. periodo: "proximos", "historico" ou "todos". */
  async listarDoCliente(clienteId, periodo) {
    const [linhas] = await db.query(
      `SELECT ${CAMPOS_LISTAGEM},
              a.servico_id AS servicoId,
              p.id AS profissionalId, u.nome AS profissionalNome,
              e.id AS estabelecimentoId, e.nome AS estabelecimentoNome,
              EXISTS (SELECT 1 FROM avaliacoes av WHERE av.agendamento_id = a.id) AS avaliado
       FROM agendamentos a
       JOIN servicos s ON s.id = a.servico_id
       JOIN profissionais p ON p.id = a.profissional_id
       JOIN usuarios u ON u.id = p.usuario_id
       JOIN estabelecimentos e ON e.id = p.estabelecimento_id
       WHERE a.cliente_id = ? AND ${FILTRO_PERIODO[periodo]}
       ORDER BY ${ORDEM_PERIODO[periodo]}`,
      [clienteId]
    );

    return linhas.map((l) => ({
      ...l,
      servicoPreco: Number(l.servicoPreco),
      avaliado: Boolean(l.avaliado),
    }));
  },

  /** Agenda de um dia para o profissional (ou administrador dono do estabelecimento). */
  async listarAgenda(usuarioId, data) {
    const [linhas] = await db.query(
      `SELECT ${CAMPOS_LISTAGEM},
              uc.nome AS clienteNome, uc.telefone AS clienteTelefone,
              up.nome AS profissionalNome
       FROM agendamentos a
       JOIN servicos s ON s.id = a.servico_id
       JOIN profissionais p ON p.id = a.profissional_id
       JOIN estabelecimentos e ON e.id = p.estabelecimento_id
       JOIN usuarios uc ON uc.id = a.cliente_id
       JOIN usuarios up ON up.id = p.usuario_id
       WHERE (p.usuario_id = ? OR e.administrador_id = ?) AND a.data = ?
       ORDER BY a.hora_inicio`,
      [usuarioId, usuarioId, data]
    );
    return linhas.map((l) => ({ ...l, servicoPreco: Number(l.servicoPreco) }));
  },

  /** Tudo o que os controllers precisam para validar permissões e notificar. */
  async buscarContexto(id) {
    const [linhas] = await db.query(
      `SELECT a.id, a.cliente_id AS clienteId, a.status, a.data,
              DATE_FORMAT(a.hora_inicio, '%H:%i') AS horaInicio,
              s.nome AS servicoNome,
              p.id AS profissionalId, p.usuario_id AS profissionalUsuarioId,
              e.id AS estabelecimentoId, e.administrador_id AS administradorId,
              uc.nome AS clienteNome
       FROM agendamentos a
       JOIN servicos s ON s.id = a.servico_id
       JOIN profissionais p ON p.id = a.profissional_id
       JOIN estabelecimentos e ON e.id = p.estabelecimento_id
       JOIN usuarios uc ON uc.id = a.cliente_id
       WHERE a.id = ?`,
      [id]
    );
    return linhas[0] || null;
  },

  async atualizarStatus(id, status) {
    await db.query('UPDATE agendamentos SET status = ? WHERE id = ?', [status, id]);
  },
};

module.exports = AgendamentoModel;
