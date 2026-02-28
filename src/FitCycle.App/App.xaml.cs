namespace FitCycle.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		var token = SecureStorage.GetAsync("auth_access_token").GetAwaiter().GetResult();

		MainPage = string.IsNullOrEmpty(token)
			? new NavigationPage(new Pages.LoginPage())
			: new AppShell();
	}
}
