param namePrefix string
param location string

resource searchService 'Microsoft.Search/searchServices@2022-09-01' = {
  name: '${namePrefix}search'
  location: location
  sku: {
    name: 'standard'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    semanticSearch: 'standard'
  }
}

output searchServiceName string = searchService.name
