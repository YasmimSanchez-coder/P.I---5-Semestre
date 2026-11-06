using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;

namespace AgendAi.Views;

public partial class Login : ContentPage
{
    private readonly AuthService _authService;
    private readonly SessionService _session;

    private bool _modoCadastro;

    public Login(AuthService authService, SessionService session)
    {
        InitializeComponent();
        _authService = authService;
        _session = session;

        DefinirModo(cadastro: false);
    }

    private void OnEntrarTabClicked(object? sender, EventArgs e) => DefinirModo(cadastro: false);

    private void OnCadastrarTabClicked(object? sender, EventArgs e) => DefinirModo(cadastro: true);

    private void OnMostrarSenhaTapped(object? sender, TappedEventArgs e) =>
        SenhaEntry.IsPassword = !SenhaEntry.IsPassword;

    private async void OnAcaoClicked(object? sender, EventArgs e)
    {
        if (_modoCadastro) await CadastrarAsync();
        else await EntrarAsync();
    }

    private async Task EntrarAsync()
    {
        var email = EmailEntry.Text?.Trim();
        var senha = SenhaEntry.Text;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(senha))
        {
            MostrarErro("Informe email e senha.");
            return;
        }

        DefinirCarregando(true);
        var resultado = await _authService.LoginAsync(email, senha);
        DefinirCarregando(false);

        await ConcluirAsync(resultado, "Não foi possível entrar.");
    }

    private async Task CadastrarAsync()
    {
        var nome = NomeEntry.Text?.Trim();
        var email = EmailEntry.Text?.Trim();
        var telefone = TelefoneEntry.Text?.Trim();
        var senha = SenhaEntry.Text;

        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(senha))
        {
            MostrarErro("Preencha nome, email e senha.");
            return;
        }

        if (senha.Length < 6)
        {
            MostrarErro("A senha deve ter pelo menos 6 caracteres.");
            return;
        }

        if (senha != ConfirmarSenhaEntry.Text)
        {
            MostrarErro("As senhas não conferem.");
            return;
        }

        DefinirCarregando(true);
        var resultado = await _authService.CadastrarAsync(new CadastroRequest(nome, email, telefone, senha));
        DefinirCarregando(false);

        await ConcluirAsync(resultado, "Não foi possível cadastrar.");
    }

    private async Task ConcluirAsync(ApiResult<LoginResponse> resultado, string erroPadrao)
    {
        if (!resultado.Sucesso)
        {
            MostrarErro(resultado.Erro ?? erroPadrao);
            return;
        }

        SenhaEntry.Text = string.Empty;
        ConfirmarSenhaEntry.Text = string.Empty;
        await Shell.Current.GoToAsync(_session.RotaInicial);
    }

    private void DefinirModo(bool cadastro)
    {
        _modoCadastro = cadastro;

        CamposCadastro.IsVisible = cadastro;
        CampoConfirmar.IsVisible = cadastro;
        AcaoButton.Text = cadastro ? "Criar Conta" : "Entrar na Conta";
        ErroLabel.IsVisible = false;

        Componentes.EstilizarAba(EntrarTab, ativa: !cadastro);
        Componentes.EstilizarAba(CadastrarTab, ativa: cadastro);
    }

    private void MostrarErro(string mensagem)
    {
        ErroLabel.Text = mensagem;
        ErroLabel.IsVisible = true;
    }

    private void DefinirCarregando(bool carregando)
    {
        ErroLabel.IsVisible = false;
        AcaoButton.IsEnabled = !carregando;
        Carregando.IsVisible = carregando;
        Carregando.IsRunning = carregando;
    }
}
