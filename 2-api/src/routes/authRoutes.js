const express = require('express');
const AuthController = require('../controllers/authController');
const { autenticar } = require('../middlewares/authMiddleware');

const router = express.Router();

router.post('/cadastro', AuthController.cadastrar);
router.post('/login', AuthController.login);
router.get('/me', autenticar, AuthController.me);

module.exports = router;
