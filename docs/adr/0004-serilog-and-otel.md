# 4. Serilog + OpenTelemetry for observability

Date: 2026-04-17
Status: Accepted

## Context

Observability is easy to add on day 0 and expensive to retrofit. We want
generated projects to emit structured logs, traces, and metrics from the
first request.

.NET has first-class OpenTelemetry support. Logging has two common
choices: the built-in `Microsoft.Extensions.Logging` abstraction and
Serilog.

## Decision

- **Serilog** as the logging implementation, emitting **JSON to stdout**.
- **Serilog → OpenTelemetry logs** bridge so logs share trace/span IDs.
- **OpenTelemetry** for traces and metrics, exported via **OTLP**.
- Dev stack: Jaeger (traces) + Seq (logs). Prod: any OTLP-compatible
  collector.
- Every request gets a correlation ID surfaced in the `traceparent`
  response header.

## Consequences

- One source of truth for correlation across logs, traces, metrics.
- Serilog's sinks (enrichers, destructuring) are widely known; low
  adoption cost for most .NET teams.
- We commit to OTLP as the wire format — teams using proprietary
  collectors (Datadog, New Relic) need their collector's OTLP endpoint.
- Dev-time tools (Seq, Jaeger) are free and self-hosted; we don't push
  users toward a SaaS.

## Alternatives considered

- **`Microsoft.Extensions.Logging` only**: simpler, but sink ecosystem
  is thinner than Serilog's.
- **Application Insights SDK directly**: locks us to Azure; OTLP is the
  portable choice.
- **NLog / log4net**: acceptable but less momentum in modern .NET.
