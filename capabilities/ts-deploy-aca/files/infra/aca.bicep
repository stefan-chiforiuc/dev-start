param location string = resourceGroup().location
param envName string = '{{name}}-env'
param appName string = '{{name}}'
param image string = 'ghcr.io/your-org/{{name}}:latest'
param containerPort int = 8080

resource logs 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${appName}-logs'
  location: location
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

resource environment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: envName
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logs.properties.customerId
        sharedKey: logs.listKeys().primarySharedKey
      }
    }
  }
}

resource app 'Microsoft.App/containerApps@2023-05-01' = {
  name: appName
  location: location
  properties: {
    managedEnvironmentId: environment.id
    configuration: {
      ingress: { external: true, targetPort: containerPort }
    }
    template: {
      containers: [ {
        name: appName
        image: image
        resources: { cpu: 1, memory: '2Gi' }
      } ]
      scale: { minReplicas: 1, maxReplicas: 5 }
    }
  }
}
