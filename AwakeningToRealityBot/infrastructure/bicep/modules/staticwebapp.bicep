param namePrefix string
param location string

resource staticWebApp 'Microsoft.Web/staticSites@2022-09-01' = {
  name: '${namePrefix}spa'
  location: location
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
  properties: {
    repositoryUrl: 'https://github.com/dharmawheel/awakening-to-reality'
    branch: 'main'
    buildProperties: {
      appLocation: 'src/Client'
      apiLocation: 'src/Server'
      appArtifactLocation: 'wwwroot'
    }
  }
}

output defaultHostname string = staticWebApp.properties.defaultHostname
