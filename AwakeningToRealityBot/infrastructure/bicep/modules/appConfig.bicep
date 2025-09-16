param namePrefix string
param location string

resource appConfig 'Microsoft.AppConfiguration/configurationStores@2022-05-01' = {
  name: '${namePrefix}appcfg'
  location: location
  sku: {
    name: 'Standard'
  }
  properties: {
    disableLocalAuth: true
    enablePurgeProtection: true
  }
}

output endpoint string = appConfig.properties.endpoint
