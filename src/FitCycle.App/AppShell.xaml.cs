using FitCycle.App.Services;

namespace FitCycle.App;

public partial class AppShell : Shell
{
	public AppShell()
	{
		L10n.Init();
		InitializeComponent();
		RoutinesTab.Title = L10n.T("TabRoutines");
		StatsTab.Title = L10n.T("TabStats");

		// Language picker
		LangPicker.ItemsSource = L10n.AvailableLanguages.Select(L10n.LanguageDisplayName).ToList();
		var currentIdx = Array.IndexOf(L10n.AvailableLanguages, L10n.CurrentLanguage);
		LangPicker.SelectedIndex = currentIdx >= 0 ? currentIdx : 0;

		Routing.RegisterRoute("editday", typeof(Pages.EditDayPage));
		Routing.RegisterRoute("workout", typeof(Pages.WorkoutPage));
		Routing.RegisterRoute("workoutsummary", typeof(Pages.WorkoutSummaryPage));
		Routing.RegisterRoute("account", typeof(Pages.AccountPage));

		LoadUserInfo();
	}

	private async void LoadUserInfo()
	{
		try
		{
			var username = await SecureStorage.GetAsync("auth_username");
			var role = await SecureStorage.GetAsync("auth_role");
			if (!string.IsNullOrEmpty(username))
			{
				AvatarInitial.Text = username[0].ToString().ToUpper();
				UserInfoLabel.Text = username;
			}
		}
		catch
		{
			// SecureStorage may not be available
		}
	}

	private async void OnAccountClicked(object? sender, EventArgs e)
	{
		await Shell.Current.GoToAsync("account");
	}

	private void OnLanguageChanged(object? sender, EventArgs e)
	{
		if (LangPicker.SelectedIndex < 0) return;
		var newLang = L10n.AvailableLanguages[LangPicker.SelectedIndex];
		if (newLang == L10n.CurrentLanguage) return;
		L10n.SetLanguage(newLang);
		Application.Current!.MainPage = new AppShell();
	}
}
