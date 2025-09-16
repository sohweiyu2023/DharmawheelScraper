using System.ClientModel;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Models;
using AtrBot.Shared.Contracts;

namespace AtrBot.Infrastructure.Search;

/// <summary>
/// Wraps Azure AI Search hybrid queries and converts them into structured citations.
/// </summary>
public sealed class DocumentSearchService
{
    private readonly SearchClient _searchClient;

    public DocumentSearchService(SearchClient searchClient)
    {
        _searchClient = searchClient ?? throw new ArgumentNullException(nameof(searchClient));
    }

    public async Task<IReadOnlyList<Citation>> SearchAsync(string query, IEnumerable<string>? documentFilters, int pageSize = 6, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<Citation>();
        }

        var options = new SearchOptions
        {
            Size = pageSize,
            QueryType = SearchQueryType.Semantic,
            SemanticConfigurationName = "atr"
        };

        if (documentFilters is not null && documentFilters.Any())
        {
            var filter = string.Join(" or ", documentFilters.Select(id => $"documentId eq '{id.Replace("'", "''")}'"));
            options.Filter = filter;
        }

        options.Select.AddRange(new[] { "documentId", "page", "chunkId", "text" });

        try
        {
            var results = await _searchClient.SearchAsync<SearchDocument>(query, options, cancellationToken);
            var citations = new List<Citation>();

            await foreach (var result in results.Value.GetResultsAsync())
            {
                var doc = result.Document;
                citations.Add(new Citation(
                    doc.GetString("documentId") ?? string.Empty,
                    (int?)doc.GetInt32("page") ?? 0,
                    doc.GetString("chunkId") ?? Guid.NewGuid().ToString("N"),
                    doc.GetString("text") ?? string.Empty));
            }

            return citations;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return Array.Empty<Citation>();
        }
        catch (ClientResultException)
        {
            return Array.Empty<Citation>();
        }
    }
}
