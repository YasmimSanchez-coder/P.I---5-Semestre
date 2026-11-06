using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;

namespace AgendAi.Views;

public partial class Buscar : ContentPage
{
    private const string PorProximidade = "Mais próximos";
    private const string PorAvaliacao = "Melhor avaliados";
    private const string PorPreco = "Menor preço";

    private readonly EstabelecimentoService _estabelecimentos;
    private readonly LocalizacaoService _localizacao;

    private string _categoria = "Todos";
    private string _ordem = PorProximidade;
    private List<Estabelecimento> _resultados = [];

    public Buscar(EstabelecimentoService estabelecimentos, LocalizacaoService localizacao)
    {
        InitializeComponent();
        _estabelecimentos = estabelecimentos;
        _localizacao = localizacao;

        MontarChips();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _localizacao.ObterAsync();
        await CarregarAsync();
    }

    private async void OnBuscar(object? sender, EventArgs e) => await CarregarAsync();

    private async void OnEstabelecimentoTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as BindableObject)?.BindingContext is not Estabelecimento estabelecimento) return;

        await Shell.Current.GoToAsync($"{Rotas.PerfilSalao}?id={estabelecimento.Id}");
    }

    private async void OnOrdenarClicked(object? sender, EventArgs e)
    {
        var escolha = await this.EscolherAsync("Ordenar por", PorProximidade, PorAvaliacao, PorPreco);
        if (escolha is not (PorProximidade or PorAvaliacao or PorPreco)) return;

        _ordem = escolha;
        Exibir();
    }

    private async void SelecionarCategoria(string categoria)
    {
        _categoria = categoria;
        MontarChips();
        await CarregarAsync();
    }

    private void MontarChips()
    {
        ChipsLayout.Children.Clear();

        foreach (var categoria in Categorias.Todas)
        {
            var chip = Componentes.CriarChip(categoria, categoria == _categoria, () => SelecionarCategoria(categoria));
            ChipsLayout.Children.Add(chip);
        }
    }

    private async Task CarregarAsync()
    {
        Carregando.IsVisible = true;
        Carregando.IsRunning = true;

        var resultado = await _estabelecimentos.ListarAsync(
            BuscaEntry.Text?.Trim(), Categorias.ParaFiltro(_categoria), _localizacao.UltimaPosicao);

        Carregando.IsRunning = false;
        Carregando.IsVisible = false;

        if (!resultado.Sucesso)
        {
            Lista.ItemsSource = null;
            ResumoLabel.Text = resultado.Erro;
            return;
        }

        _resultados = resultado.Dados!;
        Exibir();
    }

    private void Exibir()
    {
        IEnumerable<Estabelecimento> ordenados = _ordem switch
        {
            PorAvaliacao => _resultados.OrderByDescending(x => x.NotaMedia),
            PorPreco => _resultados.OrderBy(x => x.PrecoMinimo ?? double.MaxValue),
            _ => _resultados.OrderBy(x => x.DistanciaKm ?? double.MaxValue).ThenByDescending(x => x.NotaMedia)
        };

        Lista.ItemsSource = ordenados.ToList();
        ResumoLabel.Text = $"{_resultados.Count} resultado(s) · {_ordem}";
    }
}
