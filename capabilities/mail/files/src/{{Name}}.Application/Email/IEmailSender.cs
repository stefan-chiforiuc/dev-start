namespace {{Name}}.Application.Email;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}
