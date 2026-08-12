namespace Atlas.Api;

public static class BusinessProfileRequestValidationExtensions
{
    public static Dictionary<string, string[]> Validate(this UpsertBusinessProfileRequest request, string? existingSource)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Language))
            errors[nameof(request.Language)] = ["Language is required."];

        switch (request.Source)
        {
            case FieldSources.Owner:
                break;
            case FieldSources.Public:
                if (!request.OwnerConfirmed)
                    errors[nameof(request.OwnerConfirmed)] = ["Publicly sourced profile data must be owner-confirmed."];
                break;
            case FieldSources.OperatorAssisted:
                if (!string.Equals(existingSource, FieldSources.OperatorAssisted, StringComparison.Ordinal))
                    errors[nameof(request.Source)] = ["Operator-assisted provenance can only be preserved from an existing operator-assisted profile."];
                else if (!request.OwnerConfirmed)
                    errors[nameof(request.OwnerConfirmed)] = ["Operator-assisted profile data must be reviewed and owner-confirmed."];
                break;
            default:
                errors[nameof(request.Source)] = ["Source must be owner, public, or an existing operator-assisted profile."];
                break;
        }

        return errors;
    }
}
