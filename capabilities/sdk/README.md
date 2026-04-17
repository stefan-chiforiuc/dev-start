# sdk capability

A typed TypeScript client generated from the API's OpenAPI spec — so
any frontend (React, Angular, plain Fetch) can call the API with full
autocompletion and no hand-written types.

## Wires

- OpenAPI spec committed as `src/{{Name}}.Api/openapi.json`, regenerated
  on build.
- `sdk/` npm workspace package.
- Generator: `openapi-typescript` + `openapi-fetch` (zero-runtime,
  type-only; small bundle).
- CI step: regenerate SDK, fail if uncommitted changes (keeps spec and
  SDK in sync).

## Opinions

- **Spec is the source of truth.** Don't hand-edit the SDK.
- **`openapi-typescript` over NSwag** for TS output — smaller, faster,
  zero runtime.
- **API-diff gate in CI** (see `.github/workflows/dotnet-ci.yml`) — breaking
  changes fail the PR.

## Escape hatches

- NSwag for class-based client: add a sibling `sdk-nswag/` project.
- Publish to a private registry: edit `sdk/package.json` and the release
  workflow.
