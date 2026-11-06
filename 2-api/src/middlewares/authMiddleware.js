const jwt = require('jsonwebtoken');
const ErroHttp = require('../utils/ErroHttp');

/** Exige um token JWT válido e preenche req.usuarioId / req.usuarioTipo. */
function autenticar(req, res, next) {
  const [esquema, token] = (req.headers.authorization || '').split(' ');

  if (esquema !== 'Bearer' || !token) {
    return next(new ErroHttp(401, 'Token não fornecido.'));
  }

  try {
    const payload = jwt.verify(token, process.env.JWT_SECRET);
    req.usuarioId = payload.id;
    req.usuarioTipo = payload.tipo;
    return next();
  } catch (erro) {
    return next(new ErroHttp(401, 'Token inválido ou expirado.'));
  }
}

/** Restringe a rota a determinados tipos de usuário. */
const exigirTipo = (...tipos) => (req, res, next) =>
  tipos.includes(req.usuarioTipo)
    ? next()
    : next(new ErroHttp(403, 'Você não tem permissão para esta ação.'));

module.exports = { autenticar, exigirTipo };
