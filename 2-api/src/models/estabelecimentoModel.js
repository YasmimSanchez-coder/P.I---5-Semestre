const db = require('../config/database');

const numero = (valor) => (valor === null || valor === undefined ? null : Number(valor));

function formatarEstabelecimento(linha) {
  return {
    ...linha,
    latitude: numero(linha.latitude),
    longitude: numero(linha.longitude),
    notaMedia: Number(linha.notaMedia),
    totalAvaliacoes: Number(linha.totalAvaliacoes),
    distanciaKm: numero(linha.distanciaKm),
    precoMinimo: numero(linha.precoMinimo),
  };
}

const CAMPOS = `
  e.id, e.nome, e.descricao, e.endereco, e.latitude, e.longitude,
  e.telefone, e.whatsapp, e.foto_url AS fotoUrl,
  TIME_FORMAT(e.horario_abertura, '%H:%i') AS horarioAbertura,
  TIME_FORMAT(e.horario_fechamento, '%H:%i') AS horarioFechamento,
  (SELECT MIN(s.preco) FROM servicos s
   WHERE s.estabelecimento_id = e.id AND s.ativo = TRUE) AS precoMinimo,
  COALESCE(ROUND(AVG(av.nota), 1), 0) AS notaMedia,
  COUNT(av.id) AS totalAvaliacoes`;

const EstabelecimentoModel = {
  /** Lista com nota média; ordena por distância quando a localização é informada. */
  async listar({ busca, categoria, latitude, longitude }) {
    const parametros = [];
    let distancia = 'NULL';

    if (latitude !== null && longitude !== null) {
      // Fórmula de Haversine (km)
      distancia = `6371 * ACOS(LEAST(1,
        COS(RADIANS(?)) * COS(RADIANS(e.latitude)) * COS(RADIANS(e.longitude) - RADIANS(?))
        + SIN(RADIANS(?)) * SIN(RADIANS(e.latitude))))`;
      parametros.push(latitude, longitude, latitude);
    }

    const condicoes = [];

    if (busca) {
      condicoes.push(`(e.nome LIKE ? OR e.endereco LIKE ?
                       OR EXISTS (SELECT 1 FROM servicos s
                                  WHERE s.estabelecimento_id = e.id AND s.ativo = TRUE AND s.nome LIKE ?))`);
      const termo = `%${busca}%`;
      parametros.push(termo, termo, termo);
    }

    if (categoria) {
      condicoes.push(`EXISTS (SELECT 1 FROM servicos s
                              WHERE s.estabelecimento_id = e.id AND s.ativo = TRUE AND s.categoria = ?)`);
      parametros.push(categoria);
    }

    const filtro = condicoes.length > 0 ? `WHERE ${condicoes.join(' AND ')}` : '';
    const ordem = distancia === 'NULL' ? 'notaMedia DESC, e.nome' : 'distanciaKm';

    const [linhas] = await db.query(
      `SELECT ${CAMPOS}, ${distancia} AS distanciaKm
       FROM estabelecimentos e
       LEFT JOIN avaliacoes av ON av.estabelecimento_id = e.id
       ${filtro}
       GROUP BY e.id
       ORDER BY ${ordem}`,
      parametros
    );
    return linhas.map(formatarEstabelecimento);
  },

  async buscarPorId(id) {
    const [linhas] = await db.query(
      `SELECT ${CAMPOS}, NULL AS distanciaKm
       FROM estabelecimentos e
       LEFT JOIN avaliacoes av ON av.estabelecimento_id = e.id
       WHERE e.id = ?
       GROUP BY e.id`,
      [id]
    );
    return linhas[0] ? formatarEstabelecimento(linhas[0]) : null;
  },

  async buscarDono(id) {
    const [linhas] = await db.query(
      'SELECT id, administrador_id AS administradorId FROM estabelecimentos WHERE id = ?',
      [id]
    );
    return linhas[0] || null;
  },

  async criar(dados) {
    const [resultado] = await db.query(
      `INSERT INTO estabelecimentos
         (administrador_id, nome, descricao, endereco, latitude, longitude,
          telefone, whatsapp, horario_abertura, horario_fechamento)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
      [
        dados.administradorId, dados.nome, dados.descricao, dados.endereco,
        dados.latitude, dados.longitude, dados.telefone, dados.whatsapp,
        dados.horarioAbertura, dados.horarioFechamento,
      ]
    );
    return resultado.insertId;
  },

  async listarServicos(estabelecimentoId) {
    const [linhas] = await db.query(
      `SELECT id, nome, descricao, categoria, preco, duracao_minutos AS duracaoMin
       FROM servicos
       WHERE estabelecimento_id = ? AND ativo = TRUE
       ORDER BY preco`,
      [estabelecimentoId]
    );
    return linhas.map((s) => ({ ...s, preco: Number(s.preco) }));
  },

  async criarServico({ estabelecimentoId, nome, descricao, categoria, preco, duracaoMin }) {
    const [resultado] = await db.query(
      `INSERT INTO servicos (estabelecimento_id, nome, descricao, categoria, preco, duracao_minutos)
       VALUES (?, ?, ?, ?, ?, ?)`,
      [estabelecimentoId, nome, descricao, categoria, preco, duracaoMin]
    );
    return resultado.insertId;
  },
};

module.exports = EstabelecimentoModel;
