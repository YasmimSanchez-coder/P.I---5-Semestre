using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AgendAi.Views;

public partial class Notificacoes : ContentPage
{
    private static readonly (string Rotulo, string? Tipo)[] Filtros =
    [
        ("Todas", null),
        ("Agendamentos", "agendamento"),
        ("Promoções", "promocao"),
        ("Lembretes", "lembrete")
    ];

    private readonly NotificacaoService _notificacoes;

    private List<Notificacao> _todas = [];
    private string? _filtro;

    public Notificacoes(NotificacaoService notificacoes)
    {
        InitializeComponent();
        _notificacoes = notificacoes;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    // ---------------------------------------------------------------- eventos
    private async void OnVoltarClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnMarcarTodasTapped(object? sender, TappedEventArgs e)
    {
        if (_todas.All(n => n.Lida)) return;

        var resultado = await _notificacoes.MarcarTodasComoLidasAsync();
        if (!resultado.Sucesso)
        {
            await this.AlertarAsync("Erro", resultado.Erro ?? "Não foi possível atualizar.");
            return;
        }

        _todas = _todas.Select(n => n with { Lida = true }).ToList();
        Renderizar();
    }

    private async void MarcarComoLida(Notificacao notificacao)
    {
        if (notificacao.Lida) return;

        var indice = _todas.FindIndex(n => n.Id == notificacao.Id);
        if (indice < 0) return;

        _todas[indice] = notificacao with { Lida = true };
        Renderizar();

        await _notificacoes.MarcarComoLidaAsync(notificacao.Id);
    }

    private void SelecionarFiltro(string? tipo)
    {
        _filtro = tipo;
        Renderizar();
    }

    // --------------------------------------------------------------- carregar
    private async Task CarregarAsync()
    {
        Carregando.IsVisible = true;
        Carregando.IsRunning = true;

        var resultado = await _notificacoes.ListarAsync();

        Carregando.IsRunning = false;
        Carregando.IsVisible = false;

        if (!resultado.Sucesso)
        {
            ListaHost.Children.Clear();
            ListaHost.Children.Add(Componentes.CriarMensagemVazia(resultado.Erro ?? "Não foi possível carregar."));
            return;
        }

        _todas = resultado.Dados!.Itens;
        Renderizar();
    }

    // --------------------------------------------------------------------- UI
    private void Renderizar()
    {
        var naoLidas = _todas.Count(n => !n.Lida);
        ContadorBadge.IsVisible = naoLidas > 0;
        ContadorLabel.Text = naoLidas.ToString();
        ResumoLabel.Text = naoLidas == 0 ? "Tudo em dia" : naoLidas == 1 ? "1 não lida" : $"{naoLidas} não lidas";

        ChipsLayout.Children.Clear();
        foreach (var (rotulo, tipo) in Filtros)
            ChipsLayout.Children.Add(Componentes.CriarChip(rotulo, tipo == _filtro, () => SelecionarFiltro(tipo)));

        var visiveis = _todas.Where(n => _filtro is null || n.Tipo == _filtro).ToList();

        ListaHost.Children.Clear();
        if (visiveis.Count == 0)
        {
            ListaHost.Children.Add(Componentes.CriarMensagemVazia("Nenhuma notificação por aqui."));
            return;
        }

        AdicionarSecao("NOVAS", visiveis.Where(n => !n.Lida));
        AdicionarSecao("ANTERIORES", visiveis.Where(n => n.Lida));
    }

    private void AdicionarSecao(string titulo, IEnumerable<Notificacao> notificacoes)
    {
        var lista = notificacoes.ToList();
        if (lista.Count == 0) return;

        ListaHost.Children.Add(new Label { Text = titulo, Style = Tema.Estilo("Micro"), Margin = new Thickness(0, 6, 0, 0) });
        foreach (var notificacao in lista)
            ListaHost.Children.Add(CriarCartao(notificacao));
    }

    private Border CriarCartao(Notificacao n)
    {
        var fundoDoIcone = n.Tipo switch
        {
            "lembrete" => "DouradoFundo",
            "promocao" => "RoxoFundo",
            _ => "SuperficieAlta"
        };

        var icone = new Border
        {
            WidthRequest = 42,
            HeightRequest = 42,
            VerticalOptions = LayoutOptions.Start,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            BackgroundColor = Tema.Cor(fundoDoIcone),
            Content = new Label
            {
                Text = n.Icone,
                FontSize = 18,
                TextColor = Tema.Cor("Dourado"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };

        var cabecalho = new Grid
        {
            ColumnSpacing = 6,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        cabecalho.NaColuna(new Label { Text = n.Titulo, Style = Tema.Estilo("TituloCartao"), FontSize = 14 }, 0);
        cabecalho.NaColuna(new Label
        {
            Text = Formatacao.TempoDecorrido(n.Criado, DateTime.Now),
            Style = Tema.Estilo("Suave"),
            FontSize = 11,
            VerticalOptions = LayoutOptions.Center
        }, 1);
        if (!n.Lida)
        {
            cabecalho.NaColuna(new Border
            {
                WidthRequest = 8,
                HeightRequest = 8,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(4) },
                BackgroundColor = Tema.Cor("Dourado"),
                VerticalOptions = LayoutOptions.Center
            }, 2);
        }

        var textos = new VerticalStackLayout
        {
            Spacing = 4,
            Children = { cabecalho, new Label { Text = n.Mensagem, Style = Tema.Estilo("Suave"), FontSize = 13 } }
        };

        var grade = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }
        };
        grade.NaColuna(icone, 0);
        grade.NaColuna(textos, 1);

        var cartao = new Border
        {
            Style = Tema.Estilo("Cartao"),
            Stroke = Tema.Cor(n.Lida ? "Borda" : "DouradoEscuro"),
            Content = grade
        };
        cartao.AoTocar(() => MarcarComoLida(n));
        return cartao;
    }
}
