using AgendAi.Helpers;
using AgendAi.Services;

namespace AgendAi.Views;

public partial class Perfil : ContentPage
{
    private readonly SessionService _session;
    private readonly NotificacaoService _notificacoes;

    public Perfil(SessionService session, NotificacaoService notificacoes)
    {
        InitializeComponent();
        _session = session;
        _notificacoes = notificacoes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_session.Usuario is not { } usuario) return;

        AvatarLabel.Text = Formatacao.Inicial(usuario.Nome);
        NomeLabel.Text = usuario.Nome;
        EmailLabel.Text = usuario.Email;
        TipoLabel.Text = usuario.Tipo switch
        {
            "cliente" => "CLIENTE",
            "profissional" => "PROFISSIONAL",
            "administrador" => "ADMINISTRADOR",
            _ => usuario.Tipo.ToUpperInvariant()
        };

        var resultado = await _notificacoes.ListarAsync();
        var naoLidas = resultado.Sucesso ? resultado.Dados!.NaoLidas : 0;
        BadgeNotificacoes.IsVisible = naoLidas > 0;
        BadgeLabel.Text = naoLidas.ToString();
    }

    private async void OnNotificacoesTapped(object? sender, TappedEventArgs e) =>
        await Shell.Current.GoToAsync(Rotas.Notificacoes);

    private async void OnSairClicked(object? sender, EventArgs e)
    {
        _session.Encerrar();
        await Shell.Current.GoToAsync(Rotas.Login);
    }
}
