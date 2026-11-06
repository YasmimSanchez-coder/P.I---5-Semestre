namespace AgendAi.Services;

public record ApiResult<T>(bool Sucesso, T? Dados, string? Erro)
{
    public static ApiResult<T> Ok(T dados) => new(true, dados, null);

    public static ApiResult<T> Falha(string erro) => new(false, default, erro);
}
