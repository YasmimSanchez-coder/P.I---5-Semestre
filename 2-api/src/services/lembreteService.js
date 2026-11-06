const db = require('../config/database');

const INTERVALO_MS = 15 * 60 * 1000;

/**
 * Cria um lembrete para cada agendamento ativo que acontece nas próximas 24 horas
 * e ainda não tem lembrete. Retorna quantos foram criados.
 */
async function gerarLembretes() {
  const [resultado] = await db.query(
    `INSERT INTO notificacoes (usuario_id, agendamento_id, tipo, titulo, mensagem)
     SELECT a.cliente_id, a.id, 'lembrete', 'Lembrete de agendamento',
            CONCAT('Seu agendamento de ', s.nome, ' com ', up.nome, ' é ',
                   IF(a.data = CURDATE(), 'hoje', 'amanhã'),
                   ' às ', TIME_FORMAT(a.hora_inicio, '%H:%i'), '. Não esqueça!')
     FROM agendamentos a
     JOIN servicos s ON s.id = a.servico_id
     JOIN profissionais p ON p.id = a.profissional_id
     JOIN usuarios up ON up.id = p.usuario_id
     WHERE a.status IN ('pendente', 'confirmado')
       AND TIMESTAMP(a.data, a.hora_inicio) BETWEEN NOW() AND DATE_ADD(NOW(), INTERVAL 24 HOUR)
       AND NOT EXISTS (SELECT 1 FROM notificacoes n
                       WHERE n.agendamento_id = a.id AND n.tipo = 'lembrete')`
  );
  return resultado.affectedRows;
}

/** Roda ao iniciar a API e a cada 15 minutos. */
function iniciarLembretes() {
  const executar = () =>
    gerarLembretes()
      .then((total) => total > 0 && console.log(`${total} lembrete(s) gerado(s).`))
      .catch((erro) => console.error('Falha ao gerar lembretes:', erro.message));

  executar();
  setInterval(executar, INTERVALO_MS).unref();
}

module.exports = { gerarLembretes, iniciarLembretes };
