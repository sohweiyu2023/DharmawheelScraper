param namePrefix string
param location string

resource webPubSub 'Microsoft.SignalRService/WebPubSub@2022-11-01' = {
  name: '${namePrefix}pubsub'
  location: location
  sku: {
    name: 'Standard_S1'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    tls: {
      clientCertEnabled: false
    }
  }
}

output hostname string = webPubSub.properties.externalIP
