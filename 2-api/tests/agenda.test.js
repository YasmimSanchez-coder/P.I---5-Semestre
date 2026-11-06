const test = require('node:test');
const assert = require('node:assert/strict');
const { calcularResumoAgenda, calcularExpediente } = require('../src/utils/agenda');

const expedienteNormal = [
  { horaInicio: '09:00', horaFim: '12:00' },
  { horaInicio: '13:00', horaFim: '18:00' },
]; // 8 horas = 480 min

test('resumo: conta atendimentos ativos, soma receita e calcula ocupação', () => {
  const itens = [
    { status: 'confirmado', horaInicio: '09:00', horaFim: '10:00', servicoPreco: 120 },
    { status: 'pendente', horaInicio: '10:30', horaFim: '12:00', servicoPreco: 180 },
    { status: 'concluido', horaInicio: '15:00', horaFim: '16:00', servicoPreco: 120 },
    { status: 'cancelado', horaInicio: '16:00', horaFim: '17:00', servicoPreco: 999 },
  ];
  const r = calcularResumoAgenda({ itens, janelas: expedienteNormal, bloqueios: [] });

  assert.equal(r.agendamentos, 3); // cancelado fora
  assert.equal(r.receitaProjetada, 420);
  assert.equal(r.ocupacaoPercentual, Math.round((210 / 480) * 100)); // 60+90+60 = 210 min
});

test('resumo: horários bloqueados reduzem a capacidade do dia', () => {
  const itens = [{ status: 'confirmado', horaInicio: '09:00', horaFim: '12:00', servicoPreco: 100 }];
  const bloqueios = [{ horaInicio: '13:00', horaFim: '18:00' }]; // sobram 3h de capacidade
  const r = calcularResumoAgenda({ itens, janelas: expedienteNormal, bloqueios });
  assert.equal(r.ocupacaoPercentual, 100);
});

test('resumo: dia sem expediente não divide por zero', () => {
  const r = calcularResumoAgenda({ itens: [], janelas: [], bloqueios: [] });
  assert.deepEqual(r, { agendamentos: 0, receitaProjetada: 0, ocupacaoPercentual: 0 });
});

test('resumo: ocupação nunca passa de 100%', () => {
  const itens = [{ status: 'confirmado', horaInicio: '09:00', horaFim: '18:00', servicoPreco: 1 }];
  const r = calcularResumoAgenda({ itens, janelas: [{ horaInicio: '09:00', horaFim: '10:00' }], bloqueios: [] });
  assert.equal(r.ocupacaoPercentual, 100);
});

test('expediente: do primeiro início ao último fim', () => {
  assert.deepEqual(calcularExpediente(expedienteNormal), { inicio: '09:00', fim: '18:00' });
  assert.equal(calcularExpediente([]), null);
});
