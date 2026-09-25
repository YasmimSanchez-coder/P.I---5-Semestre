const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const UsuarioModel = require('../models/usuarioModel');

function gerarToken(usuario) {
  return jwt.sign(
    { id: usuario.id, tipo: usuario.tipo },
    process.env.JWT_SECRET,
    { expiresIn: process.env.JWT_EXPIRES_IN || '7d' }
  );
}

const AuthController = {
  // POST /api/auth/cadastro
  async cadastrar(req, res) {
    try {
      const { nome, email, senha, telefone, tipo } = req.body;

      if (!nome || !email || !senha) {
        return res.status(400).json({
          erro: 'Nome, email e senha são obrigatórios.',
        });
      }

      const tiposValidos = ['cliente', 'profissional', 'administrador'];
      const tipoUsuario = tiposValidos.includes(tipo) ? tipo : 'cliente';

      const usuarioExistente = await UsuarioModel.buscarPorEmail(email);
      if (usuarioExistente) {
        return res.status(409).json({ erro: 'Este email já está cadastrado.' });
      }

      const senhaHash = await bcrypt.hash(senha, 10);

      const novoId = await UsuarioModel.criar({
        nome,
        email,
        senhaHash,
        telefone,
        tipo: tipoUsuario,
      });

      const usuario = await UsuarioModel.buscarPorId(novoId);
      const token = gerarToken(usuario);

      return res.status(201).json({ usuario, token });
    } catch (erro) {
      console.error('Erro ao cadastrar usuário:', erro);
      return res.status(500).json({ erro: 'Erro interno ao cadastrar usuário.' });
    }
  },

  // POST /api/auth/login
  async login(req, res) {
    try {
      const { email, senha } = req.body;

      if (!email || !senha) {
        return res.status(400).json({ erro: 'Email e senha são obrigatórios.' });
      }

      const usuario = await UsuarioModel.buscarPorEmail(email);
      if (!usuario) {
        return res.status(401).json({ erro: 'Email ou senha inválidos.' });
      }

      const senhaValida = await bcrypt.compare(senha, usuario.senha_hash);
      if (!senhaValida) {
        return res.status(401).json({ erro: 'Email ou senha inválidos.' });
      }

      const token = gerarToken(usuario);
      delete usuario.senha_hash;

      return res.status(200).json({ usuario, token });
    } catch (erro) {
      console.error('Erro ao fazer login:', erro);
      return res.status(500).json({ erro: 'Erro interno ao fazer login.' });
    }
  },

  // GET /api/auth/me (rota protegida - retorna o usuário autenticado)
  async me(req, res) {
    try {
      const usuario = await UsuarioModel.buscarPorId(req.usuarioId);
      if (!usuario) {
        return res.status(404).json({ erro: 'Usuário não encontrado.' });
      }
      return res.status(200).json({ usuario });
    } catch (erro) {
      console.error('Erro ao buscar usuário autenticado:', erro);
      return res.status(500).json({ erro: 'Erro interno.' });
    }
  },
};

module.exports = AuthController;
