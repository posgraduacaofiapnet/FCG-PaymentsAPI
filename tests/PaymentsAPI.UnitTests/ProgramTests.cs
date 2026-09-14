using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace PaymentsAPI.UnitTests;

public sealed class ProgramTests : IClassFixture<PaymentsApiFactory>
{
    private readonly PaymentsApiFactory _factory;

    public ProgramTests(PaymentsApiFactory factory) => _factory = factory;

    [Fact]
    public async Task HealthAndMetricsEndpoints_AreAvailableWithoutRabbitMq()
    {
        using var client = _factory.CreateClient();
        _ = _factory.Services.GetRequiredService<IBus>();

        (await client.GetAsync("/health")).EnsureSuccessStatusCode();
        var metrics = await client.GetAsync("/metrics");
        metrics.EnsureSuccessStatusCode();
        Assert.Contains("# HELP", await metrics.Content.ReadAsStringAsync());
    }
}

public sealed class PaymentsApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services => services.RemoveAll<IHostedService>());
}
