const EstabelecimentoModel = require('../models/estabelecimentoModel');
const ProfissionalModel = require('../models/profissionalModel');
const ErroHttp = require('../utils/ErroHttp');
const asyncHandler = require('../utils/asyncHandler');
const {
  exigirTexto, textoOpcional, exigirInteiro, exigirNumero, numeroOpcional, exigirHora,
} = require('../utils/validacao');

/** GET /estabelecimentos?busca=&categoria=&latitude=&longitude= */
const listar = asyncHandler(async (req, res) => {
  const busca = textoOpcional(req.query.busca);
  const latitude = numeroOpcional(req.query.latitude, 'Latitude inválida.');
  const longitude = numeroOpcional(req.query.longitude, 'Longitude inválida.');
  const temLocalizacao = latitude !== null && longitude !== null;

  res.json(
    await EstabelecimentoModel.listar({
      busca,
      categoria: textoOpcional(req.query.categoria),
      latitude: temLocalizacao ? latitude : null,
      longitude: temLocalizacao ? longitude : null,
    })
  );
});

/** GET /estabelecimentos/:id - dados, serviços e profissionais */
const obter = asyncHandler(async (req, res) => {
  const id = exigirInteiro(req.params.id, 'Estabelecimento inválido.');

  const estabelecimento = await EstabelecimentoModel.buscarPorId(id);
  if (!estabelecimento) throw new ErroHttp(404, 'Estabelecimento não encontrado.');

  const [servicos, profissionais] = await Promise.all([
    EstabelecimentoModel.listarServicos(id),
    ProfissionalModel.listarPorEstabelecimento(id),
  ]);

  res.json({ ...estabelecimento, servicos, profissionais });
});

/** Garante que o estabelecimento existe e pertence ao administrador logado. */
async function exigirDono(estabelecimentoId, usuarioId) {
  const dono = await EstabelecimentoModel.buscarDono(estabelecimentoId);
  if (!dono) throw new ErroHttp(404, 'Estabelecimento não encontrado.');
  if (dono.administradorId !== usuarioId) {
    throw new ErroHttp(403, 'Este estabelecimento pertence a outro administrador.');
  }
}

/** POST /estabelecimentos (administrador) */
const criar = asyncHandler(async (req, res) => {
  const corpo = req.body;
  const id = await EstabelecimentoModel.criar({
    administradorId: req.usuarioId,
    nome: exigirTexto(corpo.nome, 'Informe o nome do estabelecimento.'),
    descricao: textoOpcional(corpo.descricao),
    endereco: textoOpcional(corpo.endereco),
    latitude: numeroOpcional(corpo.latitude, 'Latitude inválida.'),
    longitude: numeroOpcional(corpo.longitude, 'Longitude inválida.'),
    telefone: textoOpcional(corpo.telefone),
    whatsapp: textoOpcional(corpo.whatsapp),
    horarioAbertura: corpo.horarioAbertura ? exigirHora(corpo.horarioAbertura) : null,
    horarioFechamento: corpo.horarioFechamento ? exigirHora(corpo.horarioFechamento) : null,
  });
  res.status(201).json({ id, mensagem: 'Estabelecimento criado.' });
});

/** POST /estabelecimentos/:id/servicos (administrador dono) */
const criarServico = asyncHandler(async (req, res) => {
  const estabelecimentoId = exigirInteiro(req.params.id, 'Estabelecimento inválido.');
  await exigirDono(estabelecimentoId, req.usuarioId);

  const id = await EstabelecimentoModel.criarServico({
    estabelecimentoId,
    nome: exigirTexto(req.body.nome, 'Informe o nome do serviço.'),
    descricao: textoOpcional(req.body.descricao),
    categoria: textoOpcional(req.body.categoria) || 'Outros',
    preco: exigirNumero(req.body.preco, 'Preço inválido.'),
    duracaoMin: exigirInteiro(req.body.duracaoMin, 'Informe a duração em minutos.'),
  });
  res.status(201).json({ id, mensagem: 'Serviço criado.' });
});

module.exports = { listar, obter, criar, criarServico, exigirDono };
