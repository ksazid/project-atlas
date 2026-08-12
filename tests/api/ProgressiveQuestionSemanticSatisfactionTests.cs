using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class ProgressiveQuestionSemanticSatisfactionTests
{
    [Fact]
    public void Owner_confirmed_operating_channels_suppress_equivalent_primary_channel_prompts()
    {
        var businessId = Guid.NewGuid();
        var context = new[]
        {
            new BusinessContextEntry
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                Key = "operatingchannels",
                Value = "Takeaway | Delivery",
                Source = FieldSources.Owner,
                OwnerConfirmed = true,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        var selected = ProgressiveQuestionCatalogueV1.Select("restaurant-cafe", context, []);

        Assert.DoesNotContain(selected, question =>
            string.Equals(question.TargetContextKey, "primarychannels", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Unconfirmed_or_blank_operating_channels_do_not_suppress_primary_channel_prompts()
    {
        var businessId = Guid.NewGuid();
        var unconfirmed = new[]
        {
            new BusinessContextEntry
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                Key = "operatingchannels",
                Value = "Delivery",
                Source = FieldSources.Public,
                OwnerConfirmed = false,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };
        var blank = new[]
        {
            new BusinessContextEntry
            {
                Id = Guid.NewGuid(),
                BusinessId = businessId,
                Key = "operatingchannels",
                Value = "   ",
                Source = FieldSources.Owner,
                OwnerConfirmed = true,
                UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        Assert.Contains(ProgressiveQuestionCatalogueV1.Select("restaurant-cafe", unconfirmed, []), question =>
            string.Equals(question.TargetContextKey, "primarychannels", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(ProgressiveQuestionCatalogueV1.Select("restaurant-cafe", blank, []), question =>
            string.Equals(question.TargetContextKey, "primarychannels", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Hours_and_service_periods_do_not_suppress_busy_periods_or_other_context()
    {
        var businessId = Guid.NewGuid();
        var context = new[]
        {
            OwnerContext(businessId, "openinghours", "Monday 09:00-17:00"),
            OwnerContext(businessId, "serviceperiods", "Lunch | Dinner")
        };

        var selected = ProgressiveQuestionCatalogueV1.Select("generic-business", context, []);

        Assert.Contains(selected, question => question.QuestionKey == "generic.busy-periods");
        Assert.Contains(selected, question => question.QuestionKey == "generic.primary-constraint");
    }

    private static BusinessContextEntry OwnerContext(Guid businessId, string key, string value) => new()
    {
        Id = Guid.NewGuid(),
        BusinessId = businessId,
        Key = key,
        Value = value,
        Source = FieldSources.Owner,
        OwnerConfirmed = true,
        UpdatedAt = DateTimeOffset.UtcNow
    };
}
