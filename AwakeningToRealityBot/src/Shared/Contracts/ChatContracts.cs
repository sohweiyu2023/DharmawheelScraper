namespace AtrBot.Shared.Contracts;

/// <summary>
/// Represents a user message sent to the conversational agent.
/// </summary>
/// <param name="Text">The natural-language prompt.</param>
/// <param name="DocumentIds">Optional document filters supplied by the user.</param>
/// <param name="Model">Model identifier requested by the user.</param>
public sealed record ChatRequest(string Text, IReadOnlyCollection<string> DocumentIds, string Model);

/// <summary>
/// Minimal response envelope streamed back to the client.
/// </summary>
/// <param name="Message">The generated answer text.</param>
/// <param name="Citations">Collection of citation metadata.</param>
/// <param name="Status">Status indicator (e.g., Thinking, Completed).</param>
public sealed record ChatResponse(string Message, IReadOnlyList<Citation> Citations, string Status);

/// <summary>
/// Identifies a specific supporting chunk surfaced to the user.
/// </summary>
/// <param name="DocumentId">Source document identifier.</param>
/// <param name="Page">Page number within the original document.</param>
/// <param name="ChunkId">Internal chunk identifier.</param>
/// <param name="Snippet">Excerpt for hover tooltips.</param>
public sealed record Citation(string DocumentId, int Page, string ChunkId, string Snippet);

/// <summary>
/// Represents monetisation tier metadata retrieved from Azure App Configuration.
/// </summary>
/// <param name="Name">Tier display name (e.g., Free, Pro).</param>
/// <param name="MonthlyPrice">Monthly price in USD.</param>
/// <param name="StandardQueryQuota">Number of synchronous queries allowed each billing period.</param>
/// <param name="ThinkingQueryQuota">Number of thinking model queries allowed.</param>
/// <param name="Models">Models available under the tier.</param>
public sealed record TierDefinition(
  string Name,
  decimal MonthlyPrice,
  int StandardQueryQuota,
  int ThinkingQueryQuota,
  IReadOnlyList<ModelDefinition> Models
);

/// <summary>
/// Represents a model exposed to the UI.
/// </summary>
/// <param name="Name">LLM identifier.</param>
/// <param name="Type">Category (standard or thinking).</param>
/// <param name="Description">Description displayed in tooltips.</param>
public sealed record ModelDefinition(string Name, string Type, string Description);
