/**
 * Testa criação e reagendamento com um banco simulado em memória
 * (não precisa de MySQL). Foco: impedir conflitos de horário.
 */
const test = require('node:test');
const assert = require('node:assert/strict');
const { dataLocal } = require('../src/utils/horarios');

const CAMINHO_BANCO = require.resolve('../src/config/database');
const DATA = dataLocal(new Date(Date.now() + 7 * 24 * 60 * 60 * 1000)); // daqui a uma semana

function criarBancoSimulado() {
  const estado = { agendamentos: [], notificacoes: [], commits: 0, rollbacks: 0 };

  const query = async (sql, params = []) => {
    if (sql.includes('FROM agendamentos WHERE id = ? AND cliente_id = ?')) {
      const a = estado.agendamentos.find((x) => x.id === params[0] && x.clienteId === params[1]);
      return [a ? [{ profissionalId: a.profissionalId }] : []];
    }
    if (sql.includes('FROM profissionais WHERE id = ? FOR UPDATE')) return [[{ id: params[0], usuarioId: 99 }]];
    if (sql.includes('JOIN profissional_servicos')) return [[{ id: 1, nome: 'Corte', duracaoMin: 60 }]];
    if (sql.includes('WHERE a.id = ? FOR UPDATE')) {
      const a = estado.agendamentos.find((x) => x.id === params[0]);
      return [[{ id: a.id, status: a.status, servicoNome: 'Corte', duracaoMin: 60 }]];
    }
    if (sql.includes('FROM horarios_disponiveis')) return [[{ horaInicio: '09:00:00', horaFim: '12:00:00' }]];
    if (sql.includes('FROM bloqueios_agenda')) return [[]];
    if (sql.includes('AND id <> ?')) {
      const [profissionalId, data, ignorarId] = params;
      return [estado.agendamentos
        .filter((a) => a.profissionalId === profissionalId && a.data === data
          && ['pendente', 'confirmado'].includes(a.status) && a.id !== ignorarId)
        .map((a) => ({ horaInicio: a.horaInicio, horaFim: a.horaFim }))];
    }
    if (sql.includes('INSERT INTO agendamentos')) {
      const [clienteId, profissionalId, , data, horaInicio, horaFim] = params;
      const id = estado.agendamentos.length + 1;
      estado.agendamentos.push({ id, clienteId, profissionalId, data, horaInicio, horaFim, status: 'pendente' });
      return [{ insertId: id }];
    }
    if (sql.includes('UPDATE agendamentos SET data')) {
      const [data, horaInicio, horaFim, id] = params;
      Object.assign(estado.agendamentos.find((x) => x.id === id), { data, horaInicio, horaFim, status: 'pendente' });
      return [{}];
    }
    if (sql.includes('SELECT nome FROM usuarios')) return [[{ nome: 'Maria' }]];
    if (sql.includes('INSERT INTO notificacoes')) { estado.notificacoes.push(params); return [{}]; }
    throw new Error(`SQL não previsto no teste: ${sql}`);
  };

  const conexao = {
    query,
    beginTransaction: async () => {},
    commit: async () => { estado.commits += 1; },
    rollback: async () => { estado.rollbacks += 1; },
    release: () => {},
  };

  require.cache[CAMINHO_BANCO] = {
    id: CAMINHO_BANCO, filename: CAMINHO_BANCO, loaded: true,
    exports: { query, getConnection: async () => conexao },
  };
  return estado;
}

function novoCenario() {
  const estado = criarBancoSimulado();
  delete require.cache[require.resolve('../src/services/agendamentoService')];
  const servico = require('../src/services/agendamentoService');

  return {
    estado,
    agendar: (horaInicio, clienteId = 1) =>
      servico.criarAgendamento({ clienteId, profissionalId: 1, servicoId: 1, data: DATA, horaInicio }),
    reagendar: (agendamentoId, horaInicio, clienteId = 1) =>
      servico.reagendarAgendamento({ agendamentoId, clienteId, data: DATA, horaInicio }),
  };
}

const erro409 = (e) => e.status === 409;

// ---------------------------------------------------------------- criação
test('agenda em horário livre, calcula o fim e notifica o profissional', async () => {
  const { estado, agendar } = novoCenario();
  const id = await agendar('09:00');
  assert.equal(id, 1);
  assert.equal(estado.agendamentos[0].horaFim, '10:00');
  assert.equal(estado.notificacoes.length, 1);
  assert.equal(estado.commits, 1);
});

test('impede dois clientes no mesmo profissional e horário', async () => {
  const { estado, agendar } = novoCenario();
  await agendar('09:00');
  await assert.rejects(() => agendar('09:00', 2), erro409);
  assert.equal(estado.agendamentos.length, 1);
  assert.equal(estado.rollbacks, 1);
});

test('impede horário que sobrepõe parcialmente outro agendamento', async () => {
  const { agendar } = novoCenario();
  await agendar('09:00'); // 09:00-10:00
  await assert.rejects(() => agendar('09:30', 2), erro409);
});

test('permite horário colado, logo após o término do anterior', async () => {
  const { estado, agendar } = novoCenario();
  await agendar('09:00');
  await agendar('10:00', 2);
  assert.equal(estado.agendamentos.length, 2);
});

test('recusa horário fora do expediente', async () => {
  const { agendar } = novoCenario();
  await assert.rejects(() => agendar('08:00'), erro409);
  await assert.rejects(() => agendar('11:30'), erro409); // terminaria 12:30
});

test('horário de agendamento cancelado volta a ficar livre', async () => {
  const { estado, agendar } = novoCenario();
  await agendar('09:00');
  estado.agendamentos[0].status = 'cancelado';
  await agendar('09:00', 2);
  assert.equal(estado.agendamentos.length, 2);
});

// ---------------------------------------------------------- reagendamento
test('reagenda para outro horário livre e volta para pendente', async () => {
  const { estado, agendar, reagendar } = novoCenario();
  await agendar('09:00');
  estado.agendamentos[0].status = 'confirmado';

  await reagendar(1, '11:00');

  assert.equal(estado.agendamentos[0].horaInicio, '11:00');
  assert.equal(estado.agendamentos[0].horaFim, '12:00');
  assert.equal(estado.agendamentos[0].status, 'pendente');
  assert.equal(estado.notificacoes.length, 2); // criação + reagendamento
});

test('reagendamento pode sobrepor o próprio horário antigo', async () => {
  const { estado, agendar, reagendar } = novoCenario();
  await agendar('09:00'); // 09:00-10:00
  await reagendar(1, '09:30'); // 09:30-10:30 invade o horário antigo, mas é o mesmo agendamento
  assert.equal(estado.agendamentos[0].horaInicio, '09:30');
});

test('reagendamento não pode ocupar horário de outro cliente', async () => {
  const { estado, agendar, reagendar } = novoCenario();
  await agendar('09:00', 1);
  await agendar('10:00', 2);
  await assert.rejects(() => reagendar(1, '10:00', 1), erro409);
  assert.equal(estado.agendamentos[0].horaInicio, '09:00'); // não mudou
});

test('cliente não reagenda agendamento de outra pessoa', async () => {
  const { agendar, reagendar } = novoCenario();
  await agendar('09:00', 1);
  await assert.rejects(() => reagendar(1, '11:00', 2), (e) => e.status === 404);
});

test('agendamento cancelado não pode ser reagendado', async () => {
  const { estado, agendar, reagendar } = novoCenario();
  await agendar('09:00');
  estado.agendamentos[0].status = 'cancelado';
  await assert.rejects(() => reagendar(1, '11:00'), erro409);
});
