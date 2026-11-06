using System.Globalization;
using AgendAi.Helpers;

namespace AgendAi.Models;

/// <summary>Agendamento visto pelo cliente (tela "Meus Agendamentos").</summary>
public record Agendamento(
    int Id,
    string Data,
    string HoraInicio,
    string HoraFim,
    string Status,
    int ServicoId,
    string ServicoNome,
    decimal ServicoPreco,
    int ProfissionalId,
    string ProfissionalNome,
    int EstabelecimentoId,
    string EstabelecimentoNome,
    bool Avaliado)
{
    private DateTime Inicio =>
        DateTime.ParseExact($"{Data} {HoraInicio}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    public string DataTexto => Formatacao.DataCurta(Inicio);

    public string PrecoTexto => Formatacao.Moeda(ServicoPreco);

    public string ServicoEProfissional => $"{ServicoNome} · {ProfissionalNome}";

    public bool PodeCancelar => (Status is "pendente" or "confirmado") && Inicio > DateTime.Now;

    public bool PodeAvaliar => Status == "concluido" && !Avaliado;
}

public record CriarAgendamentoRequest(int ProfissionalId, int ServicoId, string Data, string HoraInicio);

public record ReagendarRequest(string Data, string HoraInicio);

public record AvaliarRequest(int AgendamentoId, int Nota, string? Comentario);

public record HorariosResponse(string Data, List<string> Horarios);
