using AtrBot.Infrastructure.Configuration;
using AtrBot.Infrastructure.Messaging;
using AtrBot.Infrastructure.Search;
using AtrBot.Infrastructure.Storage;
using Azure.Identity;
using Azure.Messaging.WebPubSub;
using Azure.Search.Documents;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureAppConfiguration((context, builder) =>
    {
        builder.AddEnvironmentVariables();
        var connectionString = Environment.GetEnvironmentVariable("AZURE_APPCONFIG_CONNECTION");
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            builder.AddAzureAppConfiguration(connectionString);
        }
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton(provider =>
        {
            var blobEndpoint = context.Configuration.GetValue<string>("Storage:BlobEndpoint");
            if (string.IsNullOrWhiteSpace(blobEndpoint))
            {
                throw new InvalidOperationException("Storage:BlobEndpoint configuration is required.");
            }

            return new BlobServiceClient(new Uri(blobEndpoint), new DefaultAzureCredential());
        });

        services.AddSingleton(provider =>
        {
            var endpoint = context.Configuration.GetValue<string>("Search:Endpoint");
            var indexName = context.Configuration.GetValue<string>("Search:IndexName") ?? "documents";
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new InvalidOperationException("Search:Endpoint configuration is required.");
            }

            return new SearchClient(new Uri(endpoint), indexName, new DefaultAzureCredential());
        });

        services.AddSingleton(provider =>
        {
            var connectionString = context.Configuration.GetValue<string>("WebPubSub:ConnectionString");
            var hub = context.Configuration.GetValue<string>("WebPubSub:Hub") ?? "chat";
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("WebPubSub:ConnectionString configuration is required.");
            }

            return new WebPubSubServiceClient(connectionString, hub);
        });

        services.AddSingleton(provider =>
        {
            var blobService = provider.GetRequiredService<BlobServiceClient>();
            var container = context.Configuration.GetValue<string>("Storage:PdfContainer") ?? "pdfs";
            return new DocumentStorageService(blobService, container);
        });

        services.AddSingleton<DocumentSearchService>();
        services.AddSingleton<TierSettingsProvider>();
        services.AddSingleton<StatusNotifier>();
    })
    .Build();

await host.RunAsync();
