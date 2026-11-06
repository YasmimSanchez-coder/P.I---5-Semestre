using AgendAi.Models;

namespace AgendAi.Services;

public class NotificacaoService
{
    private readonly ApiClient _api;

    public NotificacaoService(ApiClient api)
    {
        _api = api;
    }

    public Task<ApiResult<NotificacoesResponse>> ListarAsync() =>
        _api.GetAsync<NotificacoesResponse>("api/notificacoes");

    public Task<ApiResult<MensagemResponse>> MarcarComoLidaAsync(int id) =>
        _api.PatchAsync<MensagemResponse>($"api/notificacoes/{id}/lida");

    public Task<ApiResult<MensagemResponse>> MarcarTodasComoLidasAsync() =>
        _api.PatchAsync<MensagemResponse>("api/notificacoes/marcar-todas");
}
