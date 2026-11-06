namespace AgendAi.Models;

/// <summary>Atendimento visto pelo profissional.</summary>
public record AgendaItem(
    int Id,
    string Data,
    string HoraInicio,
    string HoraFim,
    string Status,
    string ServicoNome,
    decimal ServicoPreco,
    string ClienteNome,
    string? ClienteTelefone,
    string ProfissionalNome);

public record Bloqueio(int Id, string HoraInicio, string HoraFim, string? Motivo, string ProfissionalNome);

public record ResumoAgenda(int Agendamentos, decimal ReceitaProjetada, int OcupacaoPercentual);

public record Expediente(string Inicio, string Fim);

public record AgendaDia(
    string Data,
    ResumoAgenda Resumo,
    Expediente? Expediente,
    List<AgendaItem> Itens,
    List<Bloqueio> Bloqueios);

/// <summary>Profissional que o usuário logado representa (para escolher onde bloquear horário).</summary>
public record ProfissionalDoUsuario(int Id, string Nome, string? Especialidade, string EstabelecimentoNome);

public record BloquearRequest(string Data, string HoraInicio, string HoraFim, string? Motivo);
