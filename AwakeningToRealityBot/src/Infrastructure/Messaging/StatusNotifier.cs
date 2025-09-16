using Azure.Messaging.WebPubSub;
using AtrBot.Shared.Contracts;
using System.Text.Json;

namespace AtrBot.Infrastructure.Messaging;

/// <summary>
/// Publishes chat status updates to connected clients via Azure Web PubSub.
/// </summary>
public sealed class StatusNotifier
{
    private readonly WebPubSubServiceClient _serviceClient;

    public StatusNotifier(WebPubSubServiceClient serviceClient)
    {
        _serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
    }

    public async Task BroadcastStatusAsync(string connectionId, ChatResponse response, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            throw new ArgumentException("Connection id is required", nameof(connectionId));
        }

        var payload = JsonSerializer.Serialize(response);
        await _serviceClient.SendToConnectionAsync(connectionId, BinaryData.FromString(payload), cancellationToken: cancellationToken);
    }
}
