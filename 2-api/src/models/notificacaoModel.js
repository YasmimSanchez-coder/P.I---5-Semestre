const db = require('../config/database');

const NotificacaoModel = {
  async criar({ usuarioId, agendamentoId, tipo = 'agendamento', titulo, mensagem }, conexao = db) {
    await conexao.query(
      'INSERT INTO notificacoes (usuario_id, agendamento_id, tipo, titulo, mensagem) VALUES (?, ?, ?, ?, ?)',
      [usuarioId, agendamentoId, tipo, titulo, mensagem]
    );
  },

  async listar(usuarioId, tipo) {
    const filtroTipo = tipo ? 'AND tipo = ?' : '';
    const parametros = tipo ? [usuarioId, tipo] : [usuarioId];

    const [linhas] = await db.query(
      `SELECT id, tipo, titulo, mensagem, lida, criado_em AS criadoEm
       FROM notificacoes
       WHERE usuario_id = ? ${filtroTipo}
       ORDER BY criado_em DESC, id DESC
       LIMIT 100`,
      parametros
    );
    return linhas.map((n) => ({ ...n, lida: Boolean(n.lida) }));
  },

  async contarNaoLidas(usuarioId) {
    const [linhas] = await db.query(
      'SELECT COUNT(*) AS total FROM notificacoes WHERE usuario_id = ? AND lida = FALSE',
      [usuarioId]
    );
    return Number(linhas[0].total);
  },

  async marcarComoLida(id, usuarioId) {
    const [resultado] = await db.query(
      'UPDATE notificacoes SET lida = TRUE WHERE id = ? AND usuario_id = ?',
      [id, usuarioId]
    );
    return resultado.affectedRows > 0;
  },

  async marcarTodasComoLidas(usuarioId) {
    await db.query('UPDATE notificacoes SET lida = TRUE WHERE usuario_id = ? AND lida = FALSE', [usuarioId]);
  },
};

module.exports = NotificacaoModel;
