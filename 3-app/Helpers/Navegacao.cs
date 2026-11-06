namespace AgendAi.Helpers;

public static class Navegacao
{
    /// <summary>Seleciona uma aba da barra inferior atual (0 = primeira).</summary>
    public static void IrParaAba(int indice)
    {
        if (Shell.Current?.CurrentItem is { } grupo && indice >= 0 && indice < grupo.Items.Count)
            grupo.CurrentItem = grupo.Items[indice];
    }
}
