using System.Net.Http.Json;
using AtrBot.Shared.Contracts;

namespace AtrBot.Client.Services;

/// <summary>
/// Wraps HTTP calls to the Azure Functions API.
/// </summary>
public sealed class ChatClient
{
    private readonly HttpClient _httpClient;

    public ChatClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var chatResponse = await response.Content.ReadFromJsonAsync<ChatResponse>(cancellationToken: cancellationToken);
        return chatResponse ?? new ChatResponse("No response received.", Array.Empty<Citation>(), "Error");
    }
}
