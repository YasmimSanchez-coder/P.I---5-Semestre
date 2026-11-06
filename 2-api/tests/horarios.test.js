const test = require('node:test');
const assert = require('node:assert/strict');
const {
  paraMinutos,
  paraHHMM,
  dataValida,
  diaDaSemana,
  minutoMinimoParaData,
  gerarHorarios,
  formatarDataBR,
} = require('../src/utils/horarios');

const manha = [{ horaInicio: '09:00:00', horaFim: '12:00:00' }];

test('conversões de hora', () => {
  assert.equal(paraMinutos('09:30:00'), 570);
  assert.equal(paraMinutos('09:30'), 570);
  assert.equal(paraHHMM(570), '09:30');
  assert.equal(paraHHMM(5), '00:05');
});

test('validação de data', () => {
  assert.equal(dataValida('2026-10-09'), true);
  assert.equal(dataValida('2026-02-30'), false);
  assert.equal(dataValida('09/10/2026'), false);
  assert.equal(dataValida(undefined), false);
});

test('dia da semana e formatação', () => {
  assert.equal(diaDaSemana('2026-10-09'), 5); // sexta-feira
  assert.equal(diaDaSemana('2026-10-11'), 0); // domingo
  assert.equal(formatarDataBR('2026-10-09'), '09/10/2026');
});

test('gera horários de 30 em 30 min respeitando a duração', () => {
  const r = gerarHorarios({ janelas: manha, ocupados: [], duracaoMin: 60 });
  assert.deepEqual(r, ['09:00', '09:30', '10:00', '10:30', '11:00']);
});

test('serviço mais longo que o expediente não gera horários', () => {
  assert.deepEqual(gerarHorarios({ janelas: manha, ocupados: [], duracaoMin: 240 }), []);
});

test('horário ocupado remove todos os inícios que se sobrepõem', () => {
  const ocupados = [{ horaInicio: '10:00:00', horaFim: '11:00:00' }];
  const r = gerarHorarios({ janelas: manha, ocupados, duracaoMin: 60 });
  // 09:30, 10:00 e 10:30 invadem o intervalo; 09:00 e 11:00 apenas encostam
  assert.deepEqual(r, ['09:00', '11:00']);
});

test('serviços curtos cabem nas brechas', () => {
  const ocupados = [{ horaInicio: '10:00:00', horaFim: '11:00:00' }];
  const r = gerarHorarios({ janelas: manha, ocupados, duracaoMin: 30 });
  assert.deepEqual(r, ['09:00', '09:30', '11:00', '11:30']);
});

test('duas janelas respeitam o intervalo de almoço', () => {
  const janelas = [
    { horaInicio: '09:00:00', horaFim: '12:00:00' },
    { horaInicio: '13:00:00', horaFim: '14:00:00' },
  ];
  const r = gerarHorarios({ janelas, ocupados: [], duracaoMin: 60 });
  assert.deepEqual(r, ['09:00', '09:30', '10:00', '10:30', '11:00', '13:00']);
});

test('não oferece horários que já passaram hoje', () => {
  const r = gerarHorarios({ janelas: manha, ocupados: [], duracaoMin: 60, minutoMinimo: 10 * 60 + 15 });
  assert.deepEqual(r, ['10:30', '11:00']);
});

test('minutoMinimoParaData: passado, hoje e futuro', () => {
  const agora = new Date(2026, 9, 9, 10, 15); // 09/10/2026 10:15 (local)
  assert.equal(minutoMinimoParaData('2026-10-08', agora), Infinity);
  assert.equal(minutoMinimoParaData('2026-10-09', agora), 615);
  assert.equal(minutoMinimoParaData('2026-10-10', agora), 0);
});

test('data passada não tem horários', () => {
  const agora = new Date(2026, 9, 9, 10, 15);
  const r = gerarHorarios({
    janelas: manha, ocupados: [], duracaoMin: 30,
    minutoMinimo: minutoMinimoParaData('2026-10-01', agora),
  });
  assert.deepEqual(r, []);
});
