using AgendAi.Models;
using Microsoft.Maui.Controls.Shapes;

namespace AgendAi.Helpers;

/// <summary>Peças visuais reutilizáveis, montadas pelo código com as cores do tema.</summary>
public static class Componentes
{
    private static RoundRectangle Cantos(double raio) => new() { CornerRadius = new CornerRadius(raio) };

    // ------------------------------------------------------------ utilidades
    public static void AoTocar(this View elemento, Action acao)
    {
        var gesto = new TapGestureRecognizer();
        gesto.Tapped += (_, _) => acao();
        elemento.GestureRecognizers.Add(gesto);
    }

    public static void NaColuna(this Grid grade, View elemento, int coluna)
    {
        Grid.SetColumn(elemento, coluna);
        grade.Children.Add(elemento);
    }

    public static Label CriarMensagemVazia(string texto) => new()
    {
        Text = texto,
        Style = Tema.Estilo("Suave"),
        HorizontalOptions = LayoutOptions.Center,
        HorizontalTextAlignment = TextAlignment.Center,
        Margin = new Thickness(0, 32)
    };

    public static Button CriarBotao(string texto, string estilo, Action acao)
    {
        var botao = new Button { Text = texto, Style = Tema.Estilo(estilo), HorizontalOptions = LayoutOptions.Fill };
        botao.Clicked += (_, _) => acao();
        return botao;
    }

    /// <summary>Botão de aba segmentada (Entrar/Cadastrar, Todos/Próximos/Histórico).</summary>
    public static void EstilizarAba(Button aba, bool ativa)
    {
        aba.BackgroundColor = ativa ? Tema.Cor("DouradoFundo") : Colors.Transparent;
        aba.TextColor = ativa ? Tema.Cor("Dourado") : Tema.Cor("TextoSuave");
        aba.BorderColor = ativa ? Tema.Cor("DouradoEscuro") : Colors.Transparent;
    }

    // ----------------------------------------------------------------- chips
    /// <summary>Filtro em formato de pílula (Todos, Cabelo, Unhas...).</summary>
    public static Border CriarChip(string texto, bool selecionado, Action aoTocar)
    {
        var chip = new Border
        {
            Padding = new Thickness(16, 8),
            Margin = new Thickness(0, 0, 8, 0),
            StrokeShape = Cantos(20),
            StrokeThickness = 1,
            Stroke = Tema.Cor(selecionado ? "Dourado" : "Borda"),
            BackgroundColor = Tema.Cor(selecionado ? "Dourado" : "Superficie"),
            Content = new Label
            {
                Text = texto,
                FontSize = 13,
                FontAttributes = selecionado ? FontAttributes.Bold : FontAttributes.None,
                TextColor = Tema.Cor(selecionado ? "Fundo" : "TextoSuave")
            }
        };
        chip.AoTocar(aoTocar);
        return chip;
    }

    // --------------------------------------------------------------- avatares
    public static Border CriarAvatar(string nome, double tamanho = 44) => new()
    {
        WidthRequest = tamanho,
        HeightRequest = tamanho,
        StrokeThickness = 0,
        StrokeShape = Cantos(tamanho / 2),
        Background = Tema.Pincel("GradienteDourado"),
        Content = new Label
        {
            Text = Formatacao.Inicial(nome),
            Style = Tema.Estilo("TituloCartao"),
            FontSize = tamanho * 0.4,
            TextColor = Tema.Cor("Fundo"),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        }
    };

    // --------------------------------------------------------------- etiquetas
    public static Border CriarEtiqueta(string texto, Color corTexto, Color corFundo) => new()
    {
        Padding = new Thickness(10, 4),
        StrokeThickness = 0,
        StrokeShape = Cantos(10),
        BackgroundColor = corFundo,
        HorizontalOptions = LayoutOptions.End,
        VerticalOptions = LayoutOptions.Start,
        Content = new Label { Text = texto, FontSize = 11, FontAttributes = FontAttributes.Bold, TextColor = corTexto }
    };

    public static Border CriarEtiquetaStatus(string status)
    {
        var (texto, fundo) = status switch
        {
            "confirmado" => (Tema.Cor("Verde"), Tema.Cor("VerdeFundo")),
            "pendente" => (Tema.Cor("Dourado"), Tema.Cor("DouradoFundo")),
            "cancelado" => (Tema.Cor("Erro"), Tema.Cor("ErroFundo")),
            _ => (Tema.Cor("TextoSuave"), Tema.Cor("SuperficieAlta"))
        };
        return CriarEtiqueta(Formatacao.Status(status), texto, fundo);
    }

    // ------------------------------------------------------ calendário e horas
    /// <summary>Um dia da semana (sigla + número), usado nas faixas de datas.</summary>
    public static Border CriarDia(DateTime dia, bool selecionado, bool desativado, Action aoTocar)
    {
        var corTexto = selecionado ? "Fundo" : desativado ? "TextoMudo" : "TextoClaro";

        var tile = new Border
        {
            Padding = new Thickness(0, 10),
            StrokeShape = Cantos(14),
            StrokeThickness = 1,
            Stroke = Tema.Cor(selecionado ? "Dourado" : "Borda"),
            BackgroundColor = Tema.Cor(selecionado ? "Dourado" : "Superficie"),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    new Label
                    {
                        Text = Formatacao.DiaCurto(dia),
                        FontSize = 11,
                        TextColor = Tema.Cor(selecionado ? "Fundo" : "TextoSuave"),
                        HorizontalOptions = LayoutOptions.Center
                    },
                    new Label
                    {
                        Text = dia.Day.ToString(),
                        FontSize = 16,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Tema.Cor(corTexto),
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            }
        };

        if (!desativado) tile.AoTocar(aoTocar);
        return tile;
    }

    public static Border CriarHorario(string hora, bool selecionado, Action aoTocar)
    {
        var botao = new Border
        {
            Padding = new Thickness(0, 12),
            StrokeShape = Cantos(12),
            StrokeThickness = 1,
            Stroke = Tema.Cor(selecionado ? "Dourado" : "Borda"),
            BackgroundColor = Tema.Cor(selecionado ? "DouradoFundo" : "Superficie"),
            Content = new Label
            {
                Text = hora,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = Tema.Cor(selecionado ? "Dourado" : "TextoClaro"),
                HorizontalOptions = LayoutOptions.Center
            }
        };
        botao.AoTocar(aoTocar);
        return botao;
    }

    // ------------------------------------------------- cartões de conteúdo
    public static Border CriarCartaoProfissional(Profissional profissional, bool selecionado, Action aoTocar)
    {
        var cartao = new Border
        {
            WidthRequest = 140,
            Padding = new Thickness(10, 14),
            Margin = new Thickness(0, 0, 10, 0),
            StrokeShape = Cantos(16),
            StrokeThickness = 1,
            Stroke = Tema.Cor(selecionado ? "Dourado" : "Borda"),
            BackgroundColor = Tema.Cor(selecionado ? "DouradoFundo" : "Superficie"),
            Content = new VerticalStackLayout
            {
                Spacing = 4,
                HorizontalOptions = LayoutOptions.Center,
                Children =
                {
                    CriarAvatar(profissional.Nome, 52),
                    new Label
                    {
                        Text = profissional.Nome,
                        Style = Tema.Estilo("TituloCartao"),
                        FontSize = 13,
                        Margin = new Thickness(0, 6, 0, 0),
                        HorizontalTextAlignment = TextAlignment.Center
                    },
                    new Label
                    {
                        Text = profissional.Especialidade ?? string.Empty,
                        Style = Tema.Estilo("Suave"),
                        FontSize = 11,
                        HorizontalTextAlignment = TextAlignment.Center,
                        LineBreakMode = LineBreakMode.TailTruncation,
                        MaxLines = 1
                    },
                    new Label
                    {
                        Text = profissional.NotaTexto,
                        FontSize = 12,
                        TextColor = Tema.Cor("Dourado"),
                        HorizontalTextAlignment = TextAlignment.Center
                    }
                }
            }
        };
        cartao.AoTocar(aoTocar);
        return cartao;
    }

    /// <summary>Linha da lista de serviços, com preço e botão "+ Add".</summary>
    public static Border CriarLinhaServico(Servico servico, bool selecionado, Action aoTocar)
    {
        var botaoAdd = new Border
        {
            Padding = new Thickness(12, 6),
            StrokeShape = Cantos(10),
            StrokeThickness = 1,
            Stroke = Tema.Cor("DouradoEscuro"),
            BackgroundColor = Tema.Cor(selecionado ? "Dourado" : "DouradoFundo"),
            HorizontalOptions = LayoutOptions.End,
            Content = new Label
            {
                Text = selecionado ? "✓ Adicionado" : "+ Add",
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                TextColor = Tema.Cor(selecionado ? "Fundo" : "Dourado")
            }
        };

        var esquerda = new VerticalStackLayout
        {
            Spacing = 4,
            VerticalOptions = LayoutOptions.Center,
            Children =
            {
                new Label { Text = servico.Nome, Style = Tema.Estilo("TituloCartao"), FontSize = 15 },
                new Label { Text = $"⏱ {servico.DuracaoTexto}", Style = Tema.Estilo("Suave"), FontSize = 12 }
            }
        };

        var direita = new VerticalStackLayout
        {
            Spacing = 6,
            HorizontalOptions = LayoutOptions.End,
            Children =
            {
                new Label
                {
                    Text = servico.PrecoTexto,
                    Style = Tema.Estilo("TituloSecao"),
                    TextColor = Tema.Cor("Dourado"),
                    FontSize = 16,
                    HorizontalOptions = LayoutOptions.End
                },
                botaoAdd
            }
        };

        var grade = new Grid
        {
            ColumnSpacing = 12,
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }
        };
        grade.NaColuna(esquerda, 0);
        grade.NaColuna(direita, 1);

        var linha = new Border
        {
            Style = Tema.Estilo("Cartao"),
            Stroke = Tema.Cor(selecionado ? "Dourado" : "Borda"),
            Content = grade
        };
        linha.AoTocar(aoTocar);
        return linha;
    }

    public static Border CriarLinhaAvaliacao(Avaliacao avaliacao)
    {
        var estrelas = new string('★', avaliacao.Nota) + new string('☆', 5 - avaliacao.Nota);

        var cabecalho = new Grid
        {
            ColumnSpacing = 10,
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        cabecalho.NaColuna(CriarAvatar(avaliacao.ClienteNome, 34), 0);
        cabecalho.NaColuna(
            new Label { Text = avaliacao.ClienteNome, Style = Tema.Estilo("TituloCartao"), FontSize = 14, VerticalOptions = LayoutOptions.Center }, 1);
        cabecalho.NaColuna(
            new Label { Text = estrelas, TextColor = Tema.Cor("Dourado"), FontSize = 13, VerticalOptions = LayoutOptions.Center }, 2);

        var conteudo = new VerticalStackLayout { Spacing = 8, Children = { cabecalho } };
        if (!string.IsNullOrWhiteSpace(avaliacao.Comentario))
            conteudo.Children.Add(new Label { Text = avaliacao.Comentario, Style = Tema.Estilo("Suave"), FontSize = 13 });

        return new Border { Style = Tema.Estilo("Cartao"), Content = conteudo };
    }
}
