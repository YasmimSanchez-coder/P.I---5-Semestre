using AgendAi.Helpers;
using AgendAi.Models;

namespace AgendAi.Services;

/// <summary>Guarda em memória quem está logado enquanto o app estiver aberto.</summary>
public class SessionService
{
    public Usuario? Usuario { get; private set; }
    public string? Token { get; private set; }

    public bool EstaAutenticado => Usuario is not null && !string.IsNullOrEmpty(Token);

    public bool EhCliente => Usuario?.Tipo == "cliente";

    /// <summary>Clientes vão para a área de agendamento; profissionais e administradores, para a agenda.</summary>
    public string RotaInicial => EhCliente ? Rotas.Cliente : Rotas.Profissional;

    public void Iniciar(LoginResponse login)
    {
        Usuario = login.Usuario;
        Token = login.Token;
    }

    public void Encerrar()
    {
        Usuario = null;
        Token = null;
    }
}
