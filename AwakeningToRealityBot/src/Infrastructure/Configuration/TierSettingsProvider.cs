using AtrBot.Shared.Contracts;
using Microsoft.Extensions.Configuration;

namespace AtrBot.Infrastructure.Configuration;

/// <summary>
/// Loads monetisation tiers and model definitions from Azure App Configuration.
/// </summary>
public sealed class TierSettingsProvider
{
    private readonly IConfiguration _configuration;

    public TierSettingsProvider(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Returns all tier definitions ordered by monthly price ascending.
    /// </summary>
    public IReadOnlyList<TierDefinition> GetTiers()
    {
        var tiersSection = _configuration.GetSection("Tiers");
        if (!tiersSection.Exists())
        {
            return Array.Empty<TierDefinition>();
        }

        var tiers = new List<TierDefinition>();
        foreach (var tierSection in tiersSection.GetChildren())
        {
            var name = tierSection.GetValue<string>("Name") ?? tierSection.Key;
            var monthlyPrice = tierSection.GetValue<decimal?>("MonthlyPrice") ?? 0m;
            var standardQuota = tierSection.GetValue<int?>("StandardQueryQuota") ?? 0;
            var thinkingQuota = tierSection.GetValue<int?>("ThinkingQueryQuota") ?? 0;

            var modelsSection = tierSection.GetSection("Models");
            var models = modelsSection.GetChildren()
                .Select(child => new ModelDefinition(
                    child.GetValue<string>("Name") ?? string.Empty,
                    child.GetValue<string>("Type") ?? "standard",
                    child.GetValue<string>("Description") ?? string.Empty))
                .Where(model => !string.IsNullOrWhiteSpace(model.Name))
                .ToList();

            tiers.Add(new TierDefinition(name, monthlyPrice, standardQuota, thinkingQuota, models));
        }

        return tiers
            .OrderBy(t => t.MonthlyPrice)
            .ThenBy(t => t.Name)
            .ToList();
    }

    /// <summary>
    /// Returns a tier by name (case-insensitive) or <c>null</c> if it is not defined.
    /// </summary>
    public TierDefinition? FindTier(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return GetTiers().FirstOrDefault(tier => string.Equals(tier.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
