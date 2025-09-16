using AtrBot.Infrastructure.Configuration;
using AtrBot.Shared.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AtrBot.Infrastructure.Tests;

public sealed class TierSettingsProviderTests
{
    [Fact]
    public void GetTiers_ReturnsConfiguredTiersOrderedByPrice()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Tiers:Free:Name", "Free" },
            { "Tiers:Free:MonthlyPrice", "0" },
            { "Tiers:Free:StandardQueryQuota", "500" },
            { "Tiers:Free:ThinkingQueryQuota", "20" },
            { "Tiers:Pro:Name", "Pro" },
            { "Tiers:Pro:MonthlyPrice", "29" },
            { "Tiers:Pro:StandardQueryQuota", "2000" },
            { "Tiers:Pro:ThinkingQueryQuota", "100" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        var provider = new TierSettingsProvider(configuration);

        var tiers = provider.GetTiers();

        tiers.Should().HaveCount(2);
        tiers[0].Name.Should().Be("Free");
        tiers[1].Name.Should().Be("Pro");
    }

    [Fact]
    public void FindTier_ReturnsNullWhenNotFound()
    {
        var configuration = new ConfigurationBuilder().Build();
        var provider = new TierSettingsProvider(configuration);

        provider.FindTier("missing").Should().BeNull();
    }
}
