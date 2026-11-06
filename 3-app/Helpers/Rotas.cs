namespace AgendAi.Helpers;

/// <summary>Rotas de navegação do Shell, em um só lugar.</summary>
public static class Rotas
{
    // Absolutas (trocam o "grupo" de telas)
    public const string Login = "//Login";
    public const string Cliente = "//Cliente";
    public const string Profissional = "//Profissional";

    // Telas empilhadas (têm botão de voltar)
    public const string PerfilSalao = "salao";
    public const string Agendar = "agendar";
    public const string BloquearHorario = "bloquear";
    public const string Notificacoes = "notificacoes";
}
