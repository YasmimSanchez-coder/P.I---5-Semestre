const express = require('express');
const EstabelecimentoController = require('../controllers/estabelecimentoController');
const ProfissionalController = require('../controllers/profissionalController');
const AvaliacaoController = require('../controllers/avaliacaoController');
const { exigirTipo } = require('../middlewares/authMiddleware');

const router = express.Router();
const apenasAdmin = exigirTipo('administrador');

router.get('/', EstabelecimentoController.listar);
router.get('/:id', EstabelecimentoController.obter);
router.get('/:id/avaliacoes', AvaliacaoController.listarPorEstabelecimento);

router.post('/', apenasAdmin, EstabelecimentoController.criar);
router.post('/:id/servicos', apenasAdmin, EstabelecimentoController.criarServico);
router.post('/:id/profissionais', apenasAdmin, ProfissionalController.criar);

module.exports = router;
