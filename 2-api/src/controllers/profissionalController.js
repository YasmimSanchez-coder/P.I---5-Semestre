const bcrypt = require('bcryptjs');
const db = require('../config/database');
const UsuarioModel = require('../models/usuarioModel');
const AgendamentoModel = require('../models/agendamentoModel');
const ProfissionalModel = require('../models/profissionalModel');
const AgendamentoService = require('../services/agendamentoService');
const ErroHttp = require('../utils/ErroHttp');
const asyncHandler = require('../utils/asyncHandler');
const { paraMinutos } = require('../utils/horarios');
const {
  exigirTexto, textoOpcional, exigirEmail, exigirInteiro, exigirHora, exigirData,
} = require('../utils/validacao');
const { exigirDono } = require('./estabelecimentoController');

/** O próprio profissional ou o administrador dono do estabelecimento. */
async function exigirPermissaoSobreProfissional(profissionalId, usuarioId) {
  const contexto = await ProfissionalModel.buscarContexto(profissionalId);
  if (!contexto) throw new ErroHttp(404, 'Profissional não encontrado.');
  if (contexto.usuarioId !== usuarioId && contexto.administradorId !== usuarioId) {
    throw new ErroHttp(403, 'Você não pode alterar a agenda deste profissional.');
  }
}

/** POST /estabelecimentos/:id/profissionais (administrador dono) */
const criar = asyncHandler(async (req, res) => {
  const estabelecimentoId = exigirInteiro(req.params.id, 'Estabelecimento inválido.');
  await exigirDono(estabelecimentoId, req.usuarioId);

  const nome = exigirTexto(req.body.nome, 'Informe o nome do profissional.');
  const email = exigirEmail(req.body.email);
  const senha = exigirTexto(req.body.senha, 'Informe a senha.');
  if (senha.length < 6) throw new ErroHttp(400, 'A senha deve ter pelo menos 6 caracteres.');

  const servicosIds = [...new Set((req.body.servicosIds || []).map((id) => exigirInteiro(id, 'Serviço inválido.')))];
  const encontrados = await ProfissionalModel.contarServicosDoEstabelecimento(estabelecimentoId, servicosIds);
  if (encontrados !== servicosIds.length) {
    throw new ErroHttp(400, 'Algum serviço informado não pertence a este estabelecimento.');
  }

  if (await UsuarioModel.buscarPorEmail(email)) throw new ErroHttp(409, 'Este email já está cadastrado.');

  const id = await ProfissionalModel.criarCompleto(
    {
      estabelecimentoId,
      usuario: {
        nome,
        email,
        senhaHash: await bcrypt.hash(senha, 10),
        telefone: textoOpcional(req.body.telefone),
        tipo: 'profissional',
      },
      especialidade: textoOpcional(req.body.especialidade),
      servicosIds,
    },
    UsuarioModel.criar
  );

  res.status(201).json({ id, mensagem: 'Profissional criado com expediente padrão (seg-sáb, 09-12h e 13-18h).' });
});

/** GET /profissionais/:id/horarios-disponiveis?data=AAAA-MM-DD&servicoId=&ignorarAgendamentoId= */
const horariosDisponiveis = asyncHandler(async (req, res) => {
  const profissionalId = exigirInteiro(req.params.id, 'Profissional inválido.');
  const servicoId = exigirInteiro(req.query.servicoId, 'Informe o serviço.');
  const data = exigirData(req.query.data);

  const servico = await ProfissionalModel.buscarServicoDoProfissional(profissionalId, servicoId);
  if (!servico) throw new ErroHttp(400, 'Este profissional não realiza o serviço escolhido.');

  // No reagendamento, o horário atual do próprio cliente conta como livre
  let ignorarAgendamentoId = 0;
  if (req.query.ignorarAgendamentoId) {
    const id = exigirInteiro(req.query.ignorarAgendamentoId, 'Agendamento inválido.');
    const agendamento = await AgendamentoModel.buscarContexto(id);
    if (agendamento && agendamento.clienteId === req.usuarioId) ignorarAgendamentoId = id;
  }

  const horarios = await AgendamentoService.horariosDisponiveis(db, {
    profissionalId,
    duracaoMin: servico.duracaoMin,
    data,
    ignorarAgendamentoId,
  });
  res.json({ data, horarios });
});

/** PUT /profissionais/:id/horarios  { horarios: [{ diaSemana, horaInicio, horaFim }] } */
const definirHorarios = asyncHandler(async (req, res) => {
  const profissionalId = exigirInteiro(req.params.id, 'Profissional inválido.');
  await exigirPermissaoSobreProfissional(profissionalId, req.usuarioId);

  if (!Array.isArray(req.body.horarios)) throw new ErroHttp(400, 'Informe a lista de horários.');

  const horarios = req.body.horarios.map((h) => {
    const diaSemana = Number(h.diaSemana);
    if (!Number.isInteger(diaSemana) || diaSemana < 0 || diaSemana > 6) {
      throw new ErroHttp(400, 'diaSemana deve ser de 0 (domingo) a 6 (sábado).');
    }
    const horaInicio = exigirHora(h.horaInicio);
    const horaFim = exigirHora(h.horaFim);
    if (paraMinutos(horaFim) <= paraMinutos(horaInicio)) {
      throw new ErroHttp(400, 'O horário final deve ser maior que o inicial.');
    }
    return { diaSemana, horaInicio, horaFim };
  });

  await ProfissionalModel.substituirHorarios(profissionalId, horarios);
  res.json({ mensagem: 'Expediente atualizado.' });
});

/** POST /profissionais/:id/bloqueios  { data, horaInicio, horaFim, motivo } */
const criarBloqueio = asyncHandler(async (req, res) => {
  const profissionalId = exigirInteiro(req.params.id, 'Profissional inválido.');
  await exigirPermissaoSobreProfissional(profissionalId, req.usuarioId);

  const horaInicio = exigirHora(req.body.horaInicio);
  const horaFim = exigirHora(req.body.horaFim);
  if (paraMinutos(horaFim) <= paraMinutos(horaInicio)) {
    throw new ErroHttp(400, 'O horário final deve ser maior que o inicial.');
  }

  const data = exigirData(req.body.data);
  if (await ProfissionalModel.existeAgendamentoNoIntervalo({ profissionalId, data, horaInicio, horaFim })) {
    throw new ErroHttp(409, 'Há um agendamento neste horário. Cancele-o antes de bloquear.');
  }

  const id = await ProfissionalModel.criarBloqueio({
    profissionalId,
    data,
    horaInicio,
    horaFim,
    motivo: textoOpcional(req.body.motivo),
  });
  res.status(201).json({ id, mensagem: 'Horário bloqueado.' });
});

/** DELETE /profissionais/bloqueios/:id (profissional dono ou administrador) */
const excluirBloqueio = asyncHandler(async (req, res) => {
  const id = exigirInteiro(req.params.id, 'Bloqueio inválido.');
  const bloqueio = await ProfissionalModel.buscarBloqueio(id);
  if (!bloqueio) throw new ErroHttp(404, 'Bloqueio não encontrado.');
  if (bloqueio.usuarioId !== req.usuarioId && bloqueio.administradorId !== req.usuarioId) {
    throw new ErroHttp(403, 'Você não pode remover este bloqueio.');
  }

  await ProfissionalModel.excluirBloqueio(id);
  res.json({ mensagem: 'Bloqueio removido.' });
});

/** GET /profissionais/meus - profissionais que o usuário representa */
const meus = asyncHandler(async (req, res) => {
  res.json(await ProfissionalModel.listarDoUsuario(req.usuarioId));
});

module.exports = { criar, horariosDisponiveis, definirHorarios, criarBloqueio, excluirBloqueio, meus };
