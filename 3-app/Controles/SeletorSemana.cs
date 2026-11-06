using AgendAi.Helpers;

namespace AgendAi.Controles;

/// <summary>Mês/ano com setas e uma faixa com os 7 dias da semana, como no Figma.</summary>
public class SeletorSemana : ContentView
{
    private readonly Label _mes;
    private readonly Button _anterior;
    private readonly Button _proxima;
    private readonly Grid _dias;

    private DateTime _inicioSemana;
    private DateTime _selecionado = DateTime.Today;
    private bool _permitirPassado;
    private int _diasAFrente = 60;

    /// <summary>Agenda do profissional precisa ver dias passados; o cliente não.</summary>
    public bool PermitirPassado
    {
        get => _permitirPassado;
        set { _permitirPassado = value; Reconstruir(); }
    }

    public int DiasAFrente
    {
        get => _diasAFrente;
        set { _diasAFrente = value; Reconstruir(); }
    }

    public DateTime Selecionado => _selecionado;

    public event EventHandler<DateTime>? DiaSelecionado;

    public SeletorSemana()
    {
        _mes = new Label { Style = Tema.Estilo("TituloSecao"), VerticalOptions = LayoutOptions.Center };
        _anterior = CriarSeta("‹", () => MudarSemana(-7));
        _proxima = CriarSeta("›", () => MudarSemana(7));

        var cabecalho = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        cabecalho.NaColuna(_mes, 0);
        cabecalho.NaColuna(_anterior, 1);
        cabecalho.NaColuna(_proxima, 2);

        _dias = new Grid { ColumnSpacing = 6 };
        for (var i = 0; i < 7; i++)
            _dias.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

        Content = new VerticalStackLayout { Spacing = 12, Children = { cabecalho, _dias } };
        Definir(DateTime.Today);
    }

    /// <summary>Seleciona um dia sem disparar o evento.</summary>
    public void Definir(DateTime dia)
    {
        _selecionado = dia.Date;
        _inicioSemana = InicioDaSemana(dia);
        Reconstruir();
    }

    private static DateTime InicioDaSemana(DateTime dia) => dia.Date.AddDays(-(int)dia.DayOfWeek);

    private static Button CriarSeta(string texto, Action acao)
    {
        var botao = new Button
        {
            Text = texto,
            Style = Tema.Estilo("BotaoIcone"),
            WidthRequest = 34,
            HeightRequest = 34
        };
        botao.Clicked += (_, _) => acao();
        return botao;
    }

    private void MudarSemana(int dias)
    {
        _inicioSemana = _inicioSemana.AddDays(dias);
        Reconstruir();
    }

    private void Selecionar(DateTime dia)
    {
        _selecionado = dia;
        Reconstruir();
        DiaSelecionado?.Invoke(this, dia);
    }

    private void Reconstruir()
    {
        if (_dias is null) return; // o construtor ainda está montando os controles

        var hoje = DateTime.Today;
        var limite = hoje.AddDays(_diasAFrente);

        _mes.Text = Formatacao.MesAno(_inicioSemana.AddDays(3));

        _anterior.IsEnabled = _permitirPassado || _inicioSemana > InicioDaSemana(hoje);
        _anterior.Opacity = _anterior.IsEnabled ? 1 : 0.3;
        _proxima.IsEnabled = _inicioSemana.AddDays(7) <= limite;
        _proxima.Opacity = _proxima.IsEnabled ? 1 : 0.3;

        _dias.Children.Clear();
        for (var i = 0; i < 7; i++)
        {
            var dia = _inicioSemana.AddDays(i);
            var desativado = (!_permitirPassado && dia < hoje) || dia > limite;
            var tile = Componentes.CriarDia(dia, dia == _selecionado, desativado, () => Selecionar(dia));
            _dias.NaColuna(tile, i);
        }
    }
}
