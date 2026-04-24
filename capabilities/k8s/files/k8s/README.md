# k8s

Two ways to deploy:

## Helm

```bash
helm upgrade --install {{name}} ./helm \
  --values helm/values-dev.yaml \
  --namespace {{name}}-dev --create-namespace
```

Each env has a `values-<env>.yaml`. `dev-start promote <env>` writes a
`values.generated.yaml` that you can add to the `-f` chain.

## Kustomize

```bash
kubectl apply -k kustomize/overlays/dev
```

Overlays are self-contained — they set namespace, image tag, replica count
and resource requests. Keep any per-env secret material out of the repo;
pair with sealed-secrets, SOPS, or External Secrets.
