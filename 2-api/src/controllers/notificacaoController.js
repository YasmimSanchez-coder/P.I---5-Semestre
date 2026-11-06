const NotificacaoModel = require('../models/notificacaoModel');
const ErroHttp = require('../utils/ErroHttp');
const asyncHandler = require('../utils/asyncHandler');
const { exigirInteiro } = require('../utils/validacao');

const TIPOS = ['agendamento', 'lembrete', 'promocao'];

/** GET /notificacoes?tipo=agendamento|lembrete|promocao */
const listar = asyncHandler(async (req, res) => {
  const tipo = TIPOS.includes(req.query.tipo) ? req.query.tipo : null;

  const [itens, naoLidas] = await Promise.all([
    NotificacaoModel.listar(req.usuarioId, tipo),
    NotificacaoModel.contarNaoLidas(req.usuarioId),
  ]);

  res.json({ naoLidas, itens });
});

/** PATCH /notificacoes/:id/lida */
const marcarComoLida = asyncHandler(async (req, res) => {
  const id = exigirInteiro(req.params.id, 'Notificação inválida.');
  if (!(await NotificacaoModel.marcarComoLida(id, req.usuarioId))) {
    throw new ErroHttp(404, 'Notificação não encontrada.');
  }
  res.json({ mensagem: 'Notificação marcada como lida.' });
});

/** PATCH /notificacoes/marcar-todas */
const marcarTodasComoLidas = asyncHandler(async (req, res) => {
  await NotificacaoModel.marcarTodasComoLidas(req.usuarioId);
  res.json({ mensagem: 'Todas as notificações foram marcadas como lidas.' });
});

module.exports = { listar, marcarComoLida, marcarTodasComoLidas };
