using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AgendAi.Views;

public partial class AgendaProfissional : ContentPage
{
    private readonly AgendamentoService _agendamentos;
    private readonly SessionService _session;

    private bool _subtituloCarregado;

    public AgendaProfissional(AgendamentoService agendamentos, SessionService session)
    {
        InitializeComponent();
        _agendamentos = agendamentos;
        _session = session;

        Semana.DiaSelecionado += OnDiaSelecionado;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await CarregarSubtituloAsync();
        await CarregarAsync();
    }

    // ---------------------------------------------------------------- eventos
    private async void OnDiaSelecionado(object? sender, DateTime dia) => await CarregarAsync();

    private async void OnBloquearClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync($"{Rotas.BloquearHorario}?data={Formatacao.DataParaApi(Semana.Selecionado)}");

    private async void AbrirMenuDoAtendimento(AgendaItem item)
    {
        var opcoes = new List<string>();
        if (item.Status == "pendente") opcoes.Add("Confirmar");
        if (item.Status == "confirmado") opcoes.Add("Concluir atendimento");
        opcoes.Add("Cancelar atendimento");

        var escolha = await this.EscolherAsync($"{item.ClienteNome} · {item.HoraInicio}", [.. opcoes]);

        switch (escolha)
        {
            case "Confirmar":
                await ExecutarAsync(() => _agendamentos.ConfirmarAsync(item.Id));
                break;

            case "Concluir atendimento":
                await ExecutarAsync(() => _agendamentos.ConcluirAsync(item.Id));
                break;

            case "Cancelar atendimento":
                var confirmado = await this.ConfirmarAsync(
                    "Cancelar atendimento",
                    $"Cancelar {item.ServicoNome} de {item.ClienteNome} às {item.HoraInicio}? O cliente será avisado.");
                if (confirmado) await ExecutarAsync(() => _agendamentos.CancelarAsync(item.Id));
                break;
        }
    }

    private async void AbrirMenuDoBloqueio(Bloqueio bloqueio)
    {
        var escolha = await this.EscolherAsync($"Bloqueio · {bloqueio.HoraInicio} - {bloqueio.HoraFim}", "Remover bloqueio");
        if (escolha == "Remover bloqueio")
            await ExecutarAsync(() => _agendamentos.RemoverBloqueioAsync(bloqueio.Id));
    }

    /// <summary>Executa uma ação da agenda, mostra o erro se houver e recarrega o dia.</summary>
    private async Task ExecutarAsync(Func<Task<ApiResult<MensagemResponse>>> acao)
    {
        var resultado = await acao();
        if (!resultado.Sucesso)
        {
            await this.AlertarAsync("Erro", resultado.Erro ?? "Não foi possível concluir a ação.");
            return;
        }

        await CarregarAsync();
    }

    // --------------------------------------------------------------- carregar
    private async Task CarregarSubtituloAsync()
    {
        if (_subtituloCarregado) return;

        var resultado = await _agendamentos.MeusProfissionaisAsync();
        if (resultado.Sucesso)
        {
            var lista = resultado.Dados!;
            SubtituloLabel.Text = lista.Count == 1
                ? $"{lista[0].Nome} · {lista[0].Especialidade}"
                : $"{_session.Usuario?.Nome} · {lista.Count} profissionais";
            _subtituloCarregado = true;
        }
        else
        {
            SubtituloLabel.Text = _session.Usuario?.Nome;
        }
    }

    private async Task CarregarAsync()
    {
        Carregando.IsVisible = true;
        Carregando.IsRunning = true;

        var resultado = await _agendamentos.AgendaAsync(Formatacao.DataParaApi(Semana.Selecionado));

        Carregando.IsRunning = false;
        Carregando.IsVisible = false;

        if (!resultado.Sucesso)
        {
            await this.AlertarAsync("Erro", resultado.Erro ?? "Não foi possível carregar a agenda.");
            return;
        }

        var agenda = resultado.Dados!;
        AgendamentosLabel.Text = agenda.Resumo.Agendamentos.ToString();
        ReceitaLabel.Text = $"R$ {agenda.Resumo.ReceitaProjetada:0}";
        OcupacaoLabel.Text = $"{agenda.Resumo.OcupacaoPercentual}%";

        MontarLinhaDoTempo(agenda);
    }

    // ---------------------------------------------------------- linha do tempo
    private static int Minutos(string hhmm)
    {
        var partes = hhmm.Split(':');
        return int.Parse(partes[0]) * 60 + int.Parse(partes[1]);
    }

    private void MontarLinhaDoTempo(AgendaDia agenda)
    {
        LinhaDoTempoHost.Children.Clear();

        var entradas = new List<(int Inicio, int Fim, View Cartao)>();
        foreach (var item in agenda.Itens.Where(i => i.Status != "cancelado"))
            entradas.Add((Minutos(item.HoraInicio), Minutos(item.HoraFim), CriarCartaoDoAtendimento(item)));
        foreach (var bloqueio in agenda.Bloqueios)
            entradas.Add((Minutos(bloqueio.HoraInicio), Minutos(bloqueio.HoraFim), CriarCartaoDoBloqueio(bloqueio)));

        if (agenda.Expediente is null && entradas.Count == 0)
        {
            LinhaDoTempoHost.Children.Add(Componentes.CriarMensagemVazia("Sem expediente neste dia."));
            return;
        }

        // A linha do tempo cobre o expediente e também qualquer atendimento fora dele
        var primeiro = int.MaxValue;
        var ultimo = 0;
        if (agenda.Expediente is { } expediente)
        {
            primeiro = Minutos(expediente.Inicio);
            ultimo = Minutos(expediente.Fim);
        }
        foreach (var entrada in entradas)
        {
            primeiro = Math.Min(primeiro, entrada.Inicio);
            ultimo = Math.Max(ultimo, entrada.Fim);
        }

        for (var hora = primeiro / 60; hora * 60 < ultimo; hora++)
        {
            var inicioDaHora = hora * 60;
            var fimDaHora = inicioDaHora + 60;

            var iniciando = entradas
                .Where(x => x.Inicio >= inicioDaHora && x.Inicio < fimDaHora)
                .OrderBy(x => x.Inicio)
                .ToList();

            // Hora já coberta por um atendimento que começou antes: não repete a linha
            var coberta = iniciando.Count == 0 && entradas.Any(x => x.Inicio < inicioDaHora && x.Fim > inicioDaHora);
            if (coberta) continue;

            var conteudo = new VerticalStackLayout { Spacing = 8 };
            if (iniciando.Count == 0) conteudo.Children.Add(CriarDisponivel());
            else foreach (var entrada in iniciando) conteudo.Children.Add(entrada.Cartao);

            var linha = new Grid
            {
                ColumnSpacing = 12,
                ColumnDefinitions = { new ColumnDefinition(new GridLength(46)), new ColumnDefinition(GridLength.Star) }
            };
            linha.NaColuna(new Label
            {
                Text = $"{hora:00}:00",
                Style = Tema.Estilo("Suave"),
                FontSize = 12,
                VerticalOptions = LayoutOptions.Start,
                Margin = new Thickness(0, 12, 0, 0)
            }, 0);
            linha.NaColuna(conteudo, 1);
            LinhaDoTempoHost.Children.Add(linha);
        }
    }

    private static Border CriarDisponivel() => new()
    {
        Padding = new Thickness(14, 12),
        StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(12) },
        StrokeThickness = 1,
        Stroke = Tema.Cor("Borda"),
        StrokeDashArray = new DoubleCollection { 4, 4 },
        Content = new Label { Text = "Disponível", Style = Tema.Estilo("Suave") }
    };

    private View CriarCartaoDoAtendimento(AgendaItem item)
    {
        var (fundo, borda) = item.Status switch
        {
            "confirmado" => ("VerdeFundo", "Verde"),
            "pendente" => ("DouradoFundo", "DouradoEscuro"),
            _ => ("SuperficieAlta", "Borda")
        };

        var duracao = Minutos(item.HoraFim) - Minutos(item.HoraInicio);

        var textos = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = item.ClienteNome, Style = Tema.Estilo("TituloCartao"), FontSize = 15 },
                new Label { Text = item.ServicoNome, Style = Tema.Estilo("Suave") },
                new Label
                {
                    Text = $"⏱ {item.HoraInicio} - {item.HoraFim} · {duracao} min · {Formatacao.Status(item.Status)}",
                    Style = Tema.Estilo("Suave"),
                    FontSize = 11
                }
            }
        };

        var grade = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        grade.NaColuna(textos, 0);

        if (item.Status != "concluido")
            grade.NaColuna(CriarBotaoDeMenu(() => AbrirMenuDoAtendimento(item)), 1);

        return new Border
        {
            Padding = new Thickness(14, 12),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            StrokeThickness = 1,
            Stroke = Tema.Cor(borda),
            BackgroundColor = Tema.Cor(fundo),
            Content = grade
        };
    }

    private View CriarCartaoDoBloqueio(Bloqueio bloqueio)
    {
        var textos = new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = "Bloqueado", Style = Tema.Estilo("TituloCartao"), FontSize = 15, TextColor = Tema.Cor("Roxo") },
                new Label { Text = bloqueio.Motivo ?? "Horário bloqueado", Style = Tema.Estilo("Suave") },
                new Label { Text = $"⏱ {bloqueio.HoraInicio} - {bloqueio.HoraFim}", Style = Tema.Estilo("Suave"), FontSize = 11 }
            }
        };

        var grade = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        grade.NaColuna(textos, 0);
        grade.NaColuna(CriarBotaoDeMenu(() => AbrirMenuDoBloqueio(bloqueio)), 1);

        return new Border
        {
            Padding = new Thickness(14, 12),
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            StrokeThickness = 1,
            Stroke = Tema.Cor("Roxo"),
            BackgroundColor = Tema.Cor("RoxoFundo"),
            Content = grade
        };
    }

    private static Button CriarBotaoDeMenu(Action acao)
    {
        var botao = new Button
        {
            Text = "⋮",
            Style = Tema.Estilo("BotaoIcone"),
            WidthRequest = 34,
            HeightRequest = 34,
            VerticalOptions = LayoutOptions.Start
        };
        botao.Clicked += (_, _) => acao();
        return botao;
    }
}
