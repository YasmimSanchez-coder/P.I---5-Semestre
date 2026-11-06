using AgendAi.Helpers;

namespace AgendAi.Models;

public record Estabelecimento(
    int Id,
    string Nome,
    string? Descricao,
    string? Endereco,
    double? Latitude,
    double? Longitude,
    string? Telefone,
    string? Whatsapp,
    string? HorarioAbertura,
    string? HorarioFechamento,
    double? PrecoMinimo,
    double NotaMedia,
    int TotalAvaliacoes,
    double? DistanciaKm)
{
    public string Inicial => Formatacao.Inicial(Nome);

    public string NotaTexto => TotalAvaliacoes == 0 ? "Novo" : $"★ {NotaMedia:0.0}";

    public string DistanciaTexto => DistanciaKm is { } km ? $"{km:0.0} km" : string.Empty;

    public string DistanciaOuEndereco => DistanciaKm is { } km ? $"{km:0.0} km" : Endereco ?? string.Empty;

    public string PrecoMinimoTexto => PrecoMinimo is { } preco ? $"R$ {preco:0}+" : string.Empty;

    public Location? Posicao =>
        Latitude is { } latitude && Longitude is { } longitude ? new Location(latitude, longitude) : null;
}

public record Servico(int Id, string Nome, string? Descricao, string? Categoria, decimal Preco, int DuracaoMin)
{
    public string PrecoTexto => Formatacao.Moeda(Preco);

    public string DuracaoTexto => $"{DuracaoMin} min";

    public string ResumoTexto => $"{Nome} · {DuracaoTexto} · {PrecoTexto}";
}

public record Profissional(int Id, string Nome, string? Especialidade, double NotaMedia, List<int> ServicosIds)
{
    public string NotaTexto => NotaMedia > 0 ? $"★ {NotaMedia:0.0}" : "Novo";
}

public record Avaliacao(int Id, int Nota, string? Comentario, string CriadoEm, string ClienteNome);

public record EstabelecimentoDetalhe(
    int Id,
    string Nome,
    string? Descricao,
    string? Endereco,
    double? Latitude,
    double? Longitude,
    string? Telefone,
    string? Whatsapp,
    string? HorarioAbertura,
    string? HorarioFechamento,
    double NotaMedia,
    int TotalAvaliacoes,
    List<Servico> Servicos,
    List<Profissional> Profissionais)
{
    public string Inicial => Formatacao.Inicial(Nome);

    public bool Destaque => TotalAvaliacoes > 0 && NotaMedia >= 4.5;

    public Location? Posicao =>
        Latitude is { } latitude && Longitude is { } longitude ? new Location(latitude, longitude) : null;

    /// <summary>Aberto/fechado agora, com o complemento ("até 18:00" ou "abre às 09:00").</summary>
    public (bool? Aberto, string Complemento) StatusAbertura(DateTime agora)
    {
        if (!TimeSpan.TryParse(HorarioAbertura, out var abre) ||
            !TimeSpan.TryParse(HorarioFechamento, out var fecha))
            return (null, string.Empty);

        var estaAberto = agora.TimeOfDay >= abre && agora.TimeOfDay < fecha;
        return estaAberto
            ? (true, $"até {HorarioFechamento}")
            : (false, $"abre às {HorarioAbertura}");
    }
}
