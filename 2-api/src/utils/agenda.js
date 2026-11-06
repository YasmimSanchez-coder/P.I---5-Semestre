const { paraMinutos, paraHHMM } = require('./horarios');

const somarMinutos = (intervalos) =>
  intervalos.reduce((total, i) => total + (paraMinutos(i.horaFim) - paraMinutos(i.horaInicio)), 0);

/**
 * Números do topo da agenda do profissional.
 * @param itens     agendamentos do dia (qualquer status): { horaInicio, horaFim, status, servicoPreco }
 * @param janelas   expediente do dia: { horaInicio, horaFim }
 * @param bloqueios horários bloqueados do dia: { horaInicio, horaFim }
 */
function calcularResumoAgenda({ itens, janelas, bloqueios }) {
  const ativos = itens.filter((i) => i.status !== 'cancelado');
  const capacidade = Math.max(somarMinutos(janelas) - somarMinutos(bloqueios), 0);
  const ocupados = somarMinutos(ativos);

  return {
    agendamentos: ativos.length,
    receitaProjetada: ativos.reduce((total, i) => total + Number(i.servicoPreco), 0),
    ocupacaoPercentual: capacidade === 0 ? 0 : Math.min(100, Math.round((ocupados / capacidade) * 100)),
  };
}

/** Primeiro início e último fim do expediente do dia (null se não há expediente). */
function calcularExpediente(janelas) {
  if (janelas.length === 0) return null;
  return {
    inicio: paraHHMM(Math.min(...janelas.map((j) => paraMinutos(j.horaInicio)))),
    fim: paraHHMM(Math.max(...janelas.map((j) => paraMinutos(j.horaFim)))),
  };
}

module.exports = { calcularResumoAgenda, calcularExpediente };
