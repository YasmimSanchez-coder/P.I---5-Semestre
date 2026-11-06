using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AgendAi.Views;

public partial class MeusAgendamentos : ContentPage
{
    private const string Todos = "todos";
    private const string Proximos = "proximos";
    private const string Historico = "historico";

    private readonly AgendamentoService _agendamentos;
    private string _periodo = Todos;

    public MeusAgendamentos(AgendamentoService agendamentos)
    {
        InitializeComponent();
        _agendamentos = agendamentos;
        AtualizarAbas();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarAsync();
    }

    // ---------------------------------------------------------------- eventos
    private async void OnTodosClicked(object? sender, EventArgs e) => await TrocarPeriodoAsync(Todos);

    private async void OnProximosClicked(object? sender, EventArgs e) => await TrocarPeriodoAsync(Proximos);

    private async void OnHistoricoClicked(object? sender, EventArgs e) => await TrocarPeriodoAsync(Historico);

    private async Task TrocarPeriodoAsync(string periodo)
    {
        _periodo = periodo;
        AtualizarAbas();
        await CarregarAsync();
    }

    private async void Cancelar(Agendamento agendamento)
    {
        var confirmado = await this.ConfirmarAsync(
            "Cancelar agendamento",
            $"Deseja cancelar {agendamento.ServicoNome} em {agendamento.DataTexto} às {agendamento.HoraInicio}?");
        if (!confirmado) return;

        var resultado = await _agendamentos.CancelarAsync(agendamento.Id);
        if (!resultado.Sucesso)
        {
            await this.AlertarAsync("Erro", resultado.Erro ?? "Não foi possível cancelar.");
            return;
        }

        await CarregarAsync();
    }

    private async void Reagendar(Agendamento a) =>
        await Shell.Current.GoToAsync(
            $"{Rotas.Agendar}?id={a.EstabelecimentoId}&servicoId={a.ServicoId}&profissionalId={a.ProfissionalId}&reagendarId={a.Id}");

    private async void AgendarNovamente(Agendamento a) =>
        await Shell.Current.GoToAsync(
            $"{Rotas.Agendar}?id={a.EstabelecimentoId}&servicoId={a.ServicoId}&profissionalId={a.ProfissionalId}");

    private async void Avaliar(Agendamento agendamento)
    {
        string[] opcoes = ["5 - Excelente", "4 - Muito bom", "3 - Bom", "2 - Regular", "1 - Ruim"];
        var escolha = await this.EscolherAsync($"Nota para {agendamento.EstabelecimentoNome}", opcoes);
        if (escolha is null || !opcoes.Contains(escolha)) return;

        var nota = int.Parse(escolha[..1]);
        var comentario = await this.PerguntarAsync("Comentário", "Quer deixar um comentário? (opcional)");

        var resultado = await _agendamentos.AvaliarAsync(agendamento.Id, nota, comentario);
        if (!resultado.Sucesso)
        {
            await this.AlertarAsync("Erro", resultado.Erro ?? "Não foi possível avaliar.");
            return;
        }

        await this.AlertarAsync("Obrigado!", "Sua avaliação foi registrada.");
        await CarregarAsync();
    }

    // --------------------------------------------------------------- carregar
    private async Task CarregarAsync()
    {
        Carregando.IsVisible = true;
        Carregando.IsRunning = true;

        var resultado = await _agendamentos.MeusAsync(_periodo);

        Carregando.IsRunning = false;
        Carregando.IsVisible = false;

        ListaHost.Children.Clear();

        if (!resultado.Sucesso)
        {
            TotalLabel.Text = string.Empty;
            ListaHost.Children.Add(Componentes.CriarMensagemVazia(resultado.Erro ?? "Não foi possível carregar."));
            return;
        }

        var lista = resultado.Dados!;
        TotalLabel.Text = (lista.Count == 1 ? "1 agendamento" : $"{lista.Count} agendamentos")
                          + (_periodo == Todos ? " no total" : string.Empty);

        if (lista.Count == 0)
        {
            ListaHost.Children.Add(Componentes.CriarMensagemVazia("Nenhum agendamento por aqui."));
            return;
        }

        foreach (var agendamento in lista)
            ListaHost.Children.Add(CriarCartao(agendamento));
    }

    // --------------------------------------------------------------------- UI
    private void AtualizarAbas()
    {
        Componentes.EstilizarAba(TodosTab, _periodo == Todos);
        Componentes.EstilizarAba(ProximosTab, _periodo == Proximos);
        Componentes.EstilizarAba(HistoricoTab, _periodo == Historico);
    }

    private Border CriarCartao(Agendamento a)
    {
        // Topo: avatar, nome do salão, serviço e profissional, status
        var topo = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        topo.NaColuna(Componentes.CriarAvatar(a.EstabelecimentoNome, 48), 0);
        topo.NaColuna(new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = a.EstabelecimentoNome, Style = Tema.Estilo("TituloCartao") },
                new Label
                {
                    Text = a.ServicoEProfissional,
                    Style = Tema.Estilo("Suave"),
                    LineBreakMode = LineBreakMode.TailTruncation,
                    MaxLines = 1
                }
            }
        }, 1);
        topo.NaColuna(Componentes.CriarEtiquetaStatus(a.Status), 2);

        // Data, hora e preço
        var info = new Grid
        {
            ColumnSpacing = 14,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star)
            }
        };
        info.NaColuna(new Label { Text = $"📅 {a.DataTexto}", Style = Tema.Estilo("Texto"), FontSize = 13 }, 0);
        info.NaColuna(new Label { Text = $"🕐 {a.HoraInicio}", Style = Tema.Estilo("Texto"), FontSize = 13 }, 1);
        info.NaColuna(new Label
        {
            Text = a.PrecoTexto,
            Style = Tema.Estilo("TituloSecao"),
            TextColor = Tema.Cor("Dourado"),
            FontSize = 16,
            HorizontalOptions = LayoutOptions.End
        }, 2);

        var faixaDeInformacoes = new Border
        {
            BackgroundColor = Tema.Cor("SuperficieAlta"),
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
            Padding = new Thickness(12, 10),
            Content = info
        };

        var conteudo = new VerticalStackLayout { Spacing = 12, Children = { topo, faixaDeInformacoes } };
        conteudo.Children.Add(CriarAcoes(a));

        return new Border { Style = Tema.Estilo("Cartao"), Content = conteudo };
    }

    private Grid CriarAcoes(Agendamento a)
    {
        var acoes = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }
        };

        if (a.PodeCancelar)
        {
            acoes.NaColuna(Componentes.CriarBotao("Cancelar", "BotaoSecundario", () => Cancelar(a)), 0);
            acoes.NaColuna(Componentes.CriarBotao("Reagendar", "BotaoContorno", () => Reagendar(a)), 1);
            return acoes;
        }

        var novamente = Componentes.CriarBotao("Agendar Novamente", "BotaoContorno", () => AgendarNovamente(a));

        if (a.PodeAvaliar)
        {
            acoes.NaColuna(Componentes.CriarBotao("★ Avaliar", "BotaoSecundario", () => Avaliar(a)), 0);
            acoes.NaColuna(novamente, 1);
        }
        else
        {
            Grid.SetColumnSpan(novamente, 2);
            acoes.NaColuna(novamente, 0);
        }

        return acoes;
    }
}
