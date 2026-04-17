# auth capability

OIDC authentication — Keycloak in dev, any OIDC IdP in prod.

## Wires

- `Microsoft.AspNetCore.Authentication.JwtBearer` configured against
  `Auth__Authority`.
- `[Authorize]` default policy on every route group.
- Sample secured endpoint: `GET /me` returns the calling user's claims.
- `.http/auth.http` — token fetch + authed request recipe.

## Opinions

- **OIDC only.** No API keys in v1; they invite leakage.
- **Short-lived access tokens** (1 h default), refresh tokens managed by
  the client.
- `[Authorize]` is default; `[AllowAnonymous]` is an opt-out documented
  in review.

## Escape hatches

- Replace Keycloak with Auth0, Azure AD, Clerk — any OIDC IdP. See
  `docs/when-to-leave-the-road.md#auth`.
