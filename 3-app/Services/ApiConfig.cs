namespace AgendAi.Services;

public static class ApiConfig
{
    // Em celular físico, informe o IP do computador na rede, ex.: "http://192.168.0.10:3000/"
    private const string UrlManual = "";

    // No emulador Android, "localhost" é o próprio emulador;
    // 10.0.2.2 aponta para o computador que roda a API.
    public static string BaseUrl =>
        !string.IsNullOrEmpty(UrlManual) ? UrlManual
        : DeviceInfo.Platform == DevicePlatform.Android ? "http://10.0.2.2:3000/"
        : "http://localhost:3000/";
}
