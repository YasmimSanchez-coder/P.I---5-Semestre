namespace AgendAi.Helpers;

/// <summary>Acesso, pelo código, aos recursos definidos em Resources/Styles/AgendAiTheme.xaml.</summary>
public static class Tema
{
    public static Color Cor(string chave) => Buscar<Color>(chave);

    public static Brush Pincel(string chave) => Buscar<Brush>(chave);

    public static Style Estilo(string chave) => Buscar<Style>(chave);

    private static T Buscar<T>(string chave)
    {
        if (Application.Current?.Resources.TryGetValue(chave, out var valor) == true && valor is T resultado)
            return resultado;

        throw new InvalidOperationException($"Recurso '{chave}' não encontrado no tema.");
    }
}
