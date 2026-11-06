const ErroHttp = require('./ErroHttp');
const { dataValida } = require('./horarios');

const REGEX_EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
const REGEX_HORA = /^([01]\d|2[0-3]):[0-5]\d$/;

function exigirTexto(valor, mensagem) {
  const texto = typeof valor === 'string' ? valor.trim() : '';
  if (!texto) throw new ErroHttp(400, mensagem);
  return texto;
}

function textoOpcional(valor) {
  const texto = typeof valor === 'string' ? valor.trim() : '';
  return texto || null;
}

function exigirEmail(valor) {
  const email = exigirTexto(valor, 'Informe o email.').toLowerCase();
  if (!REGEX_EMAIL.test(email)) throw new ErroHttp(400, 'Email inválido.');
  return email;
}

function exigirInteiro(valor, mensagem) {
  const numero = Number(valor);
  if (!Number.isInteger(numero) || numero <= 0) throw new ErroHttp(400, mensagem);
  return numero;
}

function exigirNumero(valor, mensagem, { minimo = 0 } = {}) {
  const numero = Number(valor);
  if (valor === '' || valor === null || valor === undefined || !Number.isFinite(numero) || numero < minimo) {
    throw new ErroHttp(400, mensagem);
  }
  return numero;
}

function numeroOpcional(valor, mensagem) {
  if (valor === undefined || valor === null || valor === '') return null;
  return exigirNumero(valor, mensagem, { minimo: -Infinity });
}

function exigirHora(valor, mensagem = 'Horário inválido (use HH:MM).') {
  if (typeof valor !== 'string' || !REGEX_HORA.test(valor)) throw new ErroHttp(400, mensagem);
  return valor;
}

function exigirData(valor, mensagem = 'Data inválida (use AAAA-MM-DD).') {
  if (!dataValida(valor)) throw new ErroHttp(400, mensagem);
  return valor;
}

module.exports = {
  exigirTexto,
  textoOpcional,
  exigirEmail,
  exigirInteiro,
  exigirNumero,
  numeroOpcional,
  exigirHora,
  exigirData,
};
