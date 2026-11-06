const AgendamentoModel = require('../models/agendamentoModel');
const AvaliacaoModel = require('../models/avaliacaoModel');
const ErroHttp = require('../utils/ErroHttp');
const asyncHandler = require('../utils/asyncHandler');
const { exigirInteiro, textoOpcional } = require('../utils/validacao');

/** POST /avaliacoes { agendamentoId, nota (1-5), comentario } (cliente) */
const criar = asyncHandler(async (req, res) => {
  const agendamentoId = exigirInteiro(req.body.agendamentoId, 'Informe o agendamento.');
  const nota = Number(req.body.nota);
  if (!Number.isInteger(nota) || nota < 1 || nota > 5) {
    throw new ErroHttp(400, 'A nota deve ser um número inteiro de 1 a 5.');
  }

  const agendamento = await AgendamentoModel.buscarContexto(agendamentoId);
  if (!agendamento || agendamento.clienteId !== req.usuarioId) {
    throw new ErroHttp(404, 'Agendamento não encontrado.');
  }
  if (agendamento.status !== 'concluido') {
    throw new ErroHttp(409, 'Você só pode avaliar atendimentos concluídos.');
  }
  if (await AvaliacaoModel.existeParaAgendamento(agendamentoId)) {
    throw new ErroHttp(409, 'Este atendimento já foi avaliado.');
  }

  const id = await AvaliacaoModel.criar({
    agendamentoId,
    clienteId: req.usuarioId,
    estabelecimentoId: agendamento.estabelecimentoId,
    profissionalId: agendamento.profissionalId,
    nota,
    comentario: textoOpcional(req.body.comentario),
  });

  res.status(201).json({ id, mensagem: 'Avaliação registrada. Obrigado!' });
});

/** GET /estabelecimentos/:id/avaliacoes */
const listarPorEstabelecimento = asyncHandler(async (req, res) => {
  const id = exigirInteiro(req.params.id, 'Estabelecimento inválido.');
  res.json(await AvaliacaoModel.listarPorEstabelecimento(id));
});

module.exports = { criar, listarPorEstabelecimento };
