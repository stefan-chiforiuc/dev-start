# deploy-aca capability

Azure Container Apps deploy recipe using the Azure Developer CLI (`azd`)
and Bicep. Ships `azure.yaml`, a minimal Bicep module, and a GitHub
Actions deploy workflow keyed off OIDC federated credentials.

## Wires

- `azure.yaml` — azd project config.
- `infra/main.bicep` + `infra/main.parameters.json` — a Log Analytics
  workspace, Container Apps environment, and a single Container App
  pointing at the generated Dockerfile image (pushed to ACR or GHCR).
- `.github/workflows/deploy-aca.yml` — uses `azure/login@v2` (OIDC) +
  `azure/setup-azd@v2` to run `azd deploy`.

## Opinions

- **OIDC federated credentials**, never long-lived secrets.
- **Single environment** (`production`) by default; add `staging` via
  `azd env new`.
- **Ingress** on port 5000, HTTPS only, single revision per deploy.

## Escape hatches

- Swap Bicep for Terraform — remove `infra/*.bicep`, add your own config
  in `infra/`, update the workflow's `azd deploy` step.
- Container registry: defaults to GHCR (free, convenient). For ACR, set
  `containerRegistry` in `infra/main.parameters.json`.
