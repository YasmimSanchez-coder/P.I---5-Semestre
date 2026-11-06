const ErroHttp = require('../utils/ErroHttp');

function rotaNaoEncontrada(req, res, next) {
  next(new ErroHttp(404, 'Rota não encontrada.'));
}

// eslint-disable-next-line no-unused-vars
function tratarErros(err, req, res, next) {
  if (err instanceof ErroHttp) {
    return res.status(err.status).json({ erro: err.message });
  }
  if (err.type === 'entity.parse.failed') {
    return res.status(400).json({ erro: 'JSON inválido no corpo da requisição.' });
  }
  console.error(err);
  return res.status(500).json({ erro: 'Erro interno no servidor.' });
}

module.exports = { rotaNaoEncontrada, tratarErros };
