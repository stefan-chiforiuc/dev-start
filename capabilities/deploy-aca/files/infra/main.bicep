// Minimal Azure Container Apps deployment.
// Resources: Log Analytics workspace, Container Apps environment, one
// Container App ingress-exposed on port 5000.

targetScope = 'resourceGroup'

@description('Name prefix used for all resources in this environment.')
param name string

@description('Region for the resource group.')
param location string = resourceGroup().location

@description('Container image to deploy (e.g. ghcr.io/owner/{{name}}:v1).')
param image string

@description('Environment variables passed to the container app.')
param env array = []

resource law 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${name}-law'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource env_ 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: '${name}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: law.properties.customerId
        sharedKey: law.listKeys().primarySharedKey
      }
    }
  }
}

resource app 'Microsoft.App/containerApps@2024-03-01' = {
  name: name
  location: location
  properties: {
    managedEnvironmentId: env_.id
    configuration: {
      ingress: {
        external: true
        targetPort: 5000
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          name: 'api'
          image: image
          env: env
          resources: { cpu: json('0.5'), memory: '1Gi' }
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/healthz', port: 5000 }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
            {
              type: 'Readiness'
              httpGet: { path: '/readyz', port: 5000 }
              initialDelaySeconds: 10
              periodSeconds: 15
            }
          ]
        }
      ]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}

output url string = 'https://${app.properties.configuration.ingress.fqdn}'
