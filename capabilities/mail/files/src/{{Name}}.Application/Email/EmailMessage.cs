namespace {{Name}}.Application.Email;

public sealed record EmailMessage(
    string To,
    string Subject,
    string TextBody,
    string? HtmlBody = null,
    string? From = null);
