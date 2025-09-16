param namePrefix string
param location string
param storageAccountName string

resource hostingPlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: '${namePrefix}funcplan'
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2022-09-01' = {
  name: '${namePrefix}func'
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccountName};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
      ]
    }
    httpsOnly: true
  }
}

output functionAppName string = functionApp.name
