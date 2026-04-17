# flags capability

Feature flags via the **OpenFeature** SDK — so flag calls stay portable
across providers.

## Wires

- `OpenFeature` SDK.
- In-memory provider wired by default; flags defined in
  `appsettings.Development.json`.
- `IFeatureFlags` abstraction with async `IsEnabledAsync(string flag, EvaluationContext ctx)`.
- Sample flag `orders.new-flow-enabled` used by the sample endpoint.
- Test helper: toggle a flag for a scoped block.

## Opinions

- **Short-lived flags over permanent config.** Every flag has an owner
  and a removal date in its description.
- **No business rules in flag config.** Flags are booleans; complex rules
  live in code.

## Escape hatches

- Swap to LaunchDarkly, Unleash, GrowthBook, ConfigCat — any OpenFeature
  provider. One-line registration change.
