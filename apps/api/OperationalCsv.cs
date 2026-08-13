using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Atlas.Api;

public static class OperationalMetricCatalogue
{
    public static readonly IReadOnlySet<string> Approved = new HashSet<string>(StringComparer.Ordinal)
    {
        "gross-sales", "net-sales", "order-count", "average-order-value", "units-sold",
        "discount-amount", "refund-amount", "cancellation-count", "product-sales",
        "category-sales", "channel-sales", "channel-order-count"
    };
}

public static class OperationalCsvErrorCodes
{
    public const string FileTooLarge = "file-too-large";
    public const string TooManyRows = "too-many-rows";
    public const string UnsupportedSchema = "unsupported-schema";
    public const string ProhibitedPaymentData = "prohibited-payment-data";
    public const string MixedCurrency = "mixed-currency";
    public const string AmbiguousFormat = "ambiguous-format";
}

public sealed class OperationalCsvException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed record OperationalCsvPreview(
    int RowCount,
    int OrderCount,
    DateOnly EarliestBusinessDate,
    DateOnly LatestBusinessDate,
    IReadOnlyList<string> RecognizedColumns,
    IReadOnlyList<string> IgnoredSensitiveColumns,
    IReadOnlyList<string> MetricKeys,
    string Fingerprint);

public sealed record OperationalObservation(
    string MetricKey,
    decimal Value,
    string Unit,
    string? Currency,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyDictionary<string, string>? Dimensions);

public sealed record OperationalNormalizationResult(
    OperationalCsvPreview Preview,
    IReadOnlyList<OperationalObservation> Observations);

public static class OperationalCsvReader
{
    public const long MaximumFileBytes = 10 * 1024 * 1024;
    public const int MaximumRows = 100_000;

    private static readonly Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["date"] = "date", ["business date"] = "date", ["order date"] = "date",
        ["transaction date"] = "date", ["timestamp"] = "date", ["order timestamp"] = "date",
        ["order id"] = "order-id", ["transaction id"] = "order-id", ["order number"] = "order-id",
        ["gross sales"] = "gross-sales", ["gross amount"] = "gross-sales", ["amount"] = "gross-sales",
        ["net sales"] = "net-sales", ["net amount"] = "net-sales",
        ["currency"] = "currency", ["currency code"] = "currency",
        ["item"] = "product", ["product"] = "product", ["item name"] = "product",
        ["category"] = "category", ["product category"] = "category",
        ["qty"] = "quantity", ["quantity"] = "quantity", ["units"] = "quantity",
        ["channel"] = "channel", ["sales channel"] = "channel",
        ["discount"] = "discount-amount", ["discount amount"] = "discount-amount",
        ["refund"] = "refund-amount", ["refund amount"] = "refund-amount",
        ["cancelled"] = "cancelled", ["canceled"] = "cancelled"
    };

    private static readonly HashSet<string> Sensitive = new(StringComparer.OrdinalIgnoreCase)
    {
        "customer name", "customer email", "email", "phone", "customer phone",
        "delivery address", "address", "order notes", "customer notes", "notes"
    };

    private static readonly HashSet<string> Prohibited = new(StringComparer.OrdinalIgnoreCase)
    {
        "pan", "card number", "payment card", "credit card", "cvv", "cvc", "security code"
    };

    public static async Task<OperationalCsvPreview> PreviewAsync(
        Stream source, Business business, CancellationToken cancellationToken) =>
        (await ParseAsync(source, business, cancellationToken)).Preview;

    public static async Task<OperationalNormalizationResult> NormalizeAsync(
        Stream source, Business business, CancellationToken cancellationToken)
    {
        var parsed = await ParseAsync(source, business, cancellationToken);
        var observations = new List<OperationalObservation>();

        foreach (var day in parsed.Rows.GroupBy(row => row.Date).OrderBy(group => group.Key))
        {
            AddMoney(observations, day, "gross-sales", row => row.GrossSales, business.Currency);
            AddMoney(observations, day, "net-sales", row => row.NetSales, business.Currency);
            AddMoney(observations, day, "discount-amount", row => row.DiscountAmount, business.Currency);
            AddMoney(observations, day, "refund-amount", row => row.RefundAmount, business.Currency);

            var orderCount = day.Select(row => row.OrderId).Where(value => value is not null).Distinct(StringComparer.Ordinal).Count();
            if (orderCount == 0) orderCount = day.Count();
            observations.Add(new("order-count", orderCount, "count", null, day.Key, day.Key, null));

            var sales = day.Sum(row => row.GrossSales ?? row.NetSales ?? 0m);
            if (sales != 0m && orderCount > 0)
                observations.Add(new("average-order-value", sales / orderCount, "currency", business.Currency, day.Key, day.Key, null));

            var units = day.Where(row => row.Quantity.HasValue).Sum(row => row.Quantity ?? 0m);
            if (day.Any(row => row.Quantity.HasValue))
                observations.Add(new("units-sold", units, "count", null, day.Key, day.Key, null));

            AddDimensionAggregates(observations, day, business.Currency, "product", row => row.Product, "product-sales");
            AddDimensionAggregates(observations, day, business.Currency, "category", row => row.Category, "category-sales");
            AddDimensionAggregates(observations, day, business.Currency, "channel", row => row.Channel, "channel-sales");
        }

        return new(parsed.Preview, observations);
    }

    private static async Task<ParsedCsv> ParseAsync(Stream source, Business business, CancellationToken cancellationToken)
    {
        if (source.CanSeek && source.Length > MaximumFileBytes)
            throw Error(OperationalCsvErrorCodes.FileTooLarge, "CSV files must be 10 MiB or smaller.");

        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(chunk.AsMemory(), cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > MaximumFileBytes)
                throw Error(OperationalCsvErrorCodes.FileTooLarge, "CSV files must be 10 MiB or smaller.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        var bytes = buffer.ToArray();
        var fingerprint = Convert.ToHexStringLower(SHA256.HashData(bytes));
        var records = ParseRecords(Encoding.UTF8.GetString(bytes));
        if (records.Count < 2)
            throw Error(OperationalCsvErrorCodes.UnsupportedSchema, "The CSV must contain a header and at least one data row.");

        var headers = records[0].Select(NormalizeHeader).ToArray();
        if (headers.Any(Prohibited.Contains))
            throw Error(OperationalCsvErrorCodes.ProhibitedPaymentData, "Payment-card secret columns are not accepted.");

        var ignored = headers.Where(Sensitive.Contains).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var mapped = headers.Select(header => Aliases.GetValueOrDefault(header)).ToArray();
        var recognized = headers.Where((_, index) => mapped[index] is not null).ToArray();
        if (!mapped.Contains("date") || !mapped.Any(IsValueBearing))
            throw Error(OperationalCsvErrorCodes.UnsupportedSchema, "A reliable date and monetary or quantity field are required.");

        var rows = new List<ParsedRow>();
        var currencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var recordIndex = 1; recordIndex < records.Count; recordIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (records[recordIndex].All(string.IsNullOrWhiteSpace)) continue;
            if (rows.Count >= MaximumRows)
                throw Error(OperationalCsvErrorCodes.TooManyRows, "CSV files may contain at most 100,000 data rows.");
            if (records[recordIndex].Count != headers.Length)
                throw Error(OperationalCsvErrorCodes.AmbiguousFormat, "A row has an ambiguous delimiter or decimal format.");

            var values = mapped.Select((canonical, index) => (canonical, value: records[recordIndex][index].Trim()))
                .Where(pair => pair.canonical is not null)
                .ToDictionary(pair => pair.canonical!, pair => pair.value, StringComparer.Ordinal);
            if (!DateOnly.TryParseExact(values["date"], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                throw Error(OperationalCsvErrorCodes.AmbiguousFormat, "Dates must use the unambiguous yyyy-MM-dd format.");

            var currency = values.GetValueOrDefault("currency");
            if (!string.IsNullOrWhiteSpace(currency)) currencies.Add(currency.ToUpperInvariant());
            rows.Add(new(date, values.GetValueOrDefault("order-id"), Money(values, "gross-sales"), Money(values, "net-sales"),
                Money(values, "discount-amount"), Money(values, "refund-amount"), Number(values, "quantity"),
                values.GetValueOrDefault("product"), values.GetValueOrDefault("category"), values.GetValueOrDefault("channel")));
        }

        if (currencies.Count > 1 || currencies.Any(currency => !currency.Equals(business.Currency, StringComparison.OrdinalIgnoreCase)))
            throw Error(OperationalCsvErrorCodes.MixedCurrency, "All rows must use the Business currency.");

        var metrics = DerivableMetrics(mapped, rows);
        var preview = new OperationalCsvPreview(rows.Count,
            rows.Select(row => row.OrderId).Where(value => value is not null).Distinct(StringComparer.Ordinal).Count(),
            rows.Min(row => row.Date), rows.Max(row => row.Date), recognized, ignored, metrics, fingerprint);
        return new(preview, rows);
    }

    private static List<List<string>> ParseRecords(string text)
    {
        var records = new List<List<string>>();
        var record = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '"')
            {
                if (quoted && index + 1 < text.Length && text[index + 1] == '"') { field.Append('"'); index++; }
                else quoted = !quoted;
            }
            else if (character == ',' && !quoted) { record.Add(field.ToString()); field.Clear(); }
            else if ((character == '\n' || character == '\r') && !quoted)
            {
                if (character == '\r' && index + 1 < text.Length && text[index + 1] == '\n') index++;
                record.Add(field.ToString()); field.Clear();
                if (record.Any(value => value.Length > 0)) records.Add(record);
                record = new();
            }
            else field.Append(character);
        }
        if (quoted) throw Error(OperationalCsvErrorCodes.AmbiguousFormat, "The CSV contains an unterminated quoted field.");
        if (field.Length > 0 || record.Count > 0) { record.Add(field.ToString()); records.Add(record); }
        return records;
    }

    private static string NormalizeHeader(string value) => string.Join(' ', value.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    private static bool IsValueBearing(string? key) => key is "gross-sales" or "net-sales" or "quantity" or "discount-amount" or "refund-amount" or "order-id";
    private static decimal? Money(IReadOnlyDictionary<string, string> values, string key) => Number(values, key);
    private static decimal? Number(IReadOnlyDictionary<string, string> values, string key)
    {
        var value = values.GetValueOrDefault(key);
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed))
            throw Error(OperationalCsvErrorCodes.AmbiguousFormat, $"{key} must use an invariant decimal format.");
        return parsed;
    }

    private static IReadOnlyList<string> DerivableMetrics(string?[] mapped, IReadOnlyList<ParsedRow> rows)
    {
        var metrics = new HashSet<string>(StringComparer.Ordinal) { "order-count" };
        if (mapped.Contains("gross-sales")) metrics.Add("gross-sales");
        if (mapped.Contains("net-sales")) metrics.Add("net-sales");
        if (mapped.Contains("gross-sales") || mapped.Contains("net-sales")) metrics.Add("average-order-value");
        if (mapped.Contains("quantity")) metrics.Add("units-sold");
        if (mapped.Contains("discount-amount")) metrics.Add("discount-amount");
        if (mapped.Contains("refund-amount")) metrics.Add("refund-amount");
        if (mapped.Contains("product") && rows.Any(row => row.GrossSales.HasValue || row.NetSales.HasValue)) metrics.Add("product-sales");
        if (mapped.Contains("category") && rows.Any(row => row.GrossSales.HasValue || row.NetSales.HasValue)) metrics.Add("category-sales");
        if (mapped.Contains("channel") && rows.Any(row => row.GrossSales.HasValue || row.NetSales.HasValue)) metrics.Add("channel-sales");
        return metrics.Order(StringComparer.Ordinal).ToArray();
    }

    private static void AddMoney(List<OperationalObservation> target, IGrouping<DateOnly, ParsedRow> day, string metric, Func<ParsedRow, decimal?> selector, string currency)
    {
        if (!day.Any(row => selector(row).HasValue)) return;
        target.Add(new(metric, day.Sum(row => selector(row) ?? 0m), "currency", currency, day.Key, day.Key, null));
    }

    private static void AddDimensionAggregates(List<OperationalObservation> target, IGrouping<DateOnly, ParsedRow> day, string currency, string dimension, Func<ParsedRow, string?> selector, string metric)
    {
        foreach (var group in day.Where(row => !string.IsNullOrWhiteSpace(selector(row)) && (row.GrossSales.HasValue || row.NetSales.HasValue)).GroupBy(row => selector(row)!, StringComparer.OrdinalIgnoreCase))
            target.Add(new(metric, group.Sum(row => row.GrossSales ?? row.NetSales ?? 0m), "currency", currency, day.Key, day.Key,
                new Dictionary<string, string>(StringComparer.Ordinal) { [dimension] = group.Key }));
    }

    private static OperationalCsvException Error(string code, string message) => new(code, message);
    private sealed record ParsedCsv(OperationalCsvPreview Preview, IReadOnlyList<ParsedRow> Rows);
    private sealed record ParsedRow(DateOnly Date, string? OrderId, decimal? GrossSales, decimal? NetSales,
        decimal? DiscountAmount, decimal? RefundAmount, decimal? Quantity, string? Product, string? Category, string? Channel);
}
