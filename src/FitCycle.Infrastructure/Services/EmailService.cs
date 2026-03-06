using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace FitCycle.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;
    private static readonly HttpClient _httpClient = new();

    public EmailService(EmailSettings settings, ILogger<EmailService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    private bool UseBrevo => !string.IsNullOrWhiteSpace(_settings.BrevoApiKey);
    private bool UseResend => !string.IsNullOrWhiteSpace(_settings.ResendApiKey);

    private bool HasSmtpCredentials =>
        !string.IsNullOrWhiteSpace(_settings.SmtpUser) && !string.IsNullOrWhiteSpace(_settings.SmtpPassword);

    public async Task SendWelcomeEmailAsync(string toEmail, string username)
    {
        var subject = $"Bienvenido a FitCycle, {username}!";
        var html = BuildWelcomeHtml(username);
        await SendEmailAsync(toEmail, username, subject, html, "welcome");
    }

    public async Task SendDeployNotificationAsync(string status, string? environment = null)
    {
        if (!UseBrevo && !UseResend && !HasSmtpCredentials)
        {
            _logger.LogWarning("Email not configured — skipping deploy notification");
            return;
        }

        var toEmail = !string.IsNullOrWhiteSpace(_settings.NotifyEmail) ? _settings.NotifyEmail : _settings.SmtpUser;
        if (string.IsNullOrWhiteSpace(toEmail) && (UseBrevo || UseResend))
        {
            _logger.LogWarning("No NotifyEmail configured — skipping deploy notification");
            return;
        }

        var isSuccess = status.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
        var emoji = isSuccess ? "✅" : "❌";
        var env = environment ?? "production";

        var subject = $"{emoji} FitCycle Deploy {status} — {env}";
        var html = $@"<!DOCTYPE html>
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
</body></html>";

        await SendEmailAsync(toEmail, "Admin", subject, html, "deploy notification");
    }

    public async Task SendActivationEmailAsync(string toEmail, string username, string activationUrl)
    {
        var subject = "Activa tu cuenta en FitCycle";
        var html = BuildActivationHtml(username, activationUrl);
        await SendEmailAsync(toEmail, username, subject, html, "activation");
    }

    private async Task SendEmailAsync(string toEmail, string toName, string subject, string html, string emailType)
    {
        if (UseBrevo)
        {
            await SendViaBrevoAsync(toEmail, toName, subject, html, emailType);
        }
        else if (UseResend)
        {
            await SendViaResendAsync(toEmail, subject, html, emailType);
        }
        else if (HasSmtpCredentials)
        {
            await SendViaSmtpAsync(toEmail, toName, subject, html, emailType);
        }
        else
        {
            _logger.LogWarning("Email not configured — skipping {EmailType} email for {Email}", emailType, toEmail);
        }
    }

    private async Task SendViaResendAsync(string toEmail, string subject, string html, string emailType)
    {
        var from = !string.IsNullOrWhiteSpace(_settings.FromEmail)
            ? $"{_settings.FromName} <{_settings.FromEmail}>"
            : $"{_settings.FromName} <onboarding@resend.dev>";

        var payload = new
        {
            from,
            to = new[] { toEmail },
            subject,
            html
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.resend.com/emails")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ResendApiKey);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Resend: {EmailType} email sent to {Email}", emailType, toEmail);
        }
        else
        {
            _logger.LogError("Resend: failed to send {EmailType} email to {Email}. Status: {Status}, Body: {Body}",
                emailType, toEmail, response.StatusCode, responseBody);
            throw new Exception($"Resend API error: {response.StatusCode} — {responseBody}");
        }
    }

    private async Task SendViaBrevoAsync(string toEmail, string toName, string subject, string html, string emailType)
    {
        var payload = new
        {
            sender = new { name = _settings.FromName, email = _settings.FromEmail },
            to = new[] { new { email = toEmail, name = toName } },
            subject,
            htmlContent = html
        };

        var json = JsonSerializer.Serialize(payload);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", _settings.BrevoApiKey);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Brevo: {EmailType} email sent to {Email}", emailType, toEmail);
        }
        else
        {
            _logger.LogError("Brevo: failed to send {EmailType} email to {Email}. Status: {Status}, Body: {Body}",
                emailType, toEmail, response.StatusCode, responseBody);
            throw new Exception($"Brevo API error: {response.StatusCode} — {responseBody}");
        }
    }

    private async Task SendViaSmtpAsync(string toEmail, string toName, string subject, string html, string emailType)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = html };
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        var sslOptions = _settings.SmtpPort == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, sslOptions);
        await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        _logger.LogInformation("SMTP: {EmailType} email sent to {Email}", emailType, toEmail);
    }

    private static string BuildActivationHtml(string username, string activationUrl)
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
        <h1 style=""color:#333;font-size:22px;margin:0 0 16px;"">Hola, {username}!</h1>
        <p style=""color:#555;font-size:16px;line-height:1.6;margin:0 0 16px;"">
          Gracias por registrarte en <strong style=""color:#512BD4;"">FitCycle</strong>.
          Para activar tu cuenta, haz clic en el siguiente botón:
        </p>
        <div style=""text-align:center;margin:24px 0;"">
          <a href=""{activationUrl}"" style=""display:inline-block;background:#512BD4;color:#ffffff;padding:14px 32px;border-radius:8px;text-decoration:none;font-weight:600;font-size:16px;"">
            Activar mi cuenta
          </a>
        </div>
        <p style=""color:#888;font-size:13px;line-height:1.5;margin:16px 0 0;"">
          Si el botón no funciona, copia y pega este enlace en tu navegador:<br>
          <a href=""{activationUrl}"" style=""color:#512BD4;word-break:break-all;"">{activationUrl}</a>
        </p>
        <p style=""color:#999;font-size:12px;margin-top:16px;"">
          Este enlace expira en 24 horas.
        </p>
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
