using AgendAi.Helpers;
using AgendAi.Views;

namespace AgendAi;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Telas que abrem por cima das abas (com botão de voltar)
        Routing.RegisterRoute(Rotas.PerfilSalao, typeof(PerfilSalao));
        Routing.RegisterRoute(Rotas.Agendar, typeof(Agendar));
        Routing.RegisterRoute(Rotas.BloquearHorario, typeof(BloquearHorario));
        Routing.RegisterRoute(Rotas.Notificacoes, typeof(Notificacoes));
    }
}
