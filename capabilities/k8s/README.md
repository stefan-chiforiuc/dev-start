# k8s capability

Adds a production-shape Helm chart plus Kustomize overlays for `dev`, `stage`,
and `prod`. The shape mirrors `platform/compose/docker-compose.yml` so local
dev and k8s don't drift.

## Layout

```
k8s/
  helm/
    Chart.yaml
    values.yaml           # shared defaults
    values-dev.yaml       # overrides per env (checked-in)
    values-stage.yaml
    values-prod.yaml
    templates/
      deployment.yaml     # api Deployment (HPA-aware)
      service.yaml
      ingress.yaml
      configmap.yaml
      secret.yaml         # placeholders; use sealed-secrets / ESO in real orgs
      hpa.yaml            # gated on values.hpa.enabled
      job-migrate.yaml    # one-shot EF migration Job (gated on values.migrations.enabled)
      servicemonitor.yaml # gated on values.otel.enabled
  kustomize/
    base/                 # raw manifests, equivalent to the rendered Helm output
    overlays/
      dev/ stage/ prod/   # replicas, image tags, resource sizes
```

## Usage

```bash
# Render with Helm
helm install {{name}} k8s/helm \
  --values k8s/helm/values-dev.yaml

# Or apply with Kustomize (no Helm required)
kubectl apply -k k8s/kustomize/overlays/dev
```

## Generating env values

```bash
dev-start promote dev    # writes k8s/overlays/dev/values.generated.yaml
dev-start promote dev --render   # shells out to `helm template` for a full render
```

`promote` reads `.devstart.json` (services, capabilities, deploy target) and
writes env-specific overrides: replica count, image tag pattern, whether to
enable the migration Job (if `postgres`/`ts-postgres` is installed), whether to
enable the OTel collector (if `otel`/`ts-otel` is installed), and a
placeholder ingress host.
