namespace FitCycle.Infrastructure.Services;

public interface IEmailService
{
    Task SendWelcomeEmailAsync(string toEmail, string username);
    Task SendDeployNotificationAsync(string status, string? environment = null);
}
