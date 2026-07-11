using Bogus;
using FCG.Contracts;
using Microsoft.Extensions.Configuration;
using PaymentsAPI;

namespace PaymentsAPI.UnitTests;

public sealed class PaymentsFixture
{
    public Faker Faker { get; } = new("pt_BR");

    public OrderPlacedEvent CreateOrder() => new(
        Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Faker.Commerce.ProductName(),
        decimal.Parse(Faker.Commerce.Price(10, 300)), DateTime.UtcNow);

    public PaymentProcessor CreateProcessor(bool forceRejected) => new(new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Payments:ForceRejected"] = forceRejected.ToString()
        })
        .Build());
}

public sealed class PaymentProcessorTests(PaymentsFixture fixture) : IClassFixture<PaymentsFixture>
{
    [Theory]
    [InlineData(false, PaymentStatuses.Approved)]
    [InlineData(true, PaymentStatuses.Rejected)]
    public void Process_ReturnsConfiguredStatusAndPreservesOrderData(bool forceRejected, string expectedStatus)
    {
        var order = fixture.CreateOrder();

        var payment = fixture.CreateProcessor(forceRejected).Process(order);

        Assert.Equal(expectedStatus, payment.Status);
        Assert.Equal(order.OrderId, payment.OrderId);
        Assert.Equal(order.UserId, payment.UserId);
        Assert.Equal(order.Price, payment.Price);
    }

    [Fact]
    public void CorrelationId_GeneratesValueWhenHeaderIsMissing()
    {
        var result = CorrelationId.Normalize(null);

        Assert.Equal(32, result.Length);
        Assert.True(Guid.TryParseExact(result, "N", out _));
    }
}
