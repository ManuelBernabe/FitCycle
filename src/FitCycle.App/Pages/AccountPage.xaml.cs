using FitCycle.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FitCycle.App.Pages;

public partial class AccountPage : ContentPage
{
    private IAuthService? _auth;
    private string _currentRole = string.Empty;
    private int _currentUserId;

    public AccountPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = L10n.T("MyAccount");
        BackBtn.Text = L10n.T("BackToRoutinesBtn");
        EditProfileBtn.Text = L10n.T("EditProfile");
        AdminTitle.Text = L10n.T("UserManagement");
        CreateUserBtn.Text = L10n.T("CreateUser");
        LogoutBtn.Text = L10n.T("Logout");
        LanguageLabel.Text = L10n.T("Language");

        // Language picker
        LanguagePicker.ItemsSource = L10n.AvailableLanguages.Select(L10n.LanguageDisplayName).ToList();
        var currentIdx = Array.IndexOf(L10n.AvailableLanguages, L10n.CurrentLanguage);
        LanguagePicker.SelectedIndex = currentIdx >= 0 ? currentIdx : 0;

        Dispatcher.Dispatch(async () => await LoadAsync());
    }

    private IAuthService? GetAuthService()
    {
        return Application.Current?.Handler?.MauiContext?.Services.GetService<IAuthService>()
            ?? this.Handler?.MauiContext?.Services.GetService<IAuthService>();
    }

    private async Task LoadAsync()
    {
        try
        {
            StatusLbl.Text = L10n.T("Loading");
            _auth = GetAuthService();
            if (_auth is null)
            {
                StatusLbl.Text = L10n.T("ServiceUnavailable");
                return;
            }

            // Load current user info
            var me = await _auth.GetCurrentUserInfoAsync();
            if (me is null)
            {
                StatusLbl.Text = L10n.T("UserInfoError");
                return;
            }

            UsernameLabel.Text = me.Username;
            EmailLabel.Text = me.Email;
            RoleLabel.Text = me.Role;
            AvatarLabel.Text = me.Username.Length > 0 ? me.Username[0].ToString().ToUpper() : "?";
            _currentRole = me.Role;
            _currentUserId = me.Id;

            // Show admin section for Superuser
            if (me.Role == "Superuser")
            {
                AdminSection.IsVisible = true;
                await LoadUsersAsync();
            }
            else
            {
                AdminSection.IsVisible = false;
            }

            StatusLbl.Text = string.Empty;
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

    private async Task LoadUsersAsync()
    {
        if (_auth is null) return;

        UsersContainer.Children.Clear();
        var users = await _auth.GetAllUsersAsync();

        foreach (var user in users)
        {
            var frame = new Frame { Padding = 12, Margin = new Thickness(0, 2) };
            var container = new VerticalStackLayout { Spacing = 6 };

            var infoStack = new VerticalStackLayout { Spacing = 2 };
            infoStack.Children.Add(new Label { Text = user.Username, FontAttributes = FontAttributes.Bold, FontSize = 15 });
            infoStack.Children.Add(new Label { Text = user.Email, FontSize = 12, TextColor = Colors.Gray });
            infoStack.Children.Add(new Label { Text = user.Role, FontSize = 12, TextColor = Color.FromArgb("#512BD4") });

            var capturedUser = user;

            var btnLayout = new FlexLayout
            {
                Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap,
                Direction = Microsoft.Maui.Layouts.FlexDirection.Row,
                AlignItems = Microsoft.Maui.Layouts.FlexAlignItems.Center,
                JustifyContent = Microsoft.Maui.Layouts.FlexJustify.Start
            };

            var editBtn = new Button
            {
                Text = L10n.T("Edit"),
                FontSize = 12,
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 4, 2),
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#512BD4")
            };
            editBtn.Clicked += async (s, e) => await OnEditUser(capturedUser);

            var pwdBtn = new Button
            {
                Text = L10n.T("PasswordKey"),
                FontSize = 12,
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 4, 2),
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#ff8c00")
            };
            pwdBtn.Clicked += async (s, e) => await OnResetPassword(capturedUser);

            var deleteBtn = new Button
            {
                Text = L10n.T("DeleteUserBtn"),
                FontSize = 12,
                Padding = new Thickness(8, 4),
                Margin = new Thickness(0, 0, 4, 2),
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#dc3545")
            };
            deleteBtn.Clicked += async (s, e) => await OnDeleteUser(capturedUser);

            btnLayout.Children.Add(editBtn);
            btnLayout.Children.Add(pwdBtn);
            btnLayout.Children.Add(deleteBtn);

            container.Children.Add(infoStack);
            container.Children.Add(btnLayout);

            frame.Content = container;
            UsersContainer.Children.Add(frame);
        }
    }

    private async void OnCreateUserClicked(object? sender, EventArgs e)
    {
        if (_auth is null) return;

        var username = await DisplayPromptAsync(L10n.T("CreateUserTitle"), L10n.T("Username") + ":", L10n.T("Next"), L10n.T("Cancel"));
        if (string.IsNullOrWhiteSpace(username)) return;

        var email = await DisplayPromptAsync(L10n.T("CreateUserTitle"), L10n.T("Email") + ":", L10n.T("Next"), L10n.T("Cancel"), keyboard: Keyboard.Email);
        if (string.IsNullOrWhiteSpace(email)) return;

        var password = await DisplayPromptAsync(L10n.T("CreateUserTitle"), L10n.T("Password") + ":", L10n.T("Next"), L10n.T("Cancel"));
        if (string.IsNullOrWhiteSpace(password)) return;

        var role = await DisplayActionSheet(L10n.T("SelectRole"), L10n.T("Cancel"), null, "Standard", "Admin", "Superuser");
        if (string.IsNullOrWhiteSpace(role) || role == L10n.T("Cancel")) return;

        try
        {
            StatusLbl.Text = L10n.T("CreatingUser");
            var result = await _auth.CreateUserAsync(username.Trim(), email.Trim(), password, role);
            if (result is not null)
            {
                await LoadUsersAsync();
                StatusLbl.Text = L10n.T("UserCreated");
            }
            else
            {
                StatusLbl.Text = L10n.T("ErrorCreatingUser");
            }
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

    private async Task OnEditUser(UserInfoResult user)
    {
        if (_auth is null) return;

        var username = await DisplayPromptAsync(L10n.T("EditUserTitle"), L10n.T("Username") + ":", L10n.T("Next"), L10n.T("Cancel"), initialValue: user.Username);
        if (username is null) return; // Cancelled

        var email = await DisplayPromptAsync(L10n.T("EditUserTitle"), L10n.T("Email") + ":", L10n.T("Next"), L10n.T("Cancel"), initialValue: user.Email, keyboard: Keyboard.Email);
        if (email is null) return;

        var role = await DisplayActionSheet(L10n.T("SelectRole"), L10n.T("Cancel"), null, "Standard", "Admin", "Superuser");
        if (string.IsNullOrWhiteSpace(role) || role == L10n.T("Cancel")) return;

        try
        {
            StatusLbl.Text = L10n.T("UpdatingUser");
            var result = await _auth.UpdateUserAsync(user.Id,
                username != user.Username ? username.Trim() : null,
                email != user.Email ? email.Trim() : null,
                role != user.Role ? role : null);

            if (result is not null)
            {
                await LoadUsersAsync();
                StatusLbl.Text = L10n.T("UserUpdated");
            }
            else
            {
                StatusLbl.Text = L10n.T("ErrorUpdatingUser");
            }
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

    private async Task OnResetPassword(UserInfoResult user)
    {
        if (_auth is null) return;

        var newPassword = await DisplayPromptAsync(L10n.T("ChangePassword"),
            L10n.T("NewPasswordFor", user.Username), L10n.T("Save"), L10n.T("Cancel"));
        if (string.IsNullOrWhiteSpace(newPassword)) return;

        if (newPassword.Length < 6)
        {
            StatusLbl.Text = L10n.T("PasswordMinLength");
            return;
        }

        try
        {
            StatusLbl.Text = L10n.T("ChangingPassword");
            var success = await _auth.ResetPasswordAsync(user.Id, newPassword);
            StatusLbl.Text = success ? L10n.T("PasswordUpdated") : L10n.T("ErrorChangingPassword");
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

    private async Task OnDeleteUser(UserInfoResult user)
    {
        if (_auth is null) return;

        bool confirm = await DisplayAlert(L10n.T("Confirm"), L10n.T("ConfirmDeleteUser", user.Username), L10n.T("Yes"), L10n.T("No"));
        if (!confirm) return;

        try
        {
            StatusLbl.Text = L10n.T("DeletingUser");
            var success = await _auth.DeleteUserAsync(user.Id);
            if (success)
            {
                await LoadUsersAsync();
                StatusLbl.Text = L10n.T("UserDeleted");
            }
            else
            {
                StatusLbl.Text = L10n.T("ErrorDeletingUser");
            }
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

    private async void OnEditProfileClicked(object? sender, EventArgs e)
    {
        if (_auth is null) return;

        var username = await DisplayPromptAsync(L10n.T("EditProfile"), L10n.T("Username") + ":",
            L10n.T("Save"), L10n.T("Cancel"), initialValue: UsernameLabel.Text);
        if (username is null) return;

        var email = await DisplayPromptAsync(L10n.T("EditProfile"), L10n.T("Email") + ":",
            L10n.T("Save"), L10n.T("Cancel"), initialValue: EmailLabel.Text, keyboard: Keyboard.Email);
        if (email is null) return;

        try
        {
            StatusLbl.Text = L10n.T("Updating");
            var result = await _auth.UpdateUserAsync(_currentUserId,
                username.Trim() != UsernameLabel.Text ? username.Trim() : null,
                email.Trim() != EmailLabel.Text ? email.Trim() : null,
                null);

            if (result is not null)
            {
                UsernameLabel.Text = result.Username;
                EmailLabel.Text = result.Email;
                AvatarLabel.Text = result.Username[0].ToString().ToUpper();

                // Update SecureStorage
                await SecureStorage.SetAsync("auth_username", result.Username);
                StatusLbl.Text = L10n.T("ProfileUpdated");
            }
            else
            {
                StatusLbl.Text = L10n.T("ErrorUpdatingProfile");
            }
        }
        catch (Exception ex)
        {
            StatusLbl.Text = L10n.T("ErrorFmt", ex.Message);
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        if (LanguagePicker.SelectedIndex < 0) return;
        var newLang = L10n.AvailableLanguages[LanguagePicker.SelectedIndex];
        if (newLang == L10n.CurrentLanguage) return;
        L10n.SetLanguage(newLang);
        // Reload app to apply new language
        Application.Current!.MainPage = new AppShell();
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        _auth ??= GetAuthService();
        if (_auth is not null)
            await _auth.LogoutAsync();

        Application.Current!.MainPage = new NavigationPage(new LoginPage());
    }
}
