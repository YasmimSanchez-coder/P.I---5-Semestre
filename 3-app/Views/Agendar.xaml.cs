using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;
using Microsoft.Maui.Controls.Shapes;

namespace AgendAi.Views;

[QueryProperty(nameof(EstabelecimentoId), "id")]
[QueryProperty(nameof(ServicoIdInicial), "servicoId")]
[QueryProperty(nameof(ProfissionalIdInicial), "profissionalId")]
[QueryProperty(nameof(ReagendarId), "reagendarId")]
public partial class Agendar : ContentPage
{
    private readonly EstabelecimentoService _estabelecimentos;
    private readonly AgendamentoService _agendamentos;

    private EstabelecimentoDetalhe? _detalhe;
    private Servico? _servico;
    private Profissional? _profissional;
    private bool _trocandoServico;
    private List<string> _horarios = [];
    private string? _hora;

    public int EstabelecimentoId { get; set; }
    public int ServicoIdInicial { get; set; }
    public int ProfissionalIdInicial { get; set; }

    /// <summary>Maior que zero quando a tela está reagendando um agendamento existente.</summary>
    public int ReagendarId { get; set; }

    private bool Reagendando => ReagendarId > 0;

    private DateTime Dia => Semana.Selecionado;

    public Agendar(EstabelecimentoService estabelecimentos, AgendamentoService agendamentos)
    {
        InitializeComponent();
        _estabelecimentos = estabelecimentos;
        _agendamentos = agendamentos;

        Semana.DiaSelecionado += OnDiaSelecionado;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_detalhe is not null) return; // já carregado (voltou de outra tela)

        var resultado = await _estabelecimentos.ObterAsync(EstabelecimentoId);
        if (!resultado.Sucesso)
        {
            await this.AlertarAsync("Erro", resultado.Erro ?? "Não foi possível carregar.");
            await Shell.Current.GoToAsync("..");
            return;
        }

        _detalhe = resultado.Dados!;
        TituloLabel.Text = Reagendando ? "Reagendar Horário" : "Agendar Horário";

        // Escolhas vindas da tela anterior (serviço marcado no perfil do salão, ou reagendamento)
        _servico = _detalhe.Servicos.FirstOrDefault(s => s.Id == ServicoIdInicial);
        _profissional = _servico is null
            ? null
            : _detalhe.Profissionais.FirstOrDefault(p => p.Id == ProfissionalIdInicial && p.ServicosIds.Contains(_servico.Id));

        Renderizar();
        await CarregarHorariosAsync();
    }

    // ---------------------------------------------------------------- eventos
    private async void OnVoltarClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnDiaSelecionado(object? sender, DateTime dia) => await CarregarHorariosAsync();

    private async void EscolherServico(Servico servico)
    {
        _servico = servico;
        _trocandoServico = false;

        // O profissional escolhido só vale se realiza o novo serviço
        if (_profissional is not null && !_profissional.ServicosIds.Contains(servico.Id))
            _profissional = null;

        Renderizar();
        await CarregarHorariosAsync();
    }

    private async void EscolherProfissional(Profissional profissional)
    {
        _profissional = profissional;
        Renderizar();
        await CarregarHorariosAsync();
    }

    private void SelecionarHora(string hora)
    {
        _hora = hora;
        MontarHorarios();
        AtualizarBotao();
    }

    private async void OnConfirmarClicked(object? sender, EventArgs e)
    {
        if (_servico is null || _profissional is null || _hora is null) return;

        var data = Formatacao.DataParaApi(Dia);
        var hora = _hora;

        ConfirmarButton.IsEnabled = false;
        Carregando.IsVisible = true;
        Carregando.IsRunning = true;

        var resultado = Reagendando
            ? await _agendamentos.ReagendarAsync(ReagendarId, data, hora)
            : (await _agendamentos.CriarAsync(new CriarAgendamentoRequest(_profissional.Id, _servico.Id, data, hora)))
                .Mensagem();

        Carregando.IsRunning = false;
        Carregando.IsVisible = false;

        if (!resultado.Sucesso)
        {
            await this.AlertarAsync("Não foi possível agendar", resultado.Erro ?? "Tente novamente.");
            await CarregarHorariosAsync(); // o horário pode ter sido ocupado por outra pessoa
            return;
        }

        await this.AlertarAsync(
            Reagendando ? "Horário alterado!" : "Agendamento realizado!",
            $"{_servico.Nome} com {_profissional.Nome}\n{Formatacao.DataCurta(Dia)} às {hora}.\n\nAguarde a confirmação do profissional.");

        await Shell.Current.GoToAsync(Rotas.Cliente);
        Navegacao.IrParaAba(2); // aba Agenda
    }

    // --------------------------------------------------------------- horários
    private async Task CarregarHorariosAsync()
    {
        _hora = null;
        _horarios = [];
        MontarHorarios();
        AtualizarBotao();

        if (_servico is null || _profissional is null) return;

        var servico = _servico;
        var profissional = _profissional;
        var dia = Dia;

        MensagemHorariosLabel.Text = "Buscando horários...";
        var resultado = await _agendamentos.HorariosDisponiveisAsync(
            profissional.Id, servico.Id, Formatacao.DataParaApi(dia), Reagendando ? ReagendarId : (int?)null);

        // A escolha pode ter mudado enquanto a busca rodava
        if (!ReferenceEquals(_servico, servico) || !ReferenceEquals(_profissional, profissional) || Dia != dia) return;

        if (!resultado.Sucesso)
        {
            MensagemHorariosLabel.Text = resultado.Erro;
            return;
        }

        _horarios = resultado.Dados!.Horarios;
        MensagemHorariosLabel.Text = _horarios.Count == 0 ? "Sem horários livres neste dia." : string.Empty;
        MontarHorarios();
    }

    // --------------------------------------------------------------------- UI
    private void Renderizar()
    {
        SubtituloLabel.Text = MontarSubtitulo();

        SecaoServico.IsVisible = !Reagendando;
        SecaoProfissional.IsVisible = !Reagendando && _servico is not null;
        SecaoDataHora.IsVisible = _servico is not null && _profissional is not null;

        MontarServicos();
        MontarProfissionais();
        AtualizarStepper();
        AtualizarBotao();
    }

    private string MontarSubtitulo()
    {
        var partes = new List<string?> { _detalhe?.Nome, _servico?.Nome };
        if (Reagendando) partes.Add(_profissional?.Nome);
        return string.Join(" · ", partes.Where(p => !string.IsNullOrEmpty(p)));
    }

    private void MontarServicos()
    {
        ServicoHost.Children.Clear();
        if (_detalhe is null) return;

        if (_servico is not null && !_trocandoServico)
        {
            ServicoHost.Children.Add(CriarResumoDoServico(_servico));
            return;
        }

        foreach (var servico in _detalhe.Servicos)
        {
            var selecionado = servico.Id == _servico?.Id;
            ServicoHost.Children.Add(Componentes.CriarLinhaServico(servico, selecionado, () => EscolherServico(servico)));
        }
    }

    private Border CriarResumoDoServico(Servico servico)
    {
        var grade = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        grade.NaColuna(new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                new Label { Text = servico.Nome, Style = Tema.Estilo("TituloCartao") },
                new Label { Text = $"{servico.DuracaoTexto} · {servico.PrecoTexto}", Style = Tema.Estilo("Suave") }
            }
        }, 0);
        grade.NaColuna(new Label { Text = "Trocar", Style = Tema.Estilo("TextoDourado"), VerticalOptions = LayoutOptions.Center }, 1);

        var resumo = new Border { Style = Tema.Estilo("Cartao"), Stroke = Tema.Cor("Dourado"), Content = grade };
        resumo.AoTocar(() =>
        {
            _trocandoServico = true;
            MontarServicos();
        });
        return resumo;
    }

    private void MontarProfissionais()
    {
        ProfissionaisHost.Children.Clear();
        if (_detalhe is null || _servico is null) return;

        var disponiveis = _detalhe.Profissionais.Where(p => p.ServicosIds.Contains(_servico.Id)).ToList();
        if (disponiveis.Count == 0)
        {
            ProfissionaisHost.Children.Add(Componentes.CriarMensagemVazia("Nenhum profissional realiza este serviço."));
            return;
        }

        foreach (var profissional in disponiveis)
        {
            var selecionado = profissional.Id == _profissional?.Id;
            ProfissionaisHost.Children.Add(
                Componentes.CriarCartaoProfissional(profissional, selecionado, () => EscolherProfissional(profissional)));
        }
    }

    private void MontarHorarios()
    {
        HorariosGrid.Children.Clear();
        HorariosGrid.RowDefinitions.Clear();

        for (var i = 0; i < _horarios.Count; i++)
        {
            var hora = _horarios[i];
            if (i % 3 == 0) HorariosGrid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            var botao = Componentes.CriarHorario(hora, hora == _hora, () => SelecionarHora(hora));
            Grid.SetRow(botao, i / 3);
            Grid.SetColumn(botao, i % 3);
            HorariosGrid.Children.Add(botao);
        }
    }

    private void AtualizarStepper()
    {
        var etapa = _servico is null ? 1 : _profissional is null ? 2 : 3;

        StepperHost.Children.Clear();
        for (var numero = 1; numero <= 3; numero++)
        {
            StepperHost.NaColuna(CriarPasso(numero, etapa), (numero - 1) * 2);

            if (numero < 3)
            {
                var linha = new BoxView
                {
                    HeightRequest = 1,
                    VerticalOptions = LayoutOptions.Center,
                    Color = Tema.Cor(numero < etapa ? "Dourado" : "Borda")
                };
                StepperHost.NaColuna(linha, numero * 2 - 1);
            }
        }

        PassoLegendaLabel.Text = etapa switch
        {
            1 => "PASSO 1 DE 3 · SERVIÇO",
            2 => "PASSO 2 DE 3 · PROFISSIONAL",
            _ => "PASSO 3 DE 3 · DATA & HORA"
        };
    }

    private static Border CriarPasso(int numero, int etapaAtual)
    {
        var concluido = numero < etapaAtual;
        var destacado = concluido || numero == etapaAtual;

        return new Border
        {
            WidthRequest = 28,
            HeightRequest = 28,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = new CornerRadius(14) },
            BackgroundColor = Tema.Cor(destacado ? "Dourado" : "SuperficieAlta"),
            Content = new Label
            {
                Text = concluido ? "✓" : numero.ToString(),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Tema.Cor(destacado ? "Fundo" : "TextoMudo"),
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            }
        };
    }

    private void AtualizarBotao()
    {
        var pronto = _servico is not null && _profissional is not null && _hora is not null;

        ConfirmarButton.IsEnabled = pronto;
        ConfirmarButton.Style = Tema.Estilo(pronto ? "BotaoPrimario" : "BotaoDesativado");
        ConfirmarButton.Text = pronto
            ? $"{(Reagendando ? "Reagendar" : "Confirmar")} {_hora} · {Formatacao.DiaMes(Dia)}"
            : "Selecione um horário";
    }
}

internal static class ResultadoExtensions
{
    /// <summary>Converte o resultado de "criar" no mesmo formato do resultado de "reagendar".</summary>
    public static ApiResult<MensagemResponse> Mensagem(this ApiResult<CriarResponse> resultado) =>
        resultado.Sucesso
            ? ApiResult<MensagemResponse>.Ok(new MensagemResponse(resultado.Dados!.Mensagem))
            : ApiResult<MensagemResponse>.Falha(resultado.Erro ?? "Erro desconhecido.");
}
