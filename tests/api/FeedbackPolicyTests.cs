using System.Collections;
using System.Reflection;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class FeedbackPolicyTests
{
    private static Assembly ApiAssembly => typeof(AtlasDbContext).Assembly;

    [Fact]
    public void Feedback_contract_types_exist()
    {
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.SubmitFeedbackRequest"));
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.FeedbackPolicy"));
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.FeedbackRecord"));
    }

    [Theory]
    [InlineData("opportunity-rating", true, null, "useful", true)]
    [InlineData("opportunity-rating", false, null, "useful", false)]
    [InlineData("opportunity-rating", true, null, null, false)]
    [InlineData("unsafe-guidance", true, null, null, true)]
    [InlineData("unsafe-guidance", false, null, null, false)]
    [InlineData("incorrect-context", false, "primarycustomers", null, true)]
    [InlineData("general-feedback", false, null, null, true)]
    [InlineData("support-request", false, null, null, true)]
    public void Validation_matches_kind_contract(string kind, bool hasOpportunity, string? contextKey, string? usefulness, bool valid)
    {
        var request = CreateRequest(kind, hasOpportunity ? Guid.NewGuid() : null, contextKey, usefulness, " owner note ");
        Assert.Equal(valid, Validate(request).Count == 0);
    }

    [Theory]
    [InlineData("unsafe-guidance", "useful", null)]
    [InlineData("general-feedback", null, "primarycustomers")]
    [InlineData("support-request", "not-useful", null)]
    public void Validation_rejects_fields_not_supported_by_kind(string kind, string? usefulness, string? contextKey)
    {
        var request = CreateRequest(kind, kind == "unsafe-guidance" ? Guid.NewGuid() : null, contextKey, usefulness, null);
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void Validation_rejects_unknown_kind_and_oversized_fields()
    {
        var request = CreateRequest("other", null, new string('x', 121), null, new string('x', 1201));
        Assert.NotEmpty(Validate(request));
    }

    [Fact]
    public void Normalize_message_trims_and_whitespace_becomes_null()
    {
        var policy = RequireType("FeedbackPolicy");
        var method = policy.GetMethod("NormalizeMessage", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        Assert.Equal("note", method!.Invoke(null, ["  note  "]));
        Assert.Null(method.Invoke(null, ["   "]));
    }

    private static Type RequireType(string name)
    {
        var type = ApiAssembly.GetType($"Atlas.Api.{name}");
        Assert.NotNull(type);
        return type!;
    }

    private static object CreateRequest(string kind, Guid? opportunityId, string? contextKey, string? usefulness, string? message)
    {
        var type = RequireType("SubmitFeedbackRequest");
        var value = Activator.CreateInstance(type, [kind, opportunityId, contextKey, usefulness, message]);
        Assert.NotNull(value);
        return value!;
    }

    private static IDictionary Validate(object request)
    {
        var policy = RequireType("FeedbackPolicy");
        var method = policy.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        var result = method!.Invoke(null, [request]);
        Assert.IsAssignableFrom<IDictionary>(result);
        return (IDictionary)result!;
    }
}