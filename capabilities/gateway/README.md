# gateway capability

YARP reverse proxy. Used by multi-service mode to present a single
entry point that forwards authenticated requests to N services.

## Wires

- `Yarp.ReverseProxy`.
- Auth forwarding: the gateway validates the JWT once, forwards claims
  downstream via `X-User-*` headers (mTLS or signed token in prod).
- Per-route rate limits.
- Request ID propagation (`X-Request-Id`).
- Health checks aggregating backend health.

## Opinions

- **Gateway does auth, services trust it.** Downstream services do not
  re-validate JWTs — they read forwarded headers. This requires mTLS
  or a signed-header scheme between gateway and services in prod.
- **Thin gateway.** No business logic, no response transformation. If
  you find yourself writing C# in the gateway, you want a backend-for-
  frontend — make that a service.

## Escape hatches

- Prefer Traefik / Envoy / Kong? Remove this capability and replace with
  your platform's gateway. The services don't change.
