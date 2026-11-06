using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AgendAi.Models;

namespace AgendAi.Services;

/// <summary>Cliente HTTP único da API: envia o token, converte JSON e traduz erros em mensagens.</summary>
public class ApiClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SessionService _session;

    public ApiClient(HttpClient http, SessionService session)
    {
        _http = http;
        _session = session;
    }

    public Task<ApiResult<T>> GetAsync<T>(string url) => EnviarAsync<T>(HttpMethod.Get, url);

    public Task<ApiResult<T>> PostAsync<T>(string url, object corpo) => EnviarAsync<T>(HttpMethod.Post, url, corpo);

    public Task<ApiResult<T>> PatchAsync<T>(string url, object? corpo = null) => EnviarAsync<T>(HttpMethod.Patch, url, corpo);

    public Task<ApiResult<T>> DeleteAsync<T>(string url) => EnviarAsync<T>(HttpMethod.Delete, url);

    private async Task<ApiResult<T>> EnviarAsync<T>(HttpMethod metodo, string url, object? corpo = null)
    {
        try
        {
            using var requisicao = new HttpRequestMessage(metodo, url);

            if (_session.Token is { } token)
                requisicao.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            if (corpo is not null)
                requisicao.Content = JsonContent.Create(corpo, options: Json);

            using var resposta = await _http.SendAsync(requisicao);

            if (!resposta.IsSuccessStatusCode)
                return ApiResult<T>.Falha(await LerErroAsync(resposta));

            var dados = await resposta.Content.ReadFromJsonAsync<T>(Json);
            return dados is null
                ? ApiResult<T>.Falha("Resposta inválida do servidor.")
                : ApiResult<T>.Ok(dados);
        }
        catch (HttpRequestException)
        {
            return ApiResult<T>.Falha("Sem conexão com a API. Confira se ela está rodando (teste http://localhost:3000/saude no navegador).");
        }
        catch (TaskCanceledException)
        {
            return ApiResult<T>.Falha("O servidor demorou para responder.");
        }
        catch (JsonException)
        {
            return ApiResult<T>.Falha("Resposta inválida do servidor.");
        }
    }

    private static async Task<string> LerErroAsync(HttpResponseMessage resposta)
    {
        try
        {
            var erro = await resposta.Content.ReadFromJsonAsync<ErroResponse>(Json);
            if (erro?.Erro is { Length: > 0 } mensagem) return mensagem;
        }
        catch (JsonException)
        {
            // corpo não era JSON: cai na mensagem genérica
        }

        return $"Erro {(int)resposta.StatusCode} ao falar com o servidor.";
    }
}
