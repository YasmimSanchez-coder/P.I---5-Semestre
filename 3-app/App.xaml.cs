namespace AgendAi;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Dark; // diálogos e seletores nativos também ficam escuros
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Tamanho de celular para testar no Windows.
        return new Window(new AppShell())
        {
            Title = "AgendAí",
            Width = 400,
            Height = 780
        };
    }
}
