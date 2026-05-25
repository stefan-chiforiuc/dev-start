# 0011 — Capability Definition of Done

- Status: Accepted
- Date: 2026-05-25
- Relates to: [ADR 0006](0006-capabilities-not-templates.md),
  [ADR 0007](0007-injectors-over-fork-templates.md),
  [ADR 0010](0010-flat-capability-names-and-families.md)

## Context

Several user-reported bugs in 1.0.0-alpha (see `docs/bug-catalog.md`) trace to
a single root cause: features that landed without an end-to-end test. A
capability could be merged with a valid `capability.json`, files that copy
cleanly, and injectors that parse — and still produce a project that doesn't
build, a wizard that doesn't surface it, or a `doctor` run that can't
diagnose it. Tests at the unit level passed; the user was the integration
test.

The project also has no in-repo way for a contributor (human or AI) to run
the actual CLI against a throwaway project before pushing. CI catches some
of this via `pack-smoke`, but only after a push, and the failure modes there
are noisier than a local reproduction would be.

## Decision

A capability is **done** — and reviewable for merge — only when it ships
**all** of the following:

1. **Manifest (`capability.json`)** with: `name`, `version`, `description`,
   `stacks`, `dependsOn`, `conflictsWith`, `provides`, `addsServices`,
   `envAdditions`, `postInstall`, `doctor`.
2. **`README.md`** with two sections: *what it wires* and *escape hatch*.
3. **`files/`** skeleton; token substitution exercised on every text file.
4. **`injectors.json` + fragments** if it modifies shared files. Idempotent:
   re-running the install must be a no-op.
5. **Wizard entry.** Either reachable through the interactive wizard prompts
   in `NewWizard` *or* explicitly listed in `internal-only-capabilities.txt`
   with a one-line reason. There is no third state.
6. **At least one variation row** in `GeneratedSourceShapeTests.Variations`
   (or `TsStackShapeTests.Variations` for `ts-` capabilities) that scaffolds
   the capability and Roslyn-validates the generated `.cs`.
7. **At least one `doctor` check** in `capability.json` so users can verify
   the capability is healthy post-install.
8. **CHANGELOG entry** under the next unreleased section.

Items 1, 3, 4, and 7 are statically enforced by `CapabilityIntegrityTests`.
Items 5 and 6 are enforced by `WizardWiringTests` and
`CapabilityCoverageTests` respectively. Items 2 and 8 are PR-checklist items
that human reviewers verify.

## How to verify locally

```sh
just test          # unit + architecture + integrity + coverage tests
just sandbox       # end-to-end: scaffold representative projects, dotnet build each
```

`just sandbox` is the gate that catches the class of bug pure unit tests
miss: broken `.sln`, malformed templates, capabilities that scaffold but
don't compile.

## Consequences

- Slight overhead per capability — but caught at PR time, not by users.
- New contributors get a clear list of what "good" looks like.
- Bug catalog (`docs/bug-catalog.md`) grows with each fix; reviewers and
  agents pattern-match against it before approving similar-shaped PRs.
- The wizard and the doctor stay in lockstep with `capabilities/`: a
  capability cannot land without being reachable and verifiable.

## Alternatives considered

- **Looser policy** — relying on review only. Tried in 1.0.0-alpha; produced
  the catalog entries this ADR is responding to.
- **Heavier policy** — requiring integration tests against a live database
  per capability. Too expensive for the average capability; reserved for
  capabilities that wrap external services and explicitly opt in.
