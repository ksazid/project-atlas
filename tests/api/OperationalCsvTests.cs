using System.Text;
using Atlas.Api;
using Xunit;

namespace Atlas.Api.Tests;

public sealed class OperationalCsvTests
{
    private static readonly Business Business = Atlas.Api.Business.Create(new(
        "Atlas Cafe", "restaurant-cafe", "Malta", "Europe/Malta", "EUR", "Valletta", "open"));

    [Fact]
    public async Task Preview_recognizes_aliases_quoted_fields_and_derivable_metrics()
    {
        const string csv = "Order Date,Order ID,Gross Sales,Currency,Item,Qty,Channel\n"
            + "2026-08-12,A-1,12.50,EUR,\"Coffee, large\",2,Counter\n"
            + "2026-08-13,A-2,8.00,EUR,Croissant,1,Delivery\n";

        var preview = await OperationalCsvReader.PreviewAsync(Stream(csv), Business, CancellationToken.None);

        Assert.Equal(2, preview.RowCount);
        Assert.Equal(2, preview.OrderCount);
        Assert.Equal(new DateOnly(2026, 8, 12), preview.EarliestBusinessDate);
        Assert.Equal(new DateOnly(2026, 8, 13), preview.LatestBusinessDate);
        Assert.Contains("gross-sales", preview.MetricKeys);
        Assert.Contains("order-count", preview.MetricKeys);
        Assert.Contains("units-sold", preview.MetricKeys);
        Assert.Equal(64, preview.Fingerprint.Length);
    }

    [Fact]
    public async Task Normalize_returns_only_approved_aggregate_observations()
    {
        const string csv = "Business Date,Transaction ID,Net Amount,Currency,Product,Category,Quantity\n"
            + "2026-08-13,T-1,15.25,EUR,Flat White,Drinks,2\n";

        var result = await OperationalCsvReader.NormalizeAsync(Stream(csv), Business, CancellationToken.None);

        Assert.NotEmpty(result.Observations);
        Assert.All(result.Observations, observation =>
            Assert.Contains(observation.MetricKey, OperationalMetricCatalogue.Approved));
        Assert.DoesNotContain(result.Observations, observation =>
            observation.GetType().GetProperties().Any(property =>
                property.Name.Contains("customer", StringComparison.OrdinalIgnoreCase)
                || property.Name.Contains("raw", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task Preview_ignores_customer_pii_columns_without_exposing_values()
    {
        const string csv = "Date,Amount,Currency,Customer Name,Customer Email,Phone,Delivery Address,Order Notes\n"
            + "2026-08-13,9.50,EUR,Example Person,person@example.invalid,+35600000000,Example Street,Example note\n";

        var preview = await OperationalCsvReader.PreviewAsync(Stream(csv), Business, CancellationToken.None);

        Assert.Equal(5, preview.IgnoredSensitiveColumns.Count);
        var serialized = System.Text.Json.JsonSerializer.Serialize(preview);
        Assert.DoesNotContain("Example Person", serialized);
        Assert.DoesNotContain("person@example.invalid", serialized);
        Assert.DoesNotContain("Example Street", serialized);
    }

    [Theory]
    [InlineData("Date,Amount,Currency,PAN\n2026-08-13,9.50,EUR,4111111111111111\n")]
    [InlineData("Date,Amount,Currency,CVV\n2026-08-13,9.50,EUR,123\n")]
    public async Task Preview_rejects_payment_secret_columns(string csv)
    {
        var error = await Assert.ThrowsAsync<OperationalCsvException>(() =>
            OperationalCsvReader.PreviewAsync(Stream(csv), Business, CancellationToken.None));

        Assert.Equal(OperationalCsvErrorCodes.ProhibitedPaymentData, error.Code);
    }

    [Fact]
    public async Task Preview_rejects_mixed_or_business_mismatched_currency()
    {
        const string csv = "Date,Amount,Currency\n2026-08-12,10.00,EUR\n2026-08-13,11.00,USD\n";

        var error = await Assert.ThrowsAsync<OperationalCsvException>(() =>
            OperationalCsvReader.PreviewAsync(Stream(csv), Business, CancellationToken.None));

        Assert.Equal(OperationalCsvErrorCodes.MixedCurrency, error.Code);
    }

    [Fact]
    public async Task Preview_rejects_ambiguous_dates_and_decimals()
    {
        const string csv = "Date,Amount,Currency\n01/02/2026,1,25,EUR\n";

        var error = await Assert.ThrowsAsync<OperationalCsvException>(() =>
            OperationalCsvReader.PreviewAsync(Stream(csv), Business, CancellationToken.None));

        Assert.Equal(OperationalCsvErrorCodes.AmbiguousFormat, error.Code);
    }

    [Fact]
    public async Task Preview_enforces_file_size_and_row_limits()
    {
        await Assert.ThrowsAsync<OperationalCsvException>(() => OperationalCsvReader.PreviewAsync(
            new OversizedReadableStream(OperationalCsvReader.MaximumFileBytes + 1), Business, CancellationToken.None));

        var builder = new StringBuilder("Date,Amount,Currency\n");
        for (var index = 0; index <= OperationalCsvReader.MaximumRows; index++)
            builder.Append("2026-08-13,1.00,EUR\n");

        var error = await Assert.ThrowsAsync<OperationalCsvException>(() =>
            OperationalCsvReader.PreviewAsync(Stream(builder.ToString()), Business, CancellationToken.None));
        Assert.Equal(OperationalCsvErrorCodes.TooManyRows, error.Code);
    }

    private static MemoryStream Stream(string value) => new(Encoding.UTF8.GetBytes(value));

    private sealed class OversizedReadableStream(long length) : System.IO.Stream
    {
        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => length;
        public override long Position { get; set; }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => 0;
        public override long Seek(long offset, SeekOrigin origin) => Position;
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
