using System.Globalization;
using AgendAi.Helpers;
using AgendAi.Models;
using AgendAi.Services;

namespace AgendAi.Views;

[QueryProperty(nameof(Data), "data")]
public partial class BloquearHorario : ContentPage
{
    private readonly AgendamentoService _agendamentos;
    private List<ProfissionalDoUsuario>? _profissionais;

    /// <summary>Dia selecionado na agenda (AAAA-MM-DD).</summary>
    public string? Data
    {
        set
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dia))
                DataPicker.Date = dia;
        }
    }

    public BloquearHorario(AgendamentoService agendamentos)
    {
        InitializeComponent();
        _agendamentos = agendamentos;

        InicioPicker.Time = new TimeSpan(12, 0, 0);
        FimPicker.Time = new TimeSpan(13, 0, 0);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_profissionais is not null) return;

        var resultado = await _agendamentos.MeusProfissionaisAsync();
        if (!resultado.Sucesso || resultado.Dados!.Count == 0)
        {
            await this.AlertarAsync("Bloquear horário", resultado.Erro ?? "Nenhum profissional encontrado.");
            await Shell.Current.GoToAsync("..");
            return;
        }

        _profissionais = resultado.Dados;
        ProfissionalPicker.ItemsSource = _profissionais;

        if (_profissionais.Count == 1) ProfissionalPicker.SelectedIndex = 0; // só um: não precisa escolher
        else ProfissionalSecao.IsVisible = true;
    }

    private async void OnVoltarClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnSalvarClicked(object? sender, EventArgs e)
    {
        if (ProfissionalPicker.SelectedItem is not ProfissionalDoUsuario profissional)
        {
            MostrarErro("Escolha o profissional.");
            return;
        }

        var inicio = (TimeSpan?)InicioPicker.Time ?? TimeSpan.Zero;
        var fim = (TimeSpan?)FimPicker.Time ?? TimeSpan.Zero;
        if (fim <= inicio)
        {
            MostrarErro("O horário final deve ser maior que o inicial.");
            return;
        }

        var dia = (DateTime?)DataPicker.Date ?? DateTime.Today;
        var motivo = MotivoEntry.Text?.Trim();

        ErroLabel.IsVisible = false;
        SalvarButton.IsEnabled = false;

        var resultado = await _agendamentos.BloquearAsync(
            profissional.Id,
            new BloquearRequest(
                Formatacao.DataParaApi(dia),
                inicio.ToString(@"hh\:mm"),
                fim.ToString(@"hh\:mm"),
                string.IsNullOrEmpty(motivo) ? null : motivo));

        SalvarButton.IsEnabled = true;

        if (!resultado.Sucesso)
        {
            MostrarErro(resultado.Erro ?? "Não foi possível bloquear o horário.");
            return;
        }

        await Shell.Current.GoToAsync("..");
    }

    private void MostrarErro(string mensagem)
    {
        ErroLabel.Text = mensagem;
        ErroLabel.IsVisible = true;
    }
}
