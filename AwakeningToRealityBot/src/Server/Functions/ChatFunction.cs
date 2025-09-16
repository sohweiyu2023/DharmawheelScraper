using System.Net;
using System.Text.Json;
using AtrBot.Infrastructure.Configuration;
using AtrBot.Infrastructure.Search;
using AtrBot.Shared.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace AtrBot.Server.Functions;

public sealed class ChatFunction
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly DocumentSearchService _searchService;
    private readonly TierSettingsProvider _tierSettingsProvider;
    private readonly ILogger _logger;

    public ChatFunction(DocumentSearchService searchService, TierSettingsProvider tierSettingsProvider, ILoggerFactory loggerFactory)
    {
        _searchService = searchService;
        _tierSettingsProvider = tierSettingsProvider;
        _logger = loggerFactory.CreateLogger<ChatFunction>();
    }

    [Function("Chat")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "chat")] HttpRequestData request)
    {
        var body = await JsonSerializer.DeserializeAsync<ChatRequest>(request.Body, SerializerOptions);
        if (body is null || string.IsNullOrWhiteSpace(body.Text))
        {
            return await CreateResponseAsync(request, HttpStatusCode.BadRequest, new { error = "Prompt is required." });
        }

        _logger.LogInformation("Received chat request using model {Model}", body.Model);

        var tierName = request.Headers.GetValues("x-atr-tier").FirstOrDefault() ?? "Free";
        var tier = _tierSettingsProvider.FindTier(tierName);
        if (tier is null)
        {
            return await CreateResponseAsync(request, HttpStatusCode.Forbidden, new { error = "Tier is not authorised." });
        }

        var citations = await _searchService.SearchAsync(body.Text, body.DocumentIds, cancellationToken: request.FunctionContext.CancellationToken);

        var response = new ChatResponse(
            Message: "This is a placeholder response. Connect the LLM client to produce live answers.",
            Citations: citations,
            Status: "Completed");

        return await CreateResponseAsync(request, HttpStatusCode.OK, response);
    }

    private static async Task<HttpResponseData> CreateResponseAsync<T>(HttpRequestData request, HttpStatusCode statusCode, T payload)
    {
        var response = request.CreateResponse(statusCode);
        await response.WriteAsJsonAsync(payload, SerializerOptions);
        return response;
    }
}
