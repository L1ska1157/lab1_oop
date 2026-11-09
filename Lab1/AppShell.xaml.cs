namespace Lab1;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();
        Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
    }
}