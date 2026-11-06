const AgendamentoModel = require('../models/agendamentoModel');
const NotificacaoModel = require('../models/notificacaoModel');
const ProfissionalModel = require('../models/profissionalModel');
const AgendamentoService = require('../services/agendamentoService');
const ErroHttp = require('../utils/ErroHttp');
const asyncHandler = require('../utils/asyncHandler');
const { calcularResumoAgenda, calcularExpediente } = require('../utils/agenda');
const { formatarDataBR, diaDaSemana } = require('../utils/horarios');
const { exigirInteiro, exigirData, exigirHora } = require('../utils/validacao');

/** Carrega o agendamento ou responde 404. */
async function carregar(req) {
  const id = exigirInteiro(req.params.id, 'Agendamento inválido.');
  const agendamento = await AgendamentoModel.buscarContexto(id);
  if (!agendamento) throw new ErroHttp(404, 'Agendamento não encontrado.');
  return agendamento;
}

const ehDaCasa = (agendamento, usuarioId) =>
  agendamento.profissionalUsuarioId === usuarioId || agendamento.administradorId === usuarioId;

function resumo(agendamento) {
  return `${agendamento.servicoNome} em ${formatarDataBR(agendamento.data)} às ${agendamento.horaInicio}`;
}

/** POST /agendamentos { profissionalId, servicoId, data, horaInicio } (cliente) */
const criar = asyncHandler(async (req, res) => {
  const id = await AgendamentoService.criarAgendamento({
    clienteId: req.usuarioId,
    profissionalId: exigirInteiro(req.body.profissionalId, 'Informe o profissional.'),
    servicoId: exigirInteiro(req.body.servicoId, 'Informe o serviço.'),
    data: exigirData(req.body.data),
    horaInicio: exigirHora(req.body.horaInicio),
  });
  res.status(201).json({ id, mensagem: 'Agendamento realizado. Aguarde a confirmação.' });
});

/** PATCH /agendamentos/:id/reagendar { data, horaInicio } (cliente dono) */
const reagendar = asyncHandler(async (req, res) => {
  await AgendamentoService.reagendarAgendamento({
    agendamentoId: exigirInteiro(req.params.id, 'Agendamento inválido.'),
    clienteId: req.usuarioId,
    data: exigirData(req.body.data),
    horaInicio: exigirHora(req.body.horaInicio),
  });
  res.json({ mensagem: 'Agendamento reagendado. Aguarde a nova confirmação.' });
});

/** GET /agendamentos/meus?periodo=todos|proximos|historico */
const listarMeus = asyncHandler(async (req, res) => {
  const periodo = ['proximos', 'historico'].includes(req.query.periodo) ? req.query.periodo : 'todos';
  res.json(await AgendamentoModel.listarDoCliente(req.usuarioId, periodo));
});

/** GET /agendamentos/agenda?data= (profissional ou administrador): resumo, expediente, atendimentos e bloqueios */
const listarAgenda = asyncHandler(async (req, res) => {
  const data = exigirData(req.query.data);

  const [itens, janelas, bloqueios] = await Promise.all([
    AgendamentoModel.listarAgenda(req.usuarioId, data),
    ProfissionalModel.listarExpedienteDoDia(req.usuarioId, diaDaSemana(data)),
    ProfissionalModel.listarBloqueiosDoDia(req.usuarioId, data),
  ]);

  res.json({
    data,
    resumo: calcularResumoAgenda({ itens, janelas, bloqueios }),
    expediente: calcularExpediente(janelas),
    itens,
    bloqueios,
  });
});

/** PATCH /agendamentos/:id/cancelar (cliente dono, profissional ou administrador) */
const cancelar = asyncHandler(async (req, res) => {
  const agendamento = await carregar(req);
  const eCliente = agendamento.clienteId === req.usuarioId;

  if (!eCliente && !ehDaCasa(agendamento, req.usuarioId)) {
    throw new ErroHttp(403, 'Você não pode cancelar este agendamento.');
  }
  if (!['pendente', 'confirmado'].includes(agendamento.status)) {
    throw new ErroHttp(409, 'Este agendamento não pode mais ser cancelado.');
  }

  await AgendamentoModel.atualizarStatus(agendamento.id, 'cancelado');

  await NotificacaoModel.criar({
    usuarioId: eCliente ? agendamento.profissionalUsuarioId : agendamento.clienteId,
    agendamentoId: agendamento.id,
    titulo: 'Agendamento cancelado',
    mensagem: eCliente
      ? `${agendamento.clienteNome} cancelou ${resumo(agendamento)}.`
      : `Seu agendamento de ${resumo(agendamento)} foi cancelado pelo estabelecimento.`,
  });

  res.json({ mensagem: 'Agendamento cancelado.' });
});

/** PATCH /agendamentos/:id/confirmar (profissional ou administrador) */
const confirmar = asyncHandler(async (req, res) => {
  const agendamento = await carregar(req);
  if (!ehDaCasa(agendamento, req.usuarioId)) throw new ErroHttp(403, 'Você não pode confirmar este agendamento.');
  if (agendamento.status !== 'pendente') throw new ErroHttp(409, 'Apenas agendamentos pendentes podem ser confirmados.');

  await AgendamentoModel.atualizarStatus(agendamento.id, 'confirmado');
  await NotificacaoModel.criar({
    usuarioId: agendamento.clienteId,
    agendamentoId: agendamento.id,
    titulo: 'Agendamento confirmado',
    mensagem: `Seu agendamento de ${resumo(agendamento)} foi confirmado.`,
  });

  res.json({ mensagem: 'Agendamento confirmado.' });
});

/** PATCH /agendamentos/:id/concluir (profissional ou administrador) */
const concluir = asyncHandler(async (req, res) => {
  const agendamento = await carregar(req);
  if (!ehDaCasa(agendamento, req.usuarioId)) throw new ErroHttp(403, 'Você não pode concluir este agendamento.');
  if (agendamento.status !== 'confirmado') throw new ErroHttp(409, 'Apenas agendamentos confirmados podem ser concluídos.');

  await AgendamentoModel.atualizarStatus(agendamento.id, 'concluido');
  await NotificacaoModel.criar({
    usuarioId: agendamento.clienteId,
    agendamentoId: agendamento.id,
    titulo: 'Atendimento concluído',
    mensagem: `Como foi ${resumo(agendamento)}? Avalie o atendimento.`,
  });

  res.json({ mensagem: 'Atendimento concluído.' });
});

module.exports = { criar, reagendar, listarMeus, listarAgenda, cancelar, confirmar, concluir };
