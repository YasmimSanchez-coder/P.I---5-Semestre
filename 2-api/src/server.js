require('dotenv').config();

if (!process.env.JWT_SECRET) {
  console.error('Defina JWT_SECRET no arquivo .env (copie de .env.example).');
  process.exit(1);
}

const app = require('./app');
const db = require('./config/database');
const { explicarErroDeBanco } = require('./utils/erroBanco');
const { iniciarLembretes } = require('./services/lembreteService');

const PORT = process.env.PORT || 3000;

app.listen(PORT, async () => {
  console.log(`Servidor rodando em http://localhost:${PORT}`);

  try {
    await db.query('SELECT 1 FROM usuarios LIMIT 1');
    console.log(`Banco de dados conectado (${process.env.DB_NAME}).`);
    iniciarLembretes();
  } catch (erro) {
    console.error('');
    console.error('ATENÇÃO: a API iniciou, mas NÃO conseguiu usar o banco de dados.');
    console.error(`  ${explicarErroDeBanco(erro)}`);
    console.error('  Depois de corrigir, pare (Ctrl+C) e inicie a API de novo.');
    console.error('');
  }
});
