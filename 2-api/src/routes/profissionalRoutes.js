const express = require('express');
const ProfissionalController = require('../controllers/profissionalController');
const { exigirTipo } = require('../middlewares/authMiddleware');

const router = express.Router();
const equipe = exigirTipo('profissional', 'administrador');

router.get('/meus', equipe, ProfissionalController.meus);
router.get('/:id/horarios-disponiveis', ProfissionalController.horariosDisponiveis);
router.put('/:id/horarios', equipe, ProfissionalController.definirHorarios);
router.post('/:id/bloqueios', equipe, ProfissionalController.criarBloqueio);
router.delete('/bloqueios/:id', equipe, ProfissionalController.excluirBloqueio);

module.exports = router;
