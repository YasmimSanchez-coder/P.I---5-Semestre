const db = require('../config/database');

const ProfissionalModel = {
  async listarPorEstabelecimento(estabelecimentoId) {
    const [profissionais] = await db.query(
      `SELECT p.id, u.nome, p.especialidade, u.foto_url AS fotoUrl,
              COALESCE(ROUND(AVG(av.nota), 1), 0) AS notaMedia
       FROM profissionais p
       JOIN usuarios u ON u.id = p.usuario_id
       LEFT JOIN avaliacoes av ON av.profissional_id = p.id
       WHERE p.estabelecimento_id = ?
       GROUP BY p.id, u.nome, p.especialidade, u.foto_url
       ORDER BY u.nome`,
      [estabelecimentoId]
    );

    const [vinculos] = await db.query(
      `SELECT ps.profissional_id AS profissionalId, ps.servico_id AS servicoId
       FROM profissional_servicos ps
       JOIN profissionais p ON p.id = ps.profissional_id
       WHERE p.estabelecimento_id = ?`,
      [estabelecimentoId]
    );

    return profissionais.map((p) => ({
      ...p,
      notaMedia: Number(p.notaMedia),
      servicosIds: vinculos.filter((v) => v.profissionalId === p.id).map((v) => v.servicoId),
    }));
  },

  /** Dados para checar permissões sobre um profissional. */
  async buscarContexto(id) {
    const [linhas] = await db.query(
      `SELECT p.id, p.usuario_id AS usuarioId, p.estabelecimento_id AS estabelecimentoId,
              e.administrador_id AS administradorId
       FROM profissionais p
       JOIN estabelecimentos e ON e.id = p.estabelecimento_id
       WHERE p.id = ?`,
      [id]
    );
    return linhas[0] || null;
  },

  /** Serviço ativo, desde que o profissional o realize. */
  async buscarServicoDoProfissional(profissionalId, servicoId) {
    const [linhas] = await db.query(
      `SELECT s.id, s.nome, s.duracao_minutos AS duracaoMin
       FROM servicos s
       JOIN profissional_servicos ps ON ps.servico_id = s.id
       WHERE s.id = ? AND ps.profissional_id = ? AND s.ativo = TRUE`,
      [servicoId, profissionalId]
    );
    return linhas[0] || null;
  },

  /** Cria usuário + profissional + serviços + expediente padrão (seg-sáb) numa transação. */
  async criarCompleto({ estabelecimentoId, usuario, especialidade, servicosIds }, criarUsuario) {
    const conexao = await db.getConnection();
    try {
      await conexao.beginTransaction();

      const usuarioId = await criarUsuario(usuario, conexao);

      const [resultado] = await conexao.query(
        'INSERT INTO profissionais (usuario_id, estabelecimento_id, especialidade) VALUES (?, ?, ?)',
        [usuarioId, estabelecimentoId, especialidade]
      );
      const profissionalId = resultado.insertId;

      for (const servicoId of servicosIds) {
        await conexao.query(
          'INSERT INTO profissional_servicos (profissional_id, servico_id) VALUES (?, ?)',
          [profissionalId, servicoId]
        );
      }

      for (let dia = 1; dia <= 6; dia += 1) {
        await conexao.query(
          `INSERT INTO horarios_disponiveis (profissional_id, dia_semana, hora_inicio, hora_fim)
           VALUES (?, ?, '09:00:00', '12:00:00'), (?, ?, '13:00:00', '18:00:00')`,
          [profissionalId, dia, profissionalId, dia]
        );
      }

      await conexao.commit();
      return profissionalId;
    } catch (erro) {
      await conexao.rollback();
      throw erro;
    } finally {
      conexao.release();
    }
  },

  async contarServicosDoEstabelecimento(estabelecimentoId, servicosIds) {
    if (servicosIds.length === 0) return 0;
    const [linhas] = await db.query(
      'SELECT COUNT(*) AS total FROM servicos WHERE estabelecimento_id = ? AND id IN (?)',
      [estabelecimentoId, servicosIds]
    );
    return linhas[0].total;
  },

  /** Substitui todo o expediente semanal do profissional. */
  async substituirHorarios(profissionalId, horarios) {
    const conexao = await db.getConnection();
    try {
      await conexao.beginTransaction();
      await conexao.query('DELETE FROM horarios_disponiveis WHERE profissional_id = ?', [profissionalId]);
      for (const h of horarios) {
        await conexao.query(
          `INSERT INTO horarios_disponiveis (profissional_id, dia_semana, hora_inicio, hora_fim)
           VALUES (?, ?, ?, ?)`,
          [profissionalId, h.diaSemana, h.horaInicio, h.horaFim]
        );
      }
      await conexao.commit();
    } catch (erro) {
      await conexao.rollback();
      throw erro;
    } finally {
      conexao.release();
    }
  },

  /** Profissionais que o usuário representa: ele mesmo (profissional) ou todos do seu estabelecimento (administrador). */
  async listarDoUsuario(usuarioId) {
    const [linhas] = await db.query(
      `SELECT p.id, u.nome, p.especialidade, e.nome AS estabelecimentoNome
       FROM profissionais p
       JOIN usuarios u ON u.id = p.usuario_id
       JOIN estabelecimentos e ON e.id = p.estabelecimento_id
       WHERE p.usuario_id = ? OR e.administrador_id = ?
       ORDER BY u.nome`,
      [usuarioId, usuarioId]
    );
    return linhas;
  },

  /** Janelas de expediente do dia da semana, somando todos os profissionais do usuário. */
  async listarExpedienteDoDia(usuarioId, diaSemana) {
    const [linhas] = await db.query(
      `SELECT TIME_FORMAT(h.hora_inicio, '%H:%i') AS horaInicio, TIME_FORMAT(h.hora_fim, '%H:%i') AS horaFim
       FROM horarios_disponiveis h
       JOIN profissionais p ON p.id = h.profissional_id
       JOIN estabelecimentos e ON e.id = p.estabelecimento_id
       WHERE (p.usuario_id = ? OR e.administrador_id = ?) AND h.dia_semana = ?`,
      [usuarioId, usuarioId, diaSemana]
    );
    return linhas;
  },

  async listarBloqueiosDoDia(usuarioId, data) {
    const [linhas] = await db.query(
      `SELECT b.id, DATE_FORMAT(b.hora_inicio, '%H:%i') AS horaInicio,
              DATE_FORMAT(b.hora_fim, '%H:%i') AS horaFim,
              b.motivo, u.nome AS profissionalNome
       FROM bloqueios_agenda b
       JOIN profissionais p ON p.id = b.profissional_id
       JOIN usuarios u ON u.id = p.usuario_id
       JOIN estabelecimentos e ON e.id = p.estabelecimento_id
       WHERE (p.usuario_id = ? OR e.administrador_id = ?) AND b.data = ?
       ORDER BY b.hora_inicio`,
      [usuarioId, usuarioId, data]
    );
    return linhas;
  },

  async buscarBloqueio(id) {
    const [linhas] = await db.query(
      `SELECT b.id, p.usuario_id AS usuarioId, e.administrador_id AS administradorId
       FROM bloqueios_agenda b
       JOIN profissionais p ON p.id = b.profissional_id
       JOIN estabelecimentos e ON e.id = p.estabelecimento_id
       WHERE b.id = ?`,
      [id]
    );
    return linhas[0] || null;
  },

  async excluirBloqueio(id) {
    await db.query('DELETE FROM bloqueios_agenda WHERE id = ?', [id]);
  },

  /** Há agendamento ativo que se sobrepõe ao intervalo? (impede bloquear um horário já reservado) */
  async existeAgendamentoNoIntervalo({ profissionalId, data, horaInicio, horaFim }) {
    const [linhas] = await db.query(
      `SELECT 1 FROM agendamentos
       WHERE profissional_id = ? AND data = ? AND status IN ('pendente', 'confirmado')
         AND hora_inicio < ? AND hora_fim > ?
       LIMIT 1`,
      [profissionalId, data, horaFim, horaInicio]
    );
    return linhas.length > 0;
  },

  async criarBloqueio({ profissionalId, data, horaInicio, horaFim, motivo }) {
    const [resultado] = await db.query(
      `INSERT INTO bloqueios_agenda (profissional_id, data, hora_inicio, hora_fim, motivo)
       VALUES (?, ?, ?, ?, ?)`,
      [profissionalId, data, horaInicio, horaFim, motivo]
    );
    return resultado.insertId;
  },
};

module.exports = ProfissionalModel;
