const express = require('express');
const NotificacaoController = require('../controllers/notificacaoController');

const router = express.Router();

router.get('/', NotificacaoController.listar);
router.patch('/marcar-todas', NotificacaoController.marcarTodasComoLidas);
router.patch('/:id/lida', NotificacaoController.marcarComoLida);

module.exports = router;
