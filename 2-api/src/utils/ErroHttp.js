/** Erro com status HTTP, tratado de forma central em middlewares/errorHandler.js */
class ErroHttp extends Error {
  constructor(status, mensagem) {
    super(mensagem);
    this.status = status;
  }
}

module.exports = ErroHttp;
