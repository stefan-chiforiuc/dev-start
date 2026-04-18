namespace {{Name}}.Infrastructure.Email;

public sealed class SmtpOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string From { get; set; } = "no-reply@example.com";
    public string? Username { get; set; }
    public string? Password { get; set; }
}
