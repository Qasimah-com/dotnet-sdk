using System.Text.Json;
using Qasimha.Sdk.Json;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class EnumNormalisationTests
{
    [Theory]
    [InlineData(0, OrderStatus.Draft)]
    [InlineData(1, OrderStatus.New)]
    [InlineData(2, OrderStatus.Approved)]
    [InlineData(3, OrderStatus.Declined)]
    [InlineData(4, OrderStatus.Expired)]
    [InlineData(5, OrderStatus.Confirmed)]
    [InlineData(6, OrderStatus.FullyFulfilled)]
    [InlineData(7, OrderStatus.PartiallyFulfilled)]
    [InlineData(8, OrderStatus.FullyReversed)]
    [InlineData(9, OrderStatus.PartiallyReversed)]
    [InlineData(10, OrderStatus.Voided)]
    [InlineData(11, OrderStatus.Updated)]
    [InlineData(12, OrderStatus.AwaitingCheckout)]
    [InlineData(13, OrderStatus.Cancelled)]
    [InlineData(14, OrderStatus.Settled)]
    [InlineData(15, OrderStatus.WrittenOff)]
    public void Maps_every_order_status_code(int code, string expected)
    {
        Assert.Equal(expected, OrderStatus.FromCode(code));
    }

    [Theory]
    [InlineData(0, OrderChannel.Online)]
    [InlineData(1, OrderChannel.InStore)]
    [InlineData(2, OrderChannel.QrCode)]
    public void Maps_every_channel_code(int code, string expected)
    {
        Assert.Equal(expected, OrderChannel.FromCode(code));
    }

    [Theory]
    [InlineData(0, PaymentType.PayByInstalments)]
    [InlineData(1, PaymentType.PayNow)]
    [InlineData(2, PaymentType.PayNextMonth)]
    [InlineData(3, PaymentType.PayInFull)]
    public void Maps_every_payment_type_code(int code, string expected)
    {
        Assert.Equal(expected, PaymentType.FromCode(code));
    }

    [Theory]
    [InlineData(0, InstallmentItemStatus.Scheduled)]
    [InlineData(1, InstallmentItemStatus.Paid)]
    [InlineData(2, InstallmentItemStatus.Late)]
    [InlineData(3, InstallmentItemStatus.Reversed)]
    [InlineData(4, InstallmentItemStatus.Waived)]
    public void Maps_every_instalment_status_code(int code, string expected)
    {
        Assert.Equal(expected, InstallmentItemStatus.FromCode(code));
    }

    [Theory]
    [InlineData(16)]
    [InlineData(99)]
    [InlineData(-1)]
    public void Passes_an_unknown_code_through_as_its_string(int code)
    {
        Assert.Equal(code.ToString(System.Globalization.CultureInfo.InvariantCulture), OrderStatus.FromCode(code));
    }

    [Theory]
    [InlineData("Declined", OrderStatus.Declined)]
    [InlineData("declined", OrderStatus.Declined)]
    [InlineData("AWAITINGCHECKOUT", OrderStatus.AwaitingCheckout)]
    [InlineData("3", OrderStatus.Declined)]
    [InlineData("Frozen", "Frozen")]
    public void Normalises_names_and_numeric_strings(string value, string expected)
    {
        Assert.Equal(expected, OrderStatus.Normalize(value));
    }

    [Fact]
    public void Deserialises_integers_strings_booleans_and_nulls()
    {
        var json = """
            {"items":[
              {"status":5,"paymentType":"pay_now","channel":"2"},
              {"status":"Confirmed","paymentType":3,"channel":null},
              {"status":true,"paymentType":1.5,"channel":"99"}
            ]}
            """;

        var order = JsonSerializer.Deserialize<Probe>(json, JsonDefaults.Options)!;

        Assert.Equal(OrderStatus.Confirmed, order.Items[0].Status);
        Assert.Equal("pay_now", order.Items[0].PaymentType);
        Assert.Equal(OrderChannel.QrCode, order.Items[0].Channel);
        Assert.Equal(OrderStatus.Confirmed, order.Items[1].Status);
        Assert.Equal(PaymentType.PayInFull, order.Items[1].PaymentType);
        Assert.Null(order.Items[1].Channel);
        Assert.Equal("true", order.Items[2].Status);
        Assert.Equal("1.5", order.Items[2].PaymentType);
        Assert.Equal("99", order.Items[2].Channel);
    }

    [Fact]
    public void Rejects_an_object_where_an_enum_is_expected()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Probe>("""{"items":[{"status":{"x":1}}]}""", JsonDefaults.Options));
    }

    [Fact]
    public void Writes_the_name_back_as_a_string()
    {
        var json = JsonSerializer.Serialize(new Probe { Items = new[] { new ProbeRow { Status = OrderStatus.Confirmed } } }, JsonDefaults.Options);

        Assert.Equal("""{"items":[{"status":"Confirmed"}]}""", json);
    }

    [Fact]
    public void Lenient_strings_accept_numbers_booleans_and_objects()
    {
        var row = JsonSerializer.Deserialize<LenientProbe>("""{"a":7,"b":true,"c":{"k":1},"d":"text"}""", JsonDefaults.Options)!;

        Assert.Equal("7", row.A);
        Assert.Equal("true", row.B);
        Assert.Equal("""{"k":1}""", row.C);
        Assert.Equal("text", row.D);
    }

    [Fact]
    public void Public_helpers_reject_null()
    {
        Assert.Throws<ArgumentNullException>(() => OrderStatus.Normalize(null!));
        Assert.Throws<ArgumentNullException>(() => OrderChannel.Normalize(null!));
        Assert.Throws<ArgumentNullException>(() => PaymentType.Normalize(null!));
        Assert.Throws<ArgumentNullException>(() => InstallmentItemStatus.Normalize(null!));
    }

    private sealed class Probe
    {
        public ProbeRow[] Items { get; init; } = Array.Empty<ProbeRow>();
    }

    private sealed class ProbeRow
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(OrderStatusConverter))]
        public string? Status { get; init; }

        [System.Text.Json.Serialization.JsonConverter(typeof(PaymentTypeConverter))]
        public string? PaymentType { get; init; }

        [System.Text.Json.Serialization.JsonConverter(typeof(OrderChannelConverter))]
        public string? Channel { get; init; }
    }

    private sealed class LenientProbe
    {
        [System.Text.Json.Serialization.JsonConverter(typeof(LenientStringConverter))]
        public string? A { get; init; }

        [System.Text.Json.Serialization.JsonConverter(typeof(LenientStringConverter))]
        public string? B { get; init; }

        [System.Text.Json.Serialization.JsonConverter(typeof(LenientStringConverter))]
        public string? C { get; init; }

        [System.Text.Json.Serialization.JsonConverter(typeof(LenientStringConverter))]
        public string? D { get; init; }
    }
}
