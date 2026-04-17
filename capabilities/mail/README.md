# mail capability

Transactional email via SMTP.

## Wires

- Mailhog in compose (capture + UI at :8025).
- `IEmailSender` abstraction.
- Razor-based template rendering (`Views/Emails/*.cshtml`).
- Sample "welcome email" triggered by `UserCreated` domain event.
- Integration test asserts Mailhog received the message.

## Opinions

- **No HTML-only templates** — always include a plain-text alternative.
- **Rate-limit per recipient** — a user can't receive more than N of the
  same template in X minutes; default 1/hour for transactional messages.
- **Never send from the request thread** — emails go through the outbox
  (so the `queue` capability is a natural pairing).

## Escape hatches

- Swap SMTP for SendGrid / Postmark / SES by implementing `IEmailSender`
  against their SDK; Mailhog stays in dev for local testing.
