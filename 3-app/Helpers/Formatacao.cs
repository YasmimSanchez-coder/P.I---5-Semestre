using System.Globalization;

namespace AgendAi.Helpers;

public static class Formatacao
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private static readonly string[] DiasCurtos = ["Dom", "Seg", "Ter", "Qua", "Qui", "Sex", "Sáb"];

    private static readonly string[] Meses =
        ["Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
         "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];

    public static string Moeda(decimal valor) => valor.ToString("C", PtBr);

    public static string Status(string status) => status switch
    {
        "pendente" => "Pendente",
        "confirmado" => "Confirmado",
        "cancelado" => "Cancelado",
        "concluido" => "Concluído",
        _ => status
    };

    public static string Saudacao(DateTime agora) => agora.Hour switch
    {
        < 12 => "Bom dia",
        < 18 => "Boa tarde",
        _ => "Boa noite"
    };

    public static string PrimeiroNome(string nome) =>
        nome.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? nome;

    public static string Inicial(string nome) =>
        string.IsNullOrWhiteSpace(nome) ? "?" : nome.Trim()[..1].ToUpperInvariant();

    public static string DiaCurto(DateTime dia) => DiasCurtos[(int)dia.DayOfWeek];

    public static string MesAno(DateTime dia) => $"{Meses[dia.Month - 1]} {dia.Year}";

    /// <summary>Ex.: "Qua, 24 Set"</summary>
    public static string DataCurta(DateTime dia) => $"{DiaCurto(dia)}, {dia.Day} {Meses[dia.Month - 1][..3]}";

    /// <summary>Ex.: "23 Set"</summary>
    public static string DiaMes(DateTime dia) => $"{dia.Day} {Meses[dia.Month - 1][..3]}";

    public static string TempoDecorrido(DateTime momento, DateTime agora)
    {
        var intervalo = agora - momento;
        if (intervalo.TotalMinutes < 1) return "agora";
        if (intervalo.TotalMinutes < 60) return $"{(int)intervalo.TotalMinutes}min atrás";
        if (intervalo.TotalHours < 24) return $"{(int)intervalo.TotalHours}h atrás";

        var dias = (int)intervalo.TotalDays;
        return dias == 1 ? "1 dia atrás" : $"{dias} dias atrás";
    }

    public static string DataParaApi(DateTime dia) => dia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
