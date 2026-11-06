using AgendAi.Models;

namespace AgendAi.Services;

public class AgendamentoService
{
    private readonly ApiClient _api;

    public AgendamentoService(ApiClient api)
    {
        _api = api;
    }

    // ---------------------------------------------------------------- cliente
    /// <param name="ignorarAgendamentoId">No reagendamento, o horário atual conta como livre.</param>
    public Task<ApiResult<HorariosResponse>> HorariosDisponiveisAsync(
        int profissionalId, int servicoId, string data, int? ignorarAgendamentoId = null)
    {
        var url = $"api/profissionais/{profissionalId}/horarios-disponiveis?servicoId={servicoId}&data={data}";
        if (ignorarAgendamentoId is { } id) url += $"&ignorarAgendamentoId={id}";
        return _api.GetAsync<HorariosResponse>(url);
    }

    public Task<ApiResult<CriarResponse>> CriarAsync(CriarAgendamentoRequest pedido) =>
        _api.PostAsync<CriarResponse>("api/agendamentos", pedido);

    public Task<ApiResult<MensagemResponse>> ReagendarAsync(int id, string data, string horaInicio) =>
        _api.PatchAsync<MensagemResponse>($"api/agendamentos/{id}/reagendar", new ReagendarRequest(data, horaInicio));

    /// <param name="periodo">"todos", "proximos" ou "historico"</param>
    public Task<ApiResult<List<Agendamento>>> MeusAsync(string periodo) =>
        _api.GetAsync<List<Agendamento>>($"api/agendamentos/meus?periodo={periodo}");

    public Task<ApiResult<CriarResponse>> AvaliarAsync(int agendamentoId, int nota, string? comentario) =>
        _api.PostAsync<CriarResponse>("api/avaliacoes", new AvaliarRequest(agendamentoId, nota, comentario));

    public Task<ApiResult<MensagemResponse>> CancelarAsync(int id) =>
        _api.PatchAsync<MensagemResponse>($"api/agendamentos/{id}/cancelar");

    // ----------------------------------------------------------- profissional
    public Task<ApiResult<AgendaDia>> AgendaAsync(string data) =>
        _api.GetAsync<AgendaDia>($"api/agendamentos/agenda?data={data}");

    public Task<ApiResult<MensagemResponse>> ConfirmarAsync(int id) =>
        _api.PatchAsync<MensagemResponse>($"api/agendamentos/{id}/confirmar");

    public Task<ApiResult<MensagemResponse>> ConcluirAsync(int id) =>
        _api.PatchAsync<MensagemResponse>($"api/agendamentos/{id}/concluir");

    public Task<ApiResult<List<ProfissionalDoUsuario>>> MeusProfissionaisAsync() =>
        _api.GetAsync<List<ProfissionalDoUsuario>>("api/profissionais/meus");

    public Task<ApiResult<CriarResponse>> BloquearAsync(int profissionalId, BloquearRequest pedido) =>
        _api.PostAsync<CriarResponse>($"api/profissionais/{profissionalId}/bloqueios", pedido);

    public Task<ApiResult<MensagemResponse>> RemoverBloqueioAsync(int bloqueioId) =>
        _api.DeleteAsync<MensagemResponse>($"api/profissionais/bloqueios/{bloqueioId}");
}
