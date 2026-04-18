using System.Net.Mail;
using Microsoft.Extensions.Options;
using {{Name}}.Application.Email;

namespace {{Name}}.Infrastructure.Email;

internal sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    private readonly SmtpOptions _opts = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_opts.Host, _opts.Port)
        {
            EnableSsl = !string.IsNullOrEmpty(_opts.Username),
            Credentials = !string.IsNullOrEmpty(_opts.Username)
                ? new System.Net.NetworkCredential(_opts.Username, _opts.Password)
                : null,
        };

        using var msg = new MailMessage(
            from: message.From ?? _opts.From,
            to: message.To,
            subject: message.Subject,
            body: message.TextBody);

        if (message.HtmlBody is not null)
        {
            msg.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(message.HtmlBody, null, "text/html"));
        }

        await client.SendMailAsync(msg, ct);
    }
}
