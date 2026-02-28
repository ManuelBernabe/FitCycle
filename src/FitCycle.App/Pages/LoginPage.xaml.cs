using FitCycle.App.Services;

namespace FitCycle.App.Pages;

public partial class LoginPage : ContentPage
{
    private bool _isRegisterMode;

    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        AppNameLabel.Text = L10n.T("AppName");

        if (_isRegisterMode)
        {
            SectionTitle.Text = L10n.T("CreateAccount");
            ActionButton.Text = L10n.T("Register");
            ToggleLabel.Text = L10n.T("HaveAccountSignIn");
        }
        else
        {
            SectionTitle.Text = L10n.T("SignIn");
            ActionButton.Text = L10n.T("SignIn");
            ToggleLabel.Text = L10n.T("NoAccountSignUp");
        }

        UsernameEntry.Placeholder = L10n.T("Username");
        EmailEntry.Placeholder = L10n.T("Email");
        PasswordEntry.Placeholder = L10n.T("Password");
    }

    private IAuthService? GetAuthService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthService>()
            ?? this.Handler?.MauiContext?.Services.GetService<IAuthService>();
    }

    private async void OnActionClicked(object? sender, EventArgs e)
    {
        var auth = GetAuthService();
        if (auth is null) return;

        ErrorLabel.IsVisible = false;
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        ActionButton.IsEnabled = false;

        try
        {
            AuthResult result;

            if (_isRegisterMode)
            {
                result = await auth.RegisterAsync(
                    UsernameEntry.Text?.Trim() ?? "",
                    EmailEntry.Text?.Trim() ?? "",
                    PasswordEntry.Text ?? "");
            }
            else
            {
                result = await auth.LoginAsync(
                    UsernameEntry.Text?.Trim() ?? "",
                    PasswordEntry.Text ?? "");
            }

            if (result.Success)
            {
                Application.Current!.MainPage = new AppShell();
            }
            else
            {
                ErrorLabel.Text = result.Error ?? L10n.T("UnknownError");
                ErrorLabel.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            ErrorLabel.Text = L10n.T("ErrorFmt", ex.Message);
            ErrorLabel.IsVisible = true;
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
            ActionButton.IsEnabled = true;
        }
    }

    private void OnToggleTapped(object? sender, EventArgs e)
    {
        _isRegisterMode = !_isRegisterMode;

        if (_isRegisterMode)
        {
            SectionTitle.Text = L10n.T("CreateAccount");
            EmailEntry.IsVisible = true;
            ActionButton.Text = L10n.T("Register");
            ToggleLabel.Text = L10n.T("HaveAccountSignIn");
        }
        else
        {
            SectionTitle.Text = L10n.T("SignIn");
            EmailEntry.IsVisible = false;
            ActionButton.Text = L10n.T("SignIn");
            ToggleLabel.Text = L10n.T("NoAccountSignUp");
        }

        ErrorLabel.IsVisible = false;
    }
}
