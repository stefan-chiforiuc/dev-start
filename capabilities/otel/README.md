# otel capability

OpenTelemetry for traces, metrics, and logs.

## Wires

- `OpenTelemetry.Extensions.Hosting` configured with:
  - ASP.NET Core instrumentation
  - HttpClient instrumentation
  - EF Core instrumentation (when `postgres` cap is present)
  - Runtime metrics
- OTLP exporter pointed at `OTEL_EXPORTER_OTLP_ENDPOINT` (Jaeger in dev).
- Serilog → OTel logs bridge so logs carry `trace_id` / `span_id`.
- `traceparent` response header surfaced so users can paste IDs into Jaeger.

## Opinions

- **OTLP is the wire format.** Vendor collectors accept it.
- **Console log sink = JSON**, never plain text. Parseable by any log
  shipper.
- **Sampling**: head-based `ParentBased(AlwaysOn)` in dev; `TraceIdRatioBased`
  in prod, configurable via env.

## Escape hatches

- Need App Insights? Add `Azure.Monitor.OpenTelemetry.AspNetCore` alongside
  OTLP — both can run.
