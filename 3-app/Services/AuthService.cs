using AgendAi.Models;

namespace AgendAi.Services;

public class AuthService
{
    private readonly ApiClient _api;
    private readonly SessionService _session;

    public AuthService(ApiClient api, SessionService session)
    {
        _api = api;
        _session = session;
    }

    public async Task<ApiResult<LoginResponse>> LoginAsync(string email, string senha)
    {
        var resultado = await _api.PostAsync<LoginResponse>("api/auth/login", new LoginRequest(email, senha));
        if (resultado.Sucesso) _session.Iniciar(resultado.Dados!);
        return resultado;
    }

    public async Task<ApiResult<LoginResponse>> CadastrarAsync(CadastroRequest pedido)
    {
        var resultado = await _api.PostAsync<LoginResponse>("api/auth/cadastro", pedido);
        if (resultado.Sucesso) _session.Iniciar(resultado.Dados!);
        return resultado;
    }
}
