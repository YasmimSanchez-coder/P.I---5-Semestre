using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Layouts;

namespace AgendAi.Views;

public partial class Home : ContentPage
{
    // Posições decorativas (proporção X, Y) dos preços no cartão de mapa
    private static readonly (double X, double Y)[] PosicoesDosPins = [(0.12, 0.38), (0.42, 0.22), (0.68, 0.5), (0.3, 0.72)];

    private readonly EstabelecimentoService _estabelecimentos;
    private readonly NotificacaoService _notificacoes;
    private readonly LocalizacaoService _localizacao;
    private readonly SessionService _session;

    private string _categoria = "Todos";
    private List<Estabelecimento> _resultados = [];

    public Home(
        EstabelecimentoService estabelecimentos,
        NotificacaoService notificacoes,
        LocalizacaoService localizacao,
        SessionService session)
    {
        InitializeComponent();
        _estabelecimentos = estabelecimentos;
        _notificacoes = notificacoes;
        _localizacao = localizacao;
        _session = session;

        MontarChips();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_session.Usuario is { } usuario)
        {
            SaudacaoLabel.Text = $"{Formatacao.Saudacao(DateTime.Now)}, {Formatacao.PrimeiroNome(usuario.Nome)} ✦";
            AvatarLabel.Text = Formatacao.Inicial(usuario.Nome);
        }

        await _localizacao.ObterAsync();
        await Task.WhenAll(CarregarAsync(), AtualizarNotificacoesAsync());
    }

    // ---------------------------------------------------------------- eventos
    private async void OnBuscar(object? sender, EventArgs e) => await CarregarAsync();

    private async void OnNotificacoesClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(Rotas.Notificacoes);

    private void OnAvatarTapped(object? sender, TappedEventArgs e) => Navegacao.IrParaAba(3);

    private void OnVerTodosTapped(object? sender, TappedEventArgs e) => Navegacao.IrParaAba(1);

    private async void OnEstabelecimentoTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Estabelecimento estabelecimento) return;

        await Shell.Current.GoToAsync($"{Rotas.PerfilSalao}?id={estabelecimento.Id}");
    }

    private async void OnMapaCompletoClicked(object? sender, EventArgs e)
    {
        // Abre o aplicativo de mapas no salão mais próximo (ou no primeiro, se não há GPS)
        var alvo = _resultados
            .Where(x => x.Posicao is not null)
            .OrderBy(x => x.DistanciaKm ?? double.MaxValue)
            .FirstOrDefault();

        if (alvo?.Posicao is not { } posicao)
        {
            await this.AlertarAsync("Mapa", "Nenhum salão com localização cadastrada.");
            return;
        }

        try
        {
            await Map.Default.OpenAsync(posicao, new MapLaunchOptions { Name = alvo.Nome });
        }
        catch (Exception)
        {
            await this.AlertarAsync("Mapa", "Não foi possível abrir o aplicativo de mapas.");
        }
    }

    // --------------------------------------------------------------- carregar
    private async Task CarregarAsync()
    {
        Carregando.IsVisible = true;
        Carregando.IsRunning = true;

        var busca = BuscaEntry.Text?.Trim();
        var resultado = await _estabelecimentos.ListarAsync(
            busca, Categorias.ParaFiltro(_categoria), _localizacao.UltimaPosicao);

        Carregando.IsRunning = false;
        Carregando.IsVisible = false;

        if (!resultado.Sucesso)
        {
            MensagemLabel.Text = resultado.Erro;
            MensagemLabel.IsVisible = true;
            return;
        }

        _resultados = resultado.Dados!
            .OrderByDescending(x => x.NotaMedia)
            .ThenBy(x => x.DistanciaKm ?? double.MaxValue)
            .ToList();

        var filtrando = !string.IsNullOrWhiteSpace(busca) || _categoria != "Todos";
        TituloListaLabel.Text = filtrando ? "Resultados" : "Em Destaque";

        MensagemLabel.Text = "Nenhum salão encontrado para esta busca.";
        MensagemLabel.IsVisible = _resultados.Count == 0;

        Lista.ItemsSource = _resultados;
        AtualizarMapa();
    }

    private async Task AtualizarNotificacoesAsync()
    {
        var resultado = await _notificacoes.ListarAsync();
        var naoLidas = resultado.Sucesso ? resultado.Dados!.NaoLidas : 0;

        BadgeNotificacoes.IsVisible = naoLidas > 0;
        BadgeLabel.Text = naoLidas.ToString();
    }

    // -------------------------------------------------------------------- UI
    private void MontarChips()
    {
        ChipsLayout.Children.Clear();

        foreach (var categoria in Categorias.Todas)
        {
            var chip = Componentes.CriarChip(categoria, categoria == _categoria, () => SelecionarCategoria(categoria));
            ChipsLayout.Children.Add(chip);
        }
    }

    private async void SelecionarCategoria(string categoria)
    {
        _categoria = categoria;
        MontarChips();
        await CarregarAsync();
    }

    private void AtualizarMapa()
    {
        var total = _resultados.Count;
        var perto = _localizacao.UltimaPosicao is not null;
        MapaResumoLabel.Text = total == 1
            ? (perto ? "1 salão perto de você" : "1 salão cadastrado")
            : (perto ? $"{total} salões perto de você" : $"{total} salões cadastrados");

        PinsLayout.Children.Clear();
        for (var i = 0; i < Math.Min(_resultados.Count, PosicoesDosPins.Length); i++)
        {
            var (x, y) = PosicoesDosPins[i];
            var pin = new Border
            {
                Padding = new Thickness(8, 4),
                BackgroundColor = Tema.Cor("Fundo"),
                Stroke = Tema.Cor("Dourado"),
                StrokeThickness = 1,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(10) },
                Content = new Label
                {
                    Text = _resultados[i].PrecoMinimoTexto,
                    FontSize = 11,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = Tema.Cor("Dourado")
                }
            };
            AbsoluteLayout.SetLayoutBounds(pin, new Rect(x, y, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            AbsoluteLayout.SetLayoutFlags(pin, AbsoluteLayoutFlags.PositionProportional);
            PinsLayout.Children.Add(pin);
        }
    }
}
