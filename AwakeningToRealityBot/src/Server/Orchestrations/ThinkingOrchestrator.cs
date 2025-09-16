using AtrBot.Infrastructure.Messaging;
using AtrBot.Infrastructure.Search;
using AtrBot.Shared.Contracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.DurableTask;
using Microsoft.Extensions.Logging;

namespace AtrBot.Server.Orchestrations;

public sealed class ThinkingOrchestrator
{
    private readonly DocumentSearchService _searchService;
    private readonly StatusNotifier _statusNotifier;
    private readonly ILogger _logger;

    public ThinkingOrchestrator(DocumentSearchService searchService, StatusNotifier statusNotifier, ILoggerFactory loggerFactory)
    {
        _searchService = searchService;
        _statusNotifier = statusNotifier;
        _logger = loggerFactory.CreateLogger<ThinkingOrchestrator>();
    }

    [Function(nameof(ThinkingOrchestrator))]
    public async Task RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var request = context.GetInput<ChatRequest>() ?? throw new InvalidOperationException("ChatRequest is required.");
        _logger.LogInformation("Starting thinking orchestration for connection {ConnectionId}.", context.InstanceId);

        var interim = new ChatResponse("Thinking...", Array.Empty<Citation>(), "Thinking");
        await context.CallActivityAsync(nameof(SendStatusUpdate), interim);

        var citations = await context.CallActivityAsync<IReadOnlyList<Citation>>(nameof(PerformSearch), request);

        // Placeholder call for long-running LLM invocation. Replace with real API call.
        await Task.Delay(TimeSpan.FromSeconds(1));

        var finalResponse = new ChatResponse(
            "Detailed reasoning is not yet connected. Replace this stub with the thinking LLM integration.",
            citations,
            "Completed");

        await context.CallActivityAsync(nameof(SendStatusUpdate), finalResponse);
    }

    [Function(nameof(PerformSearch))]
    public async Task<IReadOnlyList<Citation>> PerformSearch([ActivityTrigger] ChatRequest request, FunctionContext context)
    {
        return await _searchService.SearchAsync(request.Text, request.DocumentIds, cancellationToken: context.CancellationToken);
    }

    [Function(nameof(SendStatusUpdate))]
    public async Task SendStatusUpdate([ActivityTrigger] ChatResponse response, FunctionContext context)
    {
        var connectionId = context.BindingContext.BindingData["connectionId"]?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            _logger.LogWarning("No connection id provided for status update.");
            return;
        }

        await _statusNotifier.BroadcastStatusAsync(connectionId, response, context.CancellationToken);
    }
}
