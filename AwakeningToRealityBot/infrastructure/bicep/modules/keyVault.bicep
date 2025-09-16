param namePrefix string
param location string

resource keyVault 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: '${namePrefix}kv'
  location: location
  properties: {
    enablePurgeProtection: true
    enableSoftDelete: true
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: []
  }
}

output vaultUri string = keyVault.properties.vaultUri
