namespace FitCycle.Infrastructure.Services;

public class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 465;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "FitCycle";
    public string NotifyEmail { get; set; } = "";
    public string WebhookSecret { get; set; } = "";
    public string AppBaseUrl { get; set; } = "https://fitcycle-production.up.railway.app";
    public string ResendApiKey { get; set; } = "";
    public string BrevoApiKey { get; set; } = "";
}
