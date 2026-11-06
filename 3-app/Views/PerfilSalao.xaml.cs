using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;

namespace AgendAi.Views;

[QueryProperty(nameof(EstabelecimentoId), "id")]
public partial class PerfilSalao : ContentPage
{
    private readonly EstabelecimentoService _estabelecimentos;
    private readonly LocalizacaoService _localizacao;

    private EstabelecimentoDetalhe? _detalhe;
    private List<Avaliacao>? _avaliacoes;
    private Servico? _servicoSelecionado;
    private bool _abaAvaliacoes;

    public int EstabelecimentoId { get; set; }

    public PerfilSalao(EstabelecimentoService estabelecimentos, LocalizacaoService localizacao)
    {
        InitializeComponent();
        _estabelecimentos = estabelecimentos;
        _localizacao = localizacao;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_detalhe is not null) return; // já carregado (voltou de outra tela)

        var resultado = await _estabelecimentos.ObterAsync(EstabelecimentoId);
        Carregando.IsRunning = false;
        Carregando.IsVisible = false;

        if (!resultado.Sucesso)
        {
            await this.AlertarAsync("Erro", resultado.Erro ?? "Não foi possível carregar.");
            await Shell.Current.GoToAsync("..");
            return;
        }

        _detalhe = resultado.Dados!;
        PreencherCabecalho(_detalhe);
        MostrarAba(avaliacoes: false);
        Conteudo.IsVisible = true;
    }

    // ---------------------------------------------------------------- eventos
    private async void OnVoltarClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private void OnServicosTapped(object? sender, TappedEventArgs e) => MostrarAba(avaliacoes: false);

    private void OnAvaliacoesTapped(object? sender, TappedEventArgs e) => MostrarAba(avaliacoes: true);

    private async void OnAgendarClicked(object? sender, EventArgs e)
    {
        var destino = $"{Rotas.Agendar}?id={EstabelecimentoId}";
        if (_servicoSelecionado is { } servico) destino += $"&servicoId={servico.Id}";

        await Shell.Current.GoToAsync(destino);
    }

    private async void OnComoChegarClicked(object? sender, EventArgs e)
    {
        if (_detalhe?.Posicao is not { } posicao)
        {
            await this.AlertarAsync("Localização", "Este estabelecimento não tem localização cadastrada.");
            return;
        }

        try
        {
            await Map.Default.OpenAsync(
                posicao,
                new MapLaunchOptions { Name = _detalhe.Nome, NavigationMode = NavigationMode.Driving });
        }
        catch (Exception)
        {
            await this.AlertarAsync("Mapa", "Não foi possível abrir o aplicativo de mapas.");
        }
    }

    private async void OnWhatsAppClicked(object? sender, EventArgs e)
    {
        if (_detalhe is null || string.IsNullOrWhiteSpace(_detalhe.Whatsapp))
        {
            await this.AlertarAsync("WhatsApp", "Este estabelecimento não informou um WhatsApp.");
            return;
        }

        var texto = Uri.EscapeDataString($"Olá! Vi o {_detalhe.Nome} no AgendAí e gostaria de uma informação.");

        try
        {
            await Launcher.Default.OpenAsync(new Uri($"https://wa.me/{_detalhe.Whatsapp}?text={texto}"));
        }
        catch (Exception)
        {
            await this.AlertarAsync("WhatsApp", "Não foi possível abrir o WhatsApp.");
        }
    }

    // --------------------------------------------------------------------- UI
    private void PreencherCabecalho(EstabelecimentoDetalhe detalhe)
    {
        NomeLabel.Text = detalhe.Nome;
        InicialLabel.Text = detalhe.Inicial;
        DestaqueBadge.IsVisible = detalhe.Destaque;

        NotaLabel.Text = detalhe.TotalAvaliacoes == 0 ? "Novo" : detalhe.NotaMedia.ToString("0.0");
        NotaLegendaLabel.Text = detalhe.TotalAvaliacoes == 1 ? "1 avaliação" : $"{detalhe.TotalAvaliacoes} avaliações";

        if (_localizacao.UltimaPosicao is { } minhaPosicao && detalhe.Posicao is { } posicaoDoSalao)
        {
            var km = Location.CalculateDistance(minhaPosicao, posicaoDoSalao, DistanceUnits.Kilometers);
            DistanciaLabel.Text = km.ToString("0.0");
            DistanciaLegendaLabel.Text = "km de distância";
        }
        else
        {
            DistanciaLabel.Text = "—";
            DistanciaLegendaLabel.Text = "distância";
        }

        var (aberto, complemento) = detalhe.StatusAbertura(DateTime.Now);
        StatusLabel.Text = aberto switch { true => "Aberto", false => "Fechado", _ => "—" };
        StatusLabel.TextColor = Tema.Cor(aberto switch { true => "Verde", false => "Erro", _ => "TextoSuave" });
        StatusLegendaLabel.Text = complemento;

        DescricaoLabel.Text = detalhe.Descricao;
        DescricaoLabel.IsVisible = !string.IsNullOrWhiteSpace(detalhe.Descricao);
        EnderecoLabel.Text = string.IsNullOrWhiteSpace(detalhe.Endereco) ? string.Empty : $"📍 {detalhe.Endereco}";
    }

    private async void MostrarAba(bool avaliacoes)
    {
        _abaAvaliacoes = avaliacoes;

        TabServicosLabel.TextColor = Tema.Cor(avaliacoes ? "TextoSuave" : "Dourado");
        TabAvaliacoesLabel.TextColor = Tema.Cor(avaliacoes ? "Dourado" : "TextoSuave");
        LinhaServicos.Color = avaliacoes ? Tema.Cor("Borda") : Tema.Cor("Dourado");
        LinhaAvaliacoes.Color = avaliacoes ? Tema.Cor("Dourado") : Tema.Cor("Borda");

        if (avaliacoes) await MontarAvaliacoesAsync();
        else MontarServicos();
    }

    private void MontarServicos()
    {
        ConteudoAba.Children.Clear();

        if (_detalhe is null || _detalhe.Servicos.Count == 0)
        {
            ConteudoAba.Children.Add(Componentes.CriarMensagemVazia("Nenhum serviço cadastrado."));
            return;
        }

        foreach (var servico in _detalhe.Servicos)
        {
            var selecionado = servico.Id == _servicoSelecionado?.Id;
            ConteudoAba.Children.Add(Componentes.CriarLinhaServico(servico, selecionado, () => AlternarServico(servico)));
        }
    }

    private void AlternarServico(Servico servico)
    {
        _servicoSelecionado = servico.Id == _servicoSelecionado?.Id ? null : servico;
        MontarServicos();
    }

    private async Task MontarAvaliacoesAsync()
    {
        ConteudoAba.Children.Clear();

        if (_avaliacoes is null)
        {
            var resultado = await _estabelecimentos.AvaliacoesAsync(EstabelecimentoId);
            if (!_abaAvaliacoes) return; // o usuário voltou para Serviços durante a busca

            if (!resultado.Sucesso)
            {
                ConteudoAba.Children.Add(Componentes.CriarMensagemVazia(resultado.Erro ?? "Não foi possível carregar."));
                return;
            }

            _avaliacoes = resultado.Dados!;
        }

        if (_avaliacoes.Count == 0)
        {
            ConteudoAba.Children.Add(Componentes.CriarMensagemVazia("Ainda não há avaliações."));
            return;
        }

        foreach (var avaliacao in _avaliacoes)
            ConteudoAba.Children.Add(Componentes.CriarLinhaAvaliacao(avaliacao));
    }
}
