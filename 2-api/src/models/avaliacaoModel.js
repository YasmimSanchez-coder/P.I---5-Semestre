const db = require('../config/database');

const AvaliacaoModel = {
  async existeParaAgendamento(agendamentoId) {
    const [linhas] = await db.query('SELECT 1 FROM avaliacoes WHERE agendamento_id = ? LIMIT 1', [agendamentoId]);
    return linhas.length > 0;
  },

  async criar({ agendamentoId, clienteId, estabelecimentoId, profissionalId, nota, comentario }) {
    const [resultado] = await db.query(
      `INSERT INTO avaliacoes (agendamento_id, cliente_id, estabelecimento_id, profissional_id, nota, comentario)
       VALUES (?, ?, ?, ?, ?, ?)`,
      [agendamentoId, clienteId, estabelecimentoId, profissionalId, nota, comentario]
    );
    return resultado.insertId;
  },

  async listarPorEstabelecimento(estabelecimentoId) {
    const [linhas] = await db.query(
      `SELECT av.id, av.nota, av.comentario, av.criado_em AS criadoEm, u.nome AS clienteNome
       FROM avaliacoes av
       JOIN usuarios u ON u.id = av.cliente_id
       WHERE av.estabelecimento_id = ?
       ORDER BY av.criado_em DESC`,
      [estabelecimentoId]
    );
    return linhas;
  },
};

module.exports = AvaliacaoModel;
