const test = require('node:test');
const assert = require('node:assert/strict');
const { explicarErroDeBanco } = require('../src/utils/erroBanco');

test('explica senha errada, MySQL desligado e banco não importado', () => {
  assert.match(explicarErroDeBanco({ code: 'ER_ACCESS_DENIED_ERROR' }), /DB_PASSWORD/);
  assert.match(explicarErroDeBanco({ code: 'ECONNREFUSED' }), /instalado e ligado/);
  assert.match(explicarErroDeBanco({ code: 'ER_BAD_DB_ERROR' }), /agendai\.sql/);
  assert.match(explicarErroDeBanco({ code: 'ER_NO_SUCH_TABLE' }), /sem as tabelas/);
});

test('erro desconhecido devolve a própria mensagem', () => {
  assert.equal(explicarErroDeBanco({ message: 'algo estranho' }), 'algo estranho');
  assert.match(explicarErroDeBanco(undefined), /desconhecido/);
});
