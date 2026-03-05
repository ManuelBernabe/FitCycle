using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FitCycle.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(EmailSettings settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string username)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpUser) || string.IsNullOrWhiteSpace(_settings.SmtpPassword))
        {
            _logger.LogWarning("Email not configured — skipping welcome email for {Email}", toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(username, toEmail));
        message.Subject = $"Bienvenido a FitCycle, {username}!";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = BuildWelcomeHtml(username)
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Welcome email sent to {Email}", toEmail);
    }

    public async Task SendDeployNotificationAsync(string status, string? environment = null)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpUser) || string.IsNullOrWhiteSpace(_settings.SmtpPassword))
        {
            _logger.LogWarning("Email not configured — skipping deploy notification");
            return;
        }

        var toEmail = !string.IsNullOrWhiteSpace(_settings.NotifyEmail) ? _settings.NotifyEmail : _settings.SmtpUser;
        var isSuccess = status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
        var emoji = isSuccess ? "✅" : "❌";
        var env = environment ?? "production";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress("Admin", toEmail));
        message.Subject = $"{emoji} FitCycle Deploy {status} — {env}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"<!DOCTYPE html>
<html><head><meta charset=""UTF-8""></head>
<body style=""margin:0;padding:0;background:#f3f0fc;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:500px;margin:32px auto;background:#fff;border-radius:12px;overflow:hidden;"">
    <tr><td style=""background:{(isSuccess ? "#28a745" : "#dc3545")};padding:24px;text-align:center;"">
      <div style=""font-size:40px;"">{emoji}</div>
      <div style=""font-size:20px;color:#fff;font-weight:700;margin-top:8px;"">Deploy {status}</div>
    </td></tr>
    <tr><td style=""padding:24px;"">
      <p style=""color:#333;font-size:16px;margin:0 0 8px;""><strong>App:</strong> FitCycle</p>
      <p style=""color:#333;font-size:16px;margin:0 0 8px;""><strong>Entorno:</strong> {env}</p>
      <p style=""color:#333;font-size:16px;margin:0 0 8px;""><strong>Fecha:</strong> {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
      {(isSuccess ? @"<div style=""text-align:center;margin-top:16px;""><a href=""https://fitcycle-production.up.railway.app/"" style=""display:inline-block;background:#512BD4;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;font-weight:600;"">Abrir App</a></div>" : "")}
    </td></tr>
  </table>
</body></html>"
        };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("Deploy notification sent to {Email}: {Status}", toEmail, status);
    }

    private static string BuildWelcomeHtml(string username)
    {
        return $@"<!DOCTYPE html>
<html>
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1.0""></head>
<body style=""margin:0;padding:0;background:#f3f0fc;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;"">
  <table width=""100%"" cellpadding=""0"" cellspacing=""0"" style=""max-width:600px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;margin-top:32px;margin-bottom:32px;"">
    <tr>
      <td style=""background:#512BD4;padding:32px;text-align:center;"">
        <div style=""font-size:48px;font-weight:bold;color:#ffffff;letter-spacing:2px;"">FC</div>
        <div style=""font-size:24px;color:#ffffff;margin-top:8px;font-weight:600;"">FitCycle</div>
      </td>
    </tr>
    <tr>
      <td style=""padding:32px;"">
        <h1 style=""color:#333;font-size:22px;margin:0 0 16px;"">Bienvenido/a, {username}!</h1>
        <p style=""color:#555;font-size:16px;line-height:1.6;margin:0 0 16px;"">
          Tu cuenta en <strong style=""color:#512BD4;"">FitCycle</strong> ha sido creada exitosamente.
        </p>
        <p style=""color:#555;font-size:16px;line-height:1.6;margin:0 0 24px;"">
          Con FitCycle puedes:
        </p>
        <ul style=""color:#555;font-size:15px;line-height:2;padding-left:20px;margin:0 0 24px;"">
          <li>Crear tu rutina semanal personalizada</li>
          <li>Registrar pesos y repeticiones por serie</li>
          <li>Seguir tu progreso con estadisticas detalladas</li>
          <li>Entrenar sin conexion gracias al modo offline</li>
        </ul>
        <div style=""text-align:center;margin:24px 0;"">
          <a href=""https://fitcycle-production.up.railway.app/"" style=""display:inline-block;background:#512BD4;color:#ffffff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;font-size:16px;"">
            Comenzar a entrenar
          </a>
        </div>
      </td>
    </tr>
    <tr>
      <td style=""background:#f9f9f9;padding:20px 32px;text-align:center;border-top:1px solid #eee;"">
        <p style=""color:#999;font-size:13px;margin:0;"">
          Equipo FitCycle &middot; Tu entrenamiento, tu ritmo
        </p>
      </td>
    </tr>
  </table>
</body>
</html>";
    }
}
