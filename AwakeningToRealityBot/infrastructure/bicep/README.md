# Infrastructure as Code

The Bicep templates provision the Azure resources required for the Awakening to Reality
platform.  Deploy using Azure CLI:

```bash
az deployment sub create \
  --name atr-infra \
  --location <azure-region> \
  --template-file main.bicep \
  --parameters environment=dev
```

## Parameters

- `environment`: Short suffix (`dev`, `test`, `prod`) appended to resource names.
- `location`: Azure region (e.g., `eastus`).
- `appInsightsLocation`: Optional override for Application Insights.

## Modules

- `storage.bicep`: General-purpose v2 storage account with blob container for PDFs.
- `search.bicep`: Azure AI Search service with vector search enabled.
- `appConfig.bicep`: Azure App Configuration store.
- `keyVault.bicep`: Key Vault for secrets.
- `staticwebapp.bicep`: Azure Static Web App for the Blazor client.
- `functions.bicep`: Consumption-plan Function App for APIs and orchestrations.
- `webPubSub.bicep`: Azure Web PubSub resource for real-time messaging.

## Post-deployment steps

1. Import `../appconfig/initial-settings.json` into the App Configuration store.
2. Upload ingestion function settings to Key Vault (OpenAI API keys, etc.).
3. Configure Static Web Apps GitHub deployment token.
4. Create Azure AD B2C tenant and record authority settings in App Configuration.
