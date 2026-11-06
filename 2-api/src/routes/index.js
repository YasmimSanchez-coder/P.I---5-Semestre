const express = require('express');
const { autenticar } = require('../middlewares/authMiddleware');
const authRoutes = require('./authRoutes');
const estabelecimentoRoutes = require('./estabelecimentoRoutes');
const profissionalRoutes = require('./profissionalRoutes');
const agendamentoRoutes = require('./agendamentoRoutes');
const avaliacaoRoutes = require('./avaliacaoRoutes');
const notificacaoRoutes = require('./notificacaoRoutes');

const router = express.Router();

// Públicas: cadastro e login
router.use('/auth', authRoutes);

// Daqui em diante, todas exigem token
router.use(autenticar);
router.use('/estabelecimentos', estabelecimentoRoutes);
router.use('/profissionais', profissionalRoutes);
router.use('/agendamentos', agendamentoRoutes);
router.use('/avaliacoes', avaliacaoRoutes);
router.use('/notificacoes', notificacaoRoutes);

module.exports = router;
