using AgendAi.Services;
using AgendAi.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgendAi;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        RegistrarServicos(builder.Services);
        RegistrarTelas(builder.Services);

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }

    private static void RegistrarServicos(IServiceCollection servicos)
    {
        servicos.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(ApiConfig.BaseUrl),
            Timeout = TimeSpan.FromSeconds(10)
        });

        servicos.AddSingleton<SessionService>();
        servicos.AddSingleton<ApiClient>();
        servicos.AddSingleton<AuthService>();
        servicos.AddSingleton<EstabelecimentoService>();
        servicos.AddSingleton<AgendamentoService>();
        servicos.AddSingleton<NotificacaoService>();
        servicos.AddSingleton<LocalizacaoService>();
    }

    private static void RegistrarTelas(IServiceCollection servicos)
    {
        servicos.AddTransient<Login>();
        servicos.AddTransient<Home>();
        servicos.AddTransient<Buscar>();
        servicos.AddTransient<PerfilSalao>();
        servicos.AddTransient<Agendar>();
        servicos.AddTransient<MeusAgendamentos>();
        servicos.AddTransient<AgendaProfissional>();
        servicos.AddTransient<BloquearHorario>();
        servicos.AddTransient<Notificacoes>();
        servicos.AddTransient<Perfil>();
    }
}
