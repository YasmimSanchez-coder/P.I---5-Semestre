const db = require('../config/database');

const CAMPOS_PUBLICOS = 'id, nome, email, telefone, tipo';

const UsuarioModel = {
  async criar({ nome, email, senhaHash, telefone, tipo }, conexao = db) {
    const [resultado] = await conexao.query(
      `INSERT INTO usuarios (nome, email, senha_hash, telefone, tipo) VALUES (?, ?, ?, ?, ?)`,
      [nome, email, senhaHash, telefone || null, tipo]
    );
    return resultado.insertId;
  },

  async buscarPorEmail(email) {
    const [linhas] = await db.query(
      `SELECT ${CAMPOS_PUBLICOS}, senha_hash AS senhaHash FROM usuarios WHERE email = ? LIMIT 1`,
      [email]
    );
    return linhas[0] || null;
  },

  async buscarPorId(id) {
    const [linhas] = await db.query(
      `SELECT ${CAMPOS_PUBLICOS} FROM usuarios WHERE id = ? LIMIT 1`,
      [id]
    );
    return linhas[0] || null;
  },
};

module.exports = UsuarioModel;
