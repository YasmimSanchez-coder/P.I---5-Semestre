namespace AgendAi.Helpers;

/// <summary>
/// Atalhos para os diálogos do MAUI. Se a sua versão do MAUI marcar DisplayAlert como obsoleto,
/// troque por DisplayAlertAsync / DisplayActionSheetAsync somente aqui.
/// </summary>
#pragma warning disable CS0618
public static class PaginaExtensions
{
    public static Task AlertarAsync(this Page pagina, string titulo, string mensagem) =>
        pagina.DisplayAlert(titulo, mensagem, "OK");

    public static Task<bool> ConfirmarAsync(this Page pagina, string titulo, string mensagem) =>
        pagina.DisplayAlert(titulo, mensagem, "Sim", "Não");

    public static Task<string> EscolherAsync(this Page pagina, string titulo, params string[] opcoes) =>
        pagina.DisplayActionSheet(titulo, "Cancelar", null, opcoes);

    public static async Task<string?> PerguntarAsync(this Page pagina, string titulo, string mensagem) =>
        await pagina.DisplayPromptAsync(titulo, mensagem, "Enviar", "Pular", maxLength: 300);
}
#pragma warning restore CS0618
