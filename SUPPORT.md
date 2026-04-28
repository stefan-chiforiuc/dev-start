# Support

## Where to ask

| You want to… | Channel |
|---|---|
| **Report a bug** | [Open an issue](https://github.com/stefan-chiforiuc/dev-start/issues/new?template=bug_report.yml) using the bug template. Include `dev-start --version` and `dev-start doctor` output. |
| **Propose a feature** | [Open an issue](https://github.com/stefan-chiforiuc/dev-start/issues/new?template=feature_request.yml) using the feature template. Read the relevant ADR first — most defaults are intentional. |
| **Propose a new capability** | Use the [capability proposal template](https://github.com/stefan-chiforiuc/dev-start/issues/new?template=capability_proposal.yml). Read [`capabilities/README.md`](./capabilities/README.md) and [ADR 0006](./docs/adr/0006-capabilities-not-templates.md) first. |
| **Ask a how-to question** | [Start a discussion](https://github.com/stefan-chiforiuc/dev-start/discussions) (preferred over issues for open-ended Q&A). |
| **Report a security issue** | Use [GitHub Private Vulnerability Reporting](https://github.com/stefan-chiforiuc/dev-start/security/advisories/new) — see [`SECURITY.md`](./SECURITY.md). **Do not file a public issue.** |
| **Contribute a fix** | Read [`CONTRIBUTING.md`](./CONTRIBUTING.md). |

## Before you ask

For most questions, the answer is in one of:

- [`README.md`](./README.md) — what `dev-start` is and how to install it.
- [`docs/golden-path.md`](./docs/golden-path.md) — getting started.
- [`docs/paved-road.md`](./docs/paved-road.md) — what the generated
  project commits to by default.
- [`docs/when-to-leave-the-road.md`](./docs/when-to-leave-the-road.md) —
  the supported escape hatches.
- [`docs/adr/`](./docs/adr/) — *why* each default is what it is.
- [`capabilities/<name>/README.md`](./capabilities/) — per-capability
  detail.

If a question isn't answered there, that's a documentation gap — please
file an issue.

## Maturity & response expectations

`dev-start` is **single-maintainer** and **pre-1.0** (currently shipping
`1.0.0-alpha`). That means:

- Response time on issues is best-effort. Critical bugs and security
  reports are prioritised.
- Feature requests outside the [`ROADMAP.md`](./ROADMAP.md) scope are
  unlikely to be accepted; an escape-hatch doc usually serves better.
- The CLI surface is opinions-locked but subject to alpha-cycle
  adjustment until `1.0.0` (non-alpha) ships. See [`RELEASING.md`](./RELEASING.md)
  § "Pre-release graduation path".
