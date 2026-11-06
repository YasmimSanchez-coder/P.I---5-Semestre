using System.Globalization;

namespace AgendAi.Models;

public record Notificacao(int Id, string Tipo, string Titulo, string Mensagem, bool Lida, string CriadoEm)
{
    public DateTime Criado =>
        DateTime.ParseExact(CriadoEm, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

    public string Icone => Tipo switch
    {
        "lembrete" => "⏰",
        "promocao" => "✦",
        _ when Titulo.Contains("cancelado", StringComparison.OrdinalIgnoreCase) => "✕",
        _ when Titulo.Contains("Como foi", StringComparison.OrdinalIgnoreCase) => "★",
        _ => "📅"
    };
}

public record NotificacoesResponse(int NaoLidas, List<Notificacao> Itens);
