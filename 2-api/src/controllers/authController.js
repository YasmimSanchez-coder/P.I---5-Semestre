const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const UsuarioModel = require('../models/usuarioModel');
const ErroHttp = require('../utils/ErroHttp');
const asyncHandler = require('../utils/asyncHandler');
const { exigirTexto, exigirEmail, textoOpcional } = require('../utils/validacao');

function gerarToken(usuario) {
  return jwt.sign({ id: usuario.id, tipo: usuario.tipo }, process.env.JWT_SECRET, {
    expiresIn: process.env.JWT_EXPIRES_IN || '7d',
  });
}

/** Cadastro público: sempre cria um cliente. Profissionais e administradores são criados pela API autenticada. */
const cadastrar = asyncHandler(async (req, res) => {
  const nome = exigirTexto(req.body.nome, 'Informe o nome.');
  const email = exigirEmail(req.body.email);
  const senha = exigirTexto(req.body.senha, 'Informe a senha.');
  if (senha.length < 6) throw new ErroHttp(400, 'A senha deve ter pelo menos 6 caracteres.');

  if (await UsuarioModel.buscarPorEmail(email)) {
    throw new ErroHttp(409, 'Este email já está cadastrado.');
  }

  const id = await UsuarioModel.criar({
    nome,
    email,
    senhaHash: await bcrypt.hash(senha, 10),
    telefone: textoOpcional(req.body.telefone),
    tipo: 'cliente',
  });

  const usuario = await UsuarioModel.buscarPorId(id);
  res.status(201).json({ usuario, token: gerarToken(usuario) });
});

const login = asyncHandler(async (req, res) => {
  const email = exigirEmail(req.body.email);
  const senha = exigirTexto(req.body.senha, 'Informe a senha.');

  const encontrado = await UsuarioModel.buscarPorEmail(email);
  const senhaCorreta = encontrado && (await bcrypt.compare(senha, encontrado.senhaHash));
  if (!senhaCorreta) throw new ErroHttp(401, 'Email ou senha inválidos.');

  const { senhaHash, ...usuario } = encontrado;
  res.json({ usuario, token: gerarToken(usuario) });
});

const me = asyncHandler(async (req, res) => {
  const usuario = await UsuarioModel.buscarPorId(req.usuarioId);
  if (!usuario) throw new ErroHttp(404, 'Usuário não encontrado.');
  res.json({ usuario });
});

module.exports = { cadastrar, login, me };
