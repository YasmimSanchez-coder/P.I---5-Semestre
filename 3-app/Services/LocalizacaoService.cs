namespace AgendAi.Services;

/// <summary>Obtém a posição do GPS (uma vez) e guarda para as outras telas.</summary>
public class LocalizacaoService
{
    private bool _consultou;

    public Location? UltimaPosicao { get; private set; }

    /// <summary>Retorna null se o usuário negar a permissão ou ocorrer falha.</summary>
    public async Task<Location?> ObterAsync()
    {
        if (_consultou) return UltimaPosicao;

        try
        {
            var permissao = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (permissao != PermissionStatus.Granted)
                permissao = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();

            if (permissao == PermissionStatus.Granted)
            {
                UltimaPosicao = await Geolocation.Default.GetLastKnownLocationAsync()
                                ?? await Geolocation.Default.GetLocationAsync(
                                    new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(8)));
            }
        }
        catch (Exception)
        {
            // GPS desligado, sem suporte na plataforma, etc.: o app segue sem ordenar por distância.
        }

        _consultou = true;
        return UltimaPosicao;
    }
}
