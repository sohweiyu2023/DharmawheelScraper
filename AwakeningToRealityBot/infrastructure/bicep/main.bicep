param environment string = 'dev'
param location string = resourceGroup().location
param appInsightsLocation string = location

var namePrefix = 'atr${environment}'

module storage '../bicep/modules/storage.bicep' = {
  name: 'storage'
  params: {
    namePrefix: namePrefix
    location: location
  }
}

module search '../bicep/modules/search.bicep' = {
  name: 'search'
  params: {
    namePrefix: namePrefix
    location: location
  }
}

module appConfig '../bicep/modules/appConfig.bicep' = {
  name: 'appConfig'
  params: {
    namePrefix: namePrefix
    location: location
  }
}

module keyVault '../bicep/modules/keyVault.bicep' = {
  name: 'keyVault'
  params: {
    namePrefix: namePrefix
    location: location
  }
}

module staticWebApp '../bicep/modules/staticwebapp.bicep' = {
  name: 'staticWebApp'
  params: {
    namePrefix: namePrefix
    location: location
  }
}

module functions '../bicep/modules/functions.bicep' = {
  name: 'functions'
  params: {
    namePrefix: namePrefix
    location: location
    storageAccountName: storage.outputs.storageAccountName
  }
}

module webPubSub '../bicep/modules/webPubSub.bicep' = {
  name: 'webPubSub'
  params: {
    namePrefix: namePrefix
    location: location
  }
}

module applicationInsights '../bicep/modules/appInsights.bicep' = {
  name: 'appInsights'
  params: {
    namePrefix: namePrefix
    location: appInsightsLocation
  }
}

output storageAccountId string = storage.outputs.storageAccountId
output searchServiceName string = search.outputs.searchServiceName
output appConfigEndpoint string = appConfig.outputs.endpoint
output keyVaultUri string = keyVault.outputs.vaultUri
output staticWebAppHostname string = staticWebApp.outputs.defaultHostname
output functionAppName string = functions.outputs.functionAppName
output webPubSubHostname string = webPubSub.outputs.hostname
output applicationInsightsInstrumentationKey string = applicationInsights.outputs.instrumentationKey
