namespace AgendAi.Helpers;

/// <summary>Categorias de serviço usadas nos filtros (devem coincidir com servicos.categoria no banco).</summary>
public static class Categorias
{
    public static readonly string[] Todas = ["Todos", "Cabelo", "Unhas", "Estética", "Barba"];

    /// <summary>"Todos" significa sem filtro (null).</summary>
    public static string? ParaFiltro(string categoria) => categoria == "Todos" ? null : categoria;
}
