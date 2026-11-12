using System.Text.Json;
using Qasimha.Sdk.Tests.Support;
using Xunit;

namespace Qasimha.Sdk.Tests;

public class SettlementsTests
{
    [Fact]
    public async Task Lists_settlements()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new
        {
            data = new[]
            {
                new { id = "s-1", providerSettlementId = "ps-1", amount = 950.25, currencyCode = "SAR", settledAt = "2026-09-20T00:00:00Z", status = "Paid", orderCount = 4 },
            },
            pagination = new { totalItems = 1, page = 1, pageSize = 20, totalPages = 1 },
        }));

        var page = await harness.Client.Settlements.ListAsync(new ListSettlementsRequest { Page = 1, PageSize = 20 });

        Assert.Equal("/api/v1/merchant/settlements", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal("?page=1&pageSize=20", harness.LastRequest.Uri.Query);
        var row = Assert.Single(page.Items);
        Assert.Equal("s-1", row.Id);
        Assert.Equal("ps-1", row.ProviderSettlementId);
        Assert.Equal(950.25m, row.Amount);
        Assert.Equal("Paid", row.Status);
        Assert.Equal(4, row.OrderCount);
        Assert.Equal(new DateTimeOffset(2026, 9, 20, 0, 0, 0, TimeSpan.Zero), row.SettledAt);
        Assert.Equal(1, page.Pagination.TotalPages);
    }

    [Fact]
    public async Task Reads_the_overview()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(200, Fixtures.Envelope("SUCCESS", new
        {
            totalSettled = 12000.50,
            count = 3,
            lastSettlement = new { id = "s-3", amount = 4000.00 },
            settlementCycle = "Weekly",
            settlementModel = 1,
            minimumSettlementAmount = 500,
            remainingForNextSettlement = 120.75,
            notPaidYet = 999.99,
        }));

        var overview = await harness.Client.Settlements.OverviewAsync();

        Assert.Equal("/api/v1/merchant/settlements/overview", harness.LastRequest.Uri.AbsolutePath);
        Assert.Equal(12000.50m, overview.TotalSettled);
        Assert.Equal(3, overview.Count);
        Assert.Equal("Weekly", overview.SettlementCycle);
        Assert.Equal("1", overview.SettlementModel);
        Assert.Equal(500m, overview.MinimumSettlementAmount);
        Assert.Equal(120.75m, overview.RemainingForNextSettlement);
        Assert.Equal("s-3", overview.LastSettlement!.Value.GetProperty("id").GetString());
        Assert.Equal(999.99m, overview.Raw.GetProperty("notPaidYet").GetDecimal());
        Assert.Equal(JsonValueKind.Object, overview.Raw.ValueKind);
    }

    [Fact]
    public async Task A_key_without_the_scope_is_refused()
    {
        using var harness = new Harness();
        harness.Handler.Enqueue(403);

        await Assert.ThrowsAsync<PermissionException>(() => harness.Client.Settlements.ListAsync());
        await Assert.ThrowsAsync<PermissionException>(() => harness.Client.Settlements.OverviewAsync());
    }
}
