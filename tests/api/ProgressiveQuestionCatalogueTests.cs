using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class ProgressiveQuestionCatalogueTests
{
    [Theory]
    [InlineData("restaurant-cafe")]
    [InlineData("beauty-personal-care")]
    [InlineData("retail")]
    [InlineData("ecommerce")]
    [InlineData("home-local-services")]
    [InlineData("professional-services")]
    [InlineData("fitness-wellness")]
    [InlineData("hospitality-accommodation")]
    [InlineData("generic-business")]
    [InlineData("future-unknown-category")]
    public void Catalogue_SelectsAtMostFiveUsefulQuestionsForEveryFamily(string category)
    {
        var selected = ProgressiveQuestionCatalogueV1.Select(category, [], []);

        Assert.InRange(selected.Count, 1, 5);
        Assert.Equal(selected.Count, selected.Select(x => x.QuestionKey).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(selected, question => Assert.False(string.IsNullOrWhiteSpace(question.TargetContextKey)));
        Assert.All(selected, question => Assert.DoesNotContain(question.TargetContextKey, CanonicalBusinessKeys, StringComparer.OrdinalIgnoreCase));
    }

    [Fact]
    public void Selection_PrefersCategorySpecificQuestionAndIsDeterministic()
    {
        var first = ProgressiveQuestionCatalogueV1.Select("restaurant-cafe", [], []);
        var second = ProgressiveQuestionCatalogueV1.Select("restaurant-cafe", [], []);

        Assert.Equal(first.Select(x => x.QuestionKey), second.Select(x => x.QuestionKey));
        Assert.StartsWith("restaurant-cafe.", first[0].QuestionKey, StringComparison.Ordinal);
    }

    [Fact]
    public void Selection_SuppressesKnownOwnerConfirmedContextWithoutFiller()
    {
        var businessId = Guid.NewGuid();
        var context = new[]
        {
            Context(businessId, "primarychannels", "In person"),
            Context(businessId, "busyperiods", "Weekday mornings"),
            Context(businessId, "constraints", "Staffing"),
            Context(businessId, "customers", "Local residents")
        };

        var selected = ProgressiveQuestionCatalogueV1.Select("generic-business", context, []);

        Assert.DoesNotContain(selected, x => context.Any(entry => string.Equals(entry.Key, x.TargetContextKey, StringComparison.OrdinalIgnoreCase)));
        Assert.Single(selected);
        Assert.Equal("currentpriorities", selected[0].TargetContextKey);
    }

    [Fact]
    public void Selection_SuppressesCustomerQuestionWhenCanonicalCustomersContextExists()
    {
        var businessId = Guid.NewGuid();
        var context = new[] { Context(businessId, "customers", "Local families and commuters") };

        var selected = ProgressiveQuestionCatalogueV1.Select("generic-business", context, []);

        Assert.DoesNotContain(selected, x => x.QuestionKey == "generic.customer-groups");
    }

    [Fact]
    public void Selection_DoesNotTreatUnconfirmedPublicContextAsAuthoritative()
    {
        var businessId = Guid.NewGuid();
        var context = new[]
        {
            new BusinessContextEntry
            {
                Id = Guid.NewGuid(), BusinessId = businessId, Key = "primarychannels", Value = "Delivery",
                Source = FieldSources.Public, OwnerConfirmed = false, UpdatedAt = DateTimeOffset.UtcNow
            }
        };

        var selected = ProgressiveQuestionCatalogueV1.Select("generic-business", context, []);

        Assert.Contains(selected, x => x.TargetContextKey == "primarychannels");
    }

    [Fact]
    public void Selection_SuppressesSkippedQuestionAcrossCatalogueVersions()
    {
        var businessId = Guid.NewGuid();
        var progress = new[]
        {
            BusinessQuestionProgress.Skipped(
                businessId,
                ProgressiveQuestionCatalogueV1.CatalogueKey,
                "0",
                "generic.primary-constraint",
                DateTimeOffset.UtcNow)
        };

        var selected = ProgressiveQuestionCatalogueV1.Select("generic-business", [], progress);

        Assert.DoesNotContain(selected, x => x.QuestionKey == "generic.primary-constraint");
    }

    [Fact]
    public void Selection_SuppressesAnsweredQuestionWhenContextStillExists()
    {
        var businessId = Guid.NewGuid();
        var context = new[] { Context(businessId, "primarychannels", "In person") };
        var progress = new[]
        {
            BusinessQuestionProgress.Answered(
                businessId,
                ProgressiveQuestionCatalogueV1.CatalogueKey,
                ProgressiveQuestionCatalogueV1.Version,
                "generic.primary-channel",
                "primarychannels",
                DateTimeOffset.UtcNow)
        };

        var selected = ProgressiveQuestionCatalogueV1.Select("generic-business", context, progress);

        Assert.DoesNotContain(selected, x => x.QuestionKey == "generic.primary-channel");
    }

    [Fact]
    public void Catalogue_UsesOnlyApprovedBoundedAnswerTypesAndStableKeys()
    {
        var all = ProgressiveQuestionCatalogueV1.Definitions;

        Assert.NotEmpty(all);
        Assert.Equal(all.Count, all.Select(x => x.QuestionKey).Distinct(StringComparer.Ordinal).Count());
        Assert.All(all, question =>
        {
            Assert.Contains(question.AnswerType, new[]
            {
                ProgressiveQuestionAnswerTypes.SingleChoice,
                ProgressiveQuestionAnswerTypes.MultiChoice,
                ProgressiveQuestionAnswerTypes.ShortText
            });
            Assert.True(question.Priority > 0);
            Assert.False(string.IsNullOrWhiteSpace(question.Prompt));
            if (question.AnswerType == ProgressiveQuestionAnswerTypes.ShortText)
                Assert.InRange(question.MaxLength ?? 0, 1, 500);
            else
                Assert.NotEmpty(question.Options);
        });
    }

    private static BusinessContextEntry Context(Guid businessId, string key, string value) => new()
    {
        Id = Guid.NewGuid(), BusinessId = businessId, Key = key, Value = value,
        Source = FieldSources.Owner, OwnerConfirmed = true, UpdatedAt = DateTimeOffset.UtcNow
    };

    private static readonly string[] CanonicalBusinessKeys =
    [
        "name", "category", "country", "timezone", "currency", "primarylocation", "operatingstatus"
    ];
}
