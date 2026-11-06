/** Traduz erros comuns de conexão com o MySQL em dicas para quem está configurando o projeto. */
function explicarErroDeBanco(erro) {
  const banco = process.env.DB_NAME || 'agendai';

  switch (erro && erro.code) {
    case 'ECONNREFUSED':
      return 'Não consegui falar com o MySQL. Ele está instalado e ligado? Confira DB_HOST e DB_PORT no arquivo .env.';
    case 'ENOTFOUND':
      return 'O endereço do MySQL (DB_HOST no arquivo .env) não foi encontrado.';
    case 'ER_ACCESS_DENIED_ERROR':
      return 'Usuário ou senha do MySQL incorretos. Confira DB_USER e DB_PASSWORD no arquivo .env.';
    case 'ER_BAD_DB_ERROR':
      return `O banco "${banco}" não existe. Importe o arquivo 1-banco-de-dados/agendai.sql no MySQL.`;
    case 'ER_NO_SUCH_TABLE':
      return 'O banco existe, mas está sem as tabelas. Importe o arquivo 1-banco-de-dados/agendai.sql no MySQL.';
    default:
      return erro && erro.message ? erro.message : 'Erro desconhecido ao falar com o MySQL.';
  }
}

module.exports = { explicarErroDeBanco };
