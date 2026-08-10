namespace Atlas.Api;

public static class RestaurantCafeKnowledgeManifestV2
{
    public const string PackKey = "restaurant-cafe-intelligence";
    public const string Version = "1.0";

    public static KnowledgePackManifestV2 Create() => new(
        SchemaVersion: 2,
        PackKey: PackKey,
        ExactVersion: Version,
        Layer: KnowledgePackLayers.Category,
        SupportedCategoryKeys: ["restaurant-cafe"],
        SupportedSubcategoryKeys: ["restaurant", "cafe", "bakery", "takeaway"],
        Kpis:
        [
            new KnowledgeKpiDefinition("ordering-path-clarity", "Ordering path clarity", "Observe whether a customer can identify a current ordering or booking path from confirmed business information."),
            new KnowledgeKpiDefinition("hours-consistency", "Hours consistency", "Compare owner-confirmed opening hours with available public business information without assuming either source is automatically correct."),
            new KnowledgeKpiDefinition("offer-visibility", "Current offer visibility", "Observe whether an owner-confirmed current offer or priority is clearly represented in the channels the owner chooses to review."),
            new KnowledgeKpiDefinition("reputation-follow-up", "Reputation follow-up", "Track whether a confirmed reputation signal has a practical owner-reviewed follow-up action.")
        ],
        EvidenceRules:
        [
            new KnowledgeEvidenceRule("restaurant-category-confirmed", "Require a confirmed canonical Restaurant & Café category before applying this pack.", 1, true),
            new KnowledgeEvidenceRule("ordering-channel-confirmed", "Require an owner-confirmed service or ordering channel before reviewing ordering-path clarity.", 1, true),
            new KnowledgeEvidenceRule("hours-evidence-present", "Require owner-confirmed or attributable public hours evidence before reviewing hours consistency.", 1, false),
            new KnowledgeEvidenceRule("current-offer-confirmed", "Require an owner-confirmed current offer or near-term priority before reviewing offer visibility.", 1, true),
            new KnowledgeEvidenceRule("reputation-signal-present", "Require an attributable reputation signal or owner-confirmed reputation concern before suggesting a follow-up.", 1, false)
        ],
        OpportunityPatterns:
        [
            new KnowledgeOpportunityPattern(
                "ordering-path-clarity-review",
                "Review the clearest ordering path for customers",
                ["growth", "customer-experience", "efficiency"],
                ["restaurant-category-confirmed", "ordering-channel-confirmed"],
                "A confirmed ordering channel gives you a specific customer path that can be checked for clarity.",
                "The business context already identifies how customers currently order, so a bounded review can be useful now.",
                "A clearer owner-reviewed ordering path and an observable follow-up signal.",
                "Low",
                "Medium",
                "ordering-path-review-checklist",
                7),
            new KnowledgeOpportunityPattern(
                "hours-consistency-review",
                "Check opening-hours consistency",
                ["customer-experience", "efficiency", "risk-reduction"],
                ["restaurant-category-confirmed", "hours-evidence-present"],
                "Conflicting or stale hours can create avoidable customer friction when the evidence actually differs.",
                "Hours evidence is available to compare without inventing missing operating details.",
                "A documented hours check and any owner-approved correction that follows from it.",
                "Low",
                "Medium",
                "hours-consistency-checklist",
                14),
            new KnowledgeOpportunityPattern(
                "current-offer-visibility-review",
                "Review visibility of the current offer",
                ["growth", "retention", "customer-experience"],
                ["restaurant-category-confirmed", "current-offer-confirmed"],
                "An owner-confirmed offer or priority can be reviewed for consistency across the channels the owner already uses.",
                "The offer is current and confirmed, so the review does not require Atlas to invent a promotion.",
                "A clearer representation of an existing owner-approved offer and a follow-up observation.",
                "Low",
                "Medium",
                "offer-visibility-checklist",
                7),
            new KnowledgeOpportunityPattern(
                "reputation-signal-follow-up",
                "Review one reputation signal and choose a follow-up",
                ["retention", "customer-experience", "risk-reduction"],
                ["restaurant-category-confirmed", "reputation-signal-present"],
                "A specific attributable reputation signal can reveal a bounded issue or strength worth reviewing.",
                "A real signal is present, so the owner can choose whether any practical follow-up is warranted.",
                "One owner-reviewed response or operational follow-up with a recorded observation afterward.",
                "Medium",
                "Medium",
                "reputation-follow-up-checklist",
                14)
        ],
        ExecutionTemplates:
        [
            new KnowledgeExecutionTemplate("ordering-path-review-checklist", "checklist", "Ordering path review", "1. Confirm the primary ordering channel.\n2. Follow the customer path using current owner-approved information.\n3. Record one point of friction or confirm that none is evident.\n4. Make only an owner-approved change.\n5. Recheck the same path and record the observation."),
            new KnowledgeExecutionTemplate("hours-consistency-checklist", "checklist", "Opening-hours consistency review", "1. Start from owner-confirmed hours.\n2. Compare only attributable public information already available to Atlas.\n3. Record any mismatch.\n4. Let the owner choose the authoritative correction.\n5. Recheck after the owner-approved update."),
            new KnowledgeExecutionTemplate("offer-visibility-checklist", "checklist", "Current offer visibility review", "1. Reconfirm the current owner-approved offer or priority.\n2. Choose the channels the owner wants to review.\n3. Check wording, availability and timing for consistency.\n4. Update only owner-approved content.\n5. Record one observable follow-up signal."),
            new KnowledgeExecutionTemplate("reputation-follow-up-checklist", "checklist", "Reputation signal follow-up", "1. Review the attributable signal in context.\n2. Separate observed facts from assumptions.\n3. Choose one owner-controlled response or operational follow-up if appropriate.\n4. Complete the action.\n5. Record what was observed afterward without claiming causation.")
        ],
        MeasurementSuggestions:
        [
            "For ordering-path work, record the same observable path before and after any owner-approved change.",
            "For hours, record whether owner-confirmed and attributable public information agree after review.",
            "For an existing offer, choose one channel-level observation such as presence, clarity or response count without attributing causation.",
            "For reputation follow-up, record the specific signal, the owner action and any later observation as separate facts."
        ],
        Seasonality:
        [
            "Check local holidays, tourist periods, events and the business's own busy periods before prioritizing time-sensitive Restaurant & Café actions.",
            "Treat seasonal timing as context for prioritization, not proof that a particular action will produce a business result."
        ],
        Guardrails:
        [
            "Use confirmed business/category/context facts and attributable public evidence; do not invent menu, pricing, hours, demand or reputation facts.",
            "Keep marketplace and ordering-channel guidance provider-neutral unless the owner has supplied an attributable source.",
            "Do not claim that visibility, response, ranking, traffic, conversion or revenue will certainly improve because an action is taken.",
            "External publication or account changes remain owner-controlled and require owner review."
        ]);
}
