using System.Collections;
using System.Reflection;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class PilotOperationsPolicyTests
{
    private static Assembly ApiAssembly => typeof(AtlasDbContext).Assembly;

    [Fact]
    public void Pilot_operations_contract_types_exist()
    {
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.IntelligenceRunRecord"));
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.PilotOperationRecord"));
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.PilotOperationActions"));
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.PilotOperationsPolicy"));
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.PilotSupportNoteRequest"));
        Assert.NotNull(ApiAssembly.GetType("Atlas.Api.PilotWithdrawRequest"));
    }

    [Fact]
    public void Pilot_operation_actions_are_stable()
    {
        var type = RequireType("PilotOperationActions");
        Assert.Equal("support-note", Constant(type, "SupportNote"));
        Assert.Equal("profile-correction", Constant(type, "ProfileCorrection"));
        Assert.Equal("opportunity-prepared", Constant(type, "OpportunityPrepared"));
        Assert.Equal("opportunity-withdrawn", Constant(type, "OpportunityWithdrawn"));
    }

    [Fact]
    public void Support_note_is_trimmed_required_and_bounded()
    {
        var requestType = RequireType("PilotSupportNoteRequest");
        Assert.Empty(Validate("ValidateSupportNote", Activator.CreateInstance(requestType, [" Need owner follow-up. "])!));
        Assert.NotEmpty(Validate("ValidateSupportNote", Activator.CreateInstance(requestType, ["   "])!));
        Assert.NotEmpty(Validate("ValidateSupportNote", Activator.CreateInstance(requestType, [new string('x', 2001)])!));
    }

    [Fact]
    public void Withdrawal_requires_bounded_reason()
    {
        var requestType = RequireType("PilotWithdrawRequest");
        Assert.Empty(Validate("ValidateWithdrawal", Activator.CreateInstance(requestType, [" Unsafe claim. ", (uint)1])!));
        Assert.NotEmpty(Validate("ValidateWithdrawal", Activator.CreateInstance(requestType, ["   ", (uint)1])!));
        Assert.NotEmpty(Validate("ValidateWithdrawal", Activator.CreateInstance(requestType, [new string('x', 2001), (uint)1])!));
    }

    private static IDictionary Validate(string methodName, object request)
    {
        var policy = RequireType("PilotOperationsPolicy");
        var method = policy.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);
        var result = method!.Invoke(null, [request]);
        return Assert.IsAssignableFrom<IDictionary>(result);
    }

    private static string Constant(Type type, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(field);
        return Assert.IsType<string>(field!.GetValue(null));
    }

    private static Type RequireType(string name)
    {
        var type = ApiAssembly.GetType($"Atlas.Api.{name}");
        Assert.NotNull(type);
        return type!;
    }
}
