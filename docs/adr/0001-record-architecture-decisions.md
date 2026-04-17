# 1. Record architecture decisions

Date: 2026-04-17
Status: Accepted

## Context

`dev-start` is an opinionated scaffolder. Opinions rot when no one
remembers why they were made. We need a cheap, append-only record of
every non-trivial default — both for maintainers and for users who
want to know whether an opinion still applies to them.

## Decision

We will use **Architecture Decision Records** in the style of Michael
Nygard's original format: one ADR per decision, numbered in order,
superseded rather than edited.

Each ADR has four sections: Context, Decision, Consequences, and an
optional "Alternatives considered."

The baseline rule:

> A pull request that changes a user-visible default, adds a capability,
> or contradicts an existing ADR **must include an ADR** under
> `docs/adr/`.

## Consequences

- Minor overhead per decision (~10 minutes to write an ADR).
- Reviewers have a canonical source when a PR contradicts a default.
- `docs/when-to-leave-the-road.md` stays consistent because each
  escape hatch references the ADR it overrides.
- New contributors can read the ADR folder and understand the
  project's stance without reading every PR.
