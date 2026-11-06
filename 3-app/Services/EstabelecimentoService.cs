using System.Globalization;
using AgendAi.Models;

namespace AgendAi.Services;

public class EstabelecimentoService
{
    private readonly ApiClient _api;

    public EstabelecimentoService(ApiClient api)
    {
        _api = api;
    }

    /// <param name="categoria">Cabelo, Unhas, Estética, Barba... (null = todas)</param>
    public Task<ApiResult<List<Estabelecimento>>> ListarAsync(string? busca, string? categoria, Location? posicao)
    {
        var filtros = new List<string>();

        if (!string.IsNullOrWhiteSpace(busca))
            filtros.Add($"busca={Uri.EscapeDataString(busca)}");

        if (!string.IsNullOrWhiteSpace(categoria))
            filtros.Add($"categoria={Uri.EscapeDataString(categoria)}");

        if (posicao is not null)
        {
            filtros.Add($"latitude={posicao.Latitude.ToString(CultureInfo.InvariantCulture)}");
            filtros.Add($"longitude={posicao.Longitude.ToString(CultureInfo.InvariantCulture)}");
        }

        var url = filtros.Count == 0
            ? "api/estabelecimentos"
            : $"api/estabelecimentos?{string.Join('&', filtros)}";

        return _api.GetAsync<List<Estabelecimento>>(url);
    }

    public Task<ApiResult<EstabelecimentoDetalhe>> ObterAsync(int id) =>
        _api.GetAsync<EstabelecimentoDetalhe>($"api/estabelecimentos/{id}");

    public Task<ApiResult<List<Avaliacao>>> AvaliacoesAsync(int id) =>
        _api.GetAsync<List<Avaliacao>>($"api/estabelecimentos/{id}/avaliacoes");
}
