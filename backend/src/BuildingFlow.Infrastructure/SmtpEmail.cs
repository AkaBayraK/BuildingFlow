using System.Net;
using System.Net.Mail;
using System.Text;
using BuildingFlow.Application;
using Microsoft.Extensions.Configuration;

namespace BuildingFlow.Infrastructure;

public sealed class SmtpOptions
{
    private static readonly string[] LocalHosts = ["localhost", "127.0.0.1", "::1", "mailpit"];
    public bool Enabled { get; init; }
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public bool UseStartTls { get; init; } = true;
    public bool AllowInsecureLocal { get; init; }
    public string From { get; init; } = "";
    public string FromName { get; init; } = "BuildingFlow";
    public int TimeoutSeconds { get; init; } = 30;

    public static SmtpOptions FromConfiguration(IConfiguration config) => new() {
        Enabled = bool.TryParse(config["Smtp:Enabled"], out var enabled) && enabled,
        Host = config["Smtp:Host"] ?? "", Port = int.TryParse(config["Smtp:Port"], out var port) ? port : 587,
        Username = config["Smtp:Username"] ?? "", Password = config["Smtp:Password"] ?? "",
        UseStartTls = !bool.TryParse(config["Smtp:UseStartTls"], out var tls) || tls,
        AllowInsecureLocal = bool.TryParse(config["Smtp:AllowInsecureLocal"], out var insecure) && insecure,
        From = config["Smtp:From"] ?? "", FromName = config["Smtp:FromName"] ?? "BuildingFlow",
        TimeoutSeconds = int.TryParse(config["Smtp:TimeoutSeconds"], out var timeout) ? timeout : 30
    };

    // Plain SMTP is accepted only for a local capture server; credentials never travel unencrypted.
    public void Validate()
    {
        var local = LocalHosts.Contains(Host, StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(Host) || Host.Any(char.IsWhiteSpace) || Port is < 1 or > 65535 ||
            TimeoutSeconds is < 5 or > 120 || !MailAddress.TryCreate(From, out var from) || from.Address != From ||
            string.IsNullOrWhiteSpace(FromName) || FromName.Length > 100 || FromName.Any(char.IsControl) ||
            (Username.Length > 0) != (Password.Length > 0) ||
            (!UseStartTls && (!AllowInsecureLocal || !local || Username.Length > 0)))
            throw new InvalidOperationException("SMTP configuration is invalid.");
    }
}

// System.Net.Mail supports STARTTLS (587) but not implicit TLS (465); use a provider endpoint with STARTTLS.
public sealed class SmtpEmailHandler : IMessageHandler
{
    public const string Name = "smtp-email";
    private readonly SmtpOptions options;

    public SmtpEmailHandler(SmtpOptions options)
    {
        options.Validate();
        this.options = options;
    }

    public string Type => MessageContracts.Email;
    public string Consumer => Name;

    public async Task<MessageHandlingOutcome> HandleAsync(IMessageCommand command, CancellationToken cancellationToken)
    {
        if (command is not EmailRequested email) throw new InvalidMessageException();
        using var message = new MailMessage(new MailAddress(options.From, options.FromName, Encoding.UTF8), new MailAddress(email.Recipient)) {
            Subject = email.Subject, Body = email.Body, IsBodyHtml = false,
            SubjectEncoding = Encoding.UTF8, BodyEncoding = Encoding.UTF8, HeadersEncoding = Encoding.UTF8
        };
        message.Headers.Add("Message-ID", $"<{email.MessageId:D}@buildingflow>");
        message.Headers.Add("X-BuildingFlow-Message-Id", email.MessageId.ToString("D"));
        message.Headers.Add("X-BuildingFlow-Correlation-Id", email.CorrelationId.ToString("D"));
        message.Headers.Add("Content-Language", email.Locale);
        using var client = new SmtpClient(options.Host, options.Port) {
            EnableSsl = options.UseStartTls, DeliveryMethod = SmtpDeliveryMethod.Network,
            Timeout = options.TimeoutSeconds * 1000, UseDefaultCredentials = false,
            Credentials = options.Username.Length > 0 ? new NetworkCredential(options.Username, options.Password) : null
        };
        try {
            await client.SendMailAsync(message, cancellationToken);
        } catch (SmtpException exception) when (IsPermanent(exception)) {
            throw new PermanentMessageFailureException();
        } catch (SmtpException) {
            throw new TimeoutException("SMTP delivery failed transiently.");
        }
        return MessageHandlingOutcome.Delivered;
    }

    // 5xx replies are permanent (RFC 5321 §4.2.1); connection failures and 4xx replies are retried.
    public static bool IsPermanent(SmtpException exception) =>
        exception is SmtpFailedRecipientsException many
            ? many.InnerExceptions.All(inner => (int)inner.StatusCode >= 500)
            : (int)exception.StatusCode >= 500;
}
