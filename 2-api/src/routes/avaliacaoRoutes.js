const express = require('express');
const AvaliacaoController = require('../controllers/avaliacaoController');
const { exigirTipo } = require('../middlewares/authMiddleware');

const router = express.Router();

router.post('/', exigirTipo('cliente'), AvaliacaoController.criar);

module.exports = router;
