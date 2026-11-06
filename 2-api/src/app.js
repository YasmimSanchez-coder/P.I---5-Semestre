const express = require('express');
const cors = require('cors');
const db = require('./config/database');
const routes = require('./routes');
const { explicarErroDeBanco } = require('./utils/erroBanco');
const { rotaNaoEncontrada, tratarErros } = require('./middlewares/errorHandler');

const app = express();

app.use(cors());
app.use(express.json());

app.get('/', (req, res) => res.json({ mensagem: 'API AgendAí no ar' }));

// Teste de conexão: http://localhost:3000/saude
app.get('/saude', async (req, res) => {
  try {
    await db.query('SELECT 1 FROM usuarios LIMIT 1');
    res.json({ api: 'ok', banco: 'ok' });
  } catch (erro) {
    res.status(500).json({ api: 'ok', banco: 'erro', detalhe: explicarErroDeBanco(erro) });
  }
});
app.use('/api', routes);

app.use(rotaNaoEncontrada);
app.use(tratarErros);

module.exports = app;
