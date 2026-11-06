/**
 * Regras de horário do agendamento (funções puras, sem acesso a banco).
 * Horas trafegam como "HH:MM" ou "HH:MM:SS"; datas como "AAAA-MM-DD".
 */
const PASSO_MINUTOS = 30;

const doisDigitos = (n) => String(n).padStart(2, '0');

function paraMinutos(hora) {
  const [h, m] = String(hora).split(':').map(Number);
  return h * 60 + m;
}

function paraHHMM(minutos) {
  return `${doisDigitos(Math.floor(minutos / 60))}:${doisDigitos(minutos % 60)}`;
}

function dataValida(data) {
  if (typeof data !== 'string' || !/^\d{4}-\d{2}-\d{2}$/.test(data)) return false;
  const d = new Date(`${data}T00:00:00Z`);
  return !Number.isNaN(d.getTime()) && d.toISOString().slice(0, 10) === data;
}

/** 0 = domingo ... 6 = sábado */
function diaDaSemana(data) {
  return new Date(`${data}T00:00:00Z`).getUTCDay();
}

function dataLocal(d = new Date()) {
  return `${d.getFullYear()}-${doisDigitos(d.getMonth() + 1)}-${doisDigitos(d.getDate())}`;
}

function formatarDataBR(data) {
  const [ano, mes, dia] = data.split('-');
  return `${dia}/${mes}/${ano}`;
}

/**
 * Menor minuto do dia em que um horário pode começar.
 * Data passada: Infinity (nada disponível). Hoje: agora. Futuro: 0.
 */
function minutoMinimoParaData(data, agora = new Date()) {
  const hoje = dataLocal(agora);
  if (data < hoje) return Infinity;
  if (data === hoje) return agora.getHours() * 60 + agora.getMinutes();
  return 0;
}

/**
 * Gera os horários de início possíveis.
 * @param janelas    expediente do dia: [{ horaInicio, horaFim }]
 * @param ocupados   intervalos já usados (agendamentos + bloqueios): [{ horaInicio, horaFim }]
 * @param duracaoMin duração do serviço em minutos
 */
function gerarHorarios({ janelas, ocupados, duracaoMin, minutoMinimo = 0, passo = PASSO_MINUTOS }) {
  const intervalosOcupados = ocupados.map((o) => ({
    inicio: paraMinutos(o.horaInicio),
    fim: paraMinutos(o.horaFim),
  }));

  const horarios = new Set();

  for (const janela of janelas) {
    const inicioJanela = paraMinutos(janela.horaInicio);
    const fimJanela = paraMinutos(janela.horaFim);

    for (let inicio = inicioJanela; inicio + duracaoMin <= fimJanela; inicio += passo) {
      if (inicio < minutoMinimo) continue;

      const fim = inicio + duracaoMin;
      const temConflito = intervalosOcupados.some((o) => inicio < o.fim && fim > o.inicio);
      if (!temConflito) horarios.add(paraHHMM(inicio));
    }
  }

  return [...horarios].sort();
}

module.exports = {
  PASSO_MINUTOS,
  paraMinutos,
  paraHHMM,
  dataValida,
  diaDaSemana,
  dataLocal,
  formatarDataBR,
  minutoMinimoParaData,
  gerarHorarios,
};
