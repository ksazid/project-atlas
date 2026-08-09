namespace Atlas.Api;

public static class ProgressiveQuestionAnswerTypes
{
    public const string SingleChoice = "single-choice";
    public const string MultiChoice = "multi-choice";
    public const string ShortText = "short-text";
}

public static class BusinessQuestionProgressStatuses
{
    public const string Answered = "answered";
    public const string Skipped = "skipped";
}

public sealed record ProgressiveQuestionDefinition(
    string QuestionKey,
    string TargetContextKey,
    IReadOnlySet<string> Categories,
    int Priority,
    string Prompt,
    string? Helper,
    string AnswerType,
    IReadOnlyList<string> Options,
    int? MaxSelections,
    int? MaxLength,
    IReadOnlySet<string> MaterialityTags);

public static class ProgressiveQuestionCatalogueV1
{
    public const string CatalogueKey = "progressive-onboarding";
    public const string Version = "1";

    private static readonly IReadOnlySet<string> GenericCategory = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "generic-business" };

    public static IReadOnlyList<ProgressiveQuestionDefinition> Definitions { get; } =
    [
        CategoryChoice("restaurant-cafe.service-channel", "restaurant-cafe", "How do most customers order from you?", ["Dine in", "Takeaway", "Own website/app", "Marketplace/platform"], ["channel", "demand"]),
        CategoryChoice("beauty-personal-care.service-model", "beauty-personal-care", "How do customers usually receive your services?", ["Walk in", "Appointment", "At home/mobile", "Online"], ["channel", "capacity"]),
        CategoryChoice("retail.sales-channel", "retail", "Where do most customer purchases happen?", ["Physical shop", "Own website/app", "Marketplace/platform", "Phone/message"], ["channel", "demand"]),
        CategoryChoice("ecommerce.sales-channel", "ecommerce", "Where do most customer orders come from?", ["Own website/app", "Marketplace/platform", "Social media/message", "Wholesale/B2B"], ["channel", "demand"]),
        CategoryChoice("home-local-services.service-channel", "home-local-services", "How do customers usually book your services?", ["Phone", "Message/chat", "Website/form", "Marketplace/platform"], ["channel", "demand"]),
        CategoryChoice("professional-services.delivery-model", "professional-services", "How do clients usually work with you?", ["In person", "Remote/online", "On-site at client", "Mixed"], ["channel", "capacity"]),
        CategoryChoice("fitness-wellness.service-model", "fitness-wellness", "How do customers usually use your service?", ["Classes", "Appointments", "Open access/membership", "Online"], ["channel", "capacity"]),
        CategoryChoice("hospitality-accommodation.booking-channel", "hospitality-accommodation", "Where do most bookings come from?", ["Direct", "Own website", "Booking marketplace", "Agent/partner"], ["channel", "demand"]),

        new(
            "generic.primary-channel",
            "primarychannels",
            GenericCategory,
            100,
            "How do customers usually buy from you?",
            "This helps Atlas keep suggestions practical for the way you operate.",
            ProgressiveQuestionAnswerTypes.MultiChoice,
            ["In person", "Phone/message", "Own website/app", "Marketplace/platform"],
            3,
            null,
            Tags("channel", "demand")),
        new(
            "generic.busy-periods",
            "busyperiods",
            GenericCategory,
            90,
            "When are you usually busiest?",
            "A broad pattern is enough; Atlas does not need exact customer-level data.",
            ProgressiveQuestionAnswerTypes.MultiChoice,
            ["Weekday mornings", "Weekday afternoons", "Weekday evenings", "Weekends", "Seasonal/events"],
            2,
            null,
            Tags("demand", "capacity")),
        new(
            "generic.primary-constraint",
            "constraints",
            GenericCategory,
            80,
            "What limits the business most right now?",
            "Choose the constraint that most changes what is practical today.",
            ProgressiveQuestionAnswerTypes.SingleChoice,
            ["Time", "Staffing", "Capacity", "Cash/budget", "Demand", "Something else"],
            1,
            null,
            Tags("constraint", "capacity")),
        new(
            "generic.customer-groups",
            "customergroups",
            GenericCategory,
            70,
            "Who do you mainly serve?",
            "Describe customer groups at a business level, without names or personal details.",
            ProgressiveQuestionAnswerTypes.ShortText,
            [],
            null,
            240,
            Tags("customer")),
        new(
            "generic.current-priority",
            "currentpriorities",
            GenericCategory,
            60,
            "What deserves the most attention right now?",
            "A short near-term priority is enough.",
            ProgressiveQuestionAnswerTypes.ShortText,
            [],
            null,
            240,
            Tags("priority"))
    ];

    public static IReadOnlyList<ProgressiveQuestionDefinition> Select(
        string category,
        IReadOnlyCollection<BusinessContextEntry> context,
        IReadOnlyCollection<BusinessQuestionProgress> progress)
    {
        var canonicalCategory = BusinessCategoryTaxonomy.IsKnownCategory(category)
            ? category.Trim().ToLowerInvariant()
            : BusinessCategoryTaxonomy.Generic.Key;

        var authoritativeContextKeys = context
            .Where(x => x.OwnerConfirmed && !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => x.Key.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var completedQuestionKeys = progress
            .Where(x => string.Equals(x.CatalogueKey, CatalogueKey, StringComparison.OrdinalIgnoreCase) &&
                        (x.Status == BusinessQuestionProgressStatuses.Skipped || x.Status == BusinessQuestionProgressStatuses.Answered))
            .Select(x => x.QuestionKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var candidates = Definitions
            .Where(question =>
                question.Categories.Contains(canonicalCategory) ||
                question.Categories.Contains(BusinessCategoryTaxonomy.Generic.Key))
            .Where(question => !authoritativeContextKeys.Contains(question.TargetContextKey))
            .Where(question => !completedQuestionKeys.Contains(question.QuestionKey))
            .OrderByDescending(question => question.Priority)
            .ThenByDescending(question => question.Categories.Contains(canonicalCategory) && !question.Categories.Contains(BusinessCategoryTaxonomy.Generic.Key))
            .ThenBy(question => question.QuestionKey, StringComparer.Ordinal)
            .ToList();

        var selected = new List<ProgressiveQuestionDefinition>(5);
        var targetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var question in candidates)
        {
            if (!targetKeys.Add(question.TargetContextKey)) continue;
            selected.Add(question);
            if (selected.Count == 5) break;
        }

        return selected;
    }

    private static ProgressiveQuestionDefinition CategoryChoice(
        string questionKey,
        string category,
        string prompt,
        IReadOnlyList<string> options,
        IReadOnlyList<string> tags) => new(
            questionKey,
            "primarychannels",
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { category },
            120,
            prompt,
            "Choose the closest fit. You can change business context later.",
            ProgressiveQuestionAnswerTypes.MultiChoice,
            options,
            2,
            null,
            new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase));

    private static IReadOnlySet<string> Tags(params string[] values) => new HashSet<string>(values, StringComparer.OrdinalIgnoreCase);
}
