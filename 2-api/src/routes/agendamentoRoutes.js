const express = require('express');
const AgendamentoController = require('../controllers/agendamentoController');
const { exigirTipo } = require('../middlewares/authMiddleware');

const router = express.Router();
const equipe = exigirTipo('profissional', 'administrador');

router.post('/', exigirTipo('cliente'), AgendamentoController.criar);
router.get('/meus', exigirTipo('cliente'), AgendamentoController.listarMeus);
router.get('/agenda', equipe, AgendamentoController.listarAgenda);

router.patch('/:id/reagendar', exigirTipo('cliente'), AgendamentoController.reagendar);
router.patch('/:id/cancelar', AgendamentoController.cancelar);
router.patch('/:id/confirmar', equipe, AgendamentoController.confirmar);
router.patch('/:id/concluir', equipe, AgendamentoController.concluir);

module.exports = router;
