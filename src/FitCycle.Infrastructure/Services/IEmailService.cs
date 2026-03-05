namespace FitCycle.Infrastructure.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string username);
    Task SendActivationEmailAsync(string toEmail, string username, string activationUrl);
    Task SendDeployNotificationAsync(string status, string? environment = null);
}
