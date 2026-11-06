namespace AgendAi.Models;

public record Usuario(int Id, string Nome, string Email, string? Telefone, string Tipo);

public record LoginRequest(string Email, string Senha);

public record CadastroRequest(string Nome, string Email, string? Telefone, string Senha);

public record LoginResponse(Usuario Usuario, string Token);

public record ErroResponse(string? Erro);

public record MensagemResponse(string Mensagem);

public record CriarResponse(int Id, string Mensagem);
