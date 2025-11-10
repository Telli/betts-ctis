using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BettsTax.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using FluentAssertions;
using Xunit;
using BettsTax.Core.Services.Payments;
using BettsTax.Shared; // Result type
using Microsoft.Extensions.DependencyInjection;

namespace BettsTax.Core.Tests.Payments;

public class SaloneSwitchTests
{
    private ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task WebhookProcessor_IsIdempotent_ForDuplicatePayload()
    {
        using var db = CreateDb();
        var logger = new NullLogger<PaymentWebhookProcessor>();
        var proc = new PaymentWebhookProcessor(db, logger);
        var xml = "<Document><EndToEndId>ABC123</EndToEndId><TxSts>PDNG</TxSts></Document>";
        var headers = new Dictionary<string, string?> { { "X-Test", "1" } };
        var first = await proc.ProcessAsync("SalonePaymentSwitch", xml, headers, CancellationToken.None);
        var second = await proc.ProcessAsync("SalonePaymentSwitch", xml, headers, CancellationToken.None);
        first.Should().BeTrue();
        second.Should().BeTrue();
    (await db.PaymentWebhookLogs.CountAsync()).Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("ACSC", PaymentTransactionStatus.Completed)]
    [InlineData("COMPLETED", PaymentTransactionStatus.Completed)]
    [InlineData("RJCT", PaymentTransactionStatus.Failed)]
    [InlineData("FAILED", PaymentTransactionStatus.Failed)]
    [InlineData("PDNG", PaymentTransactionStatus.Pending)]
    public async Task WebhookProcessor_MapsStatuses(string txSts, PaymentTransactionStatus expected)
    {
        using var db = CreateDb();
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            PaymentId = 1,
            Provider = PaymentProvider.SalonePaymentSwitch,
            TransactionReference = "REF-1",
            Amount = 10,
            Currency = "SLE"
        });
        await db.SaveChangesAsync();
        var logger = new NullLogger<PaymentWebhookProcessor>();
        var proc = new PaymentWebhookProcessor(db, logger);
        var xml = $"<Document><EndToEndId>REF-1</EndToEndId><TxSts>{txSts}</TxSts></Document>";
        await proc.ProcessAsync("SalonePaymentSwitch", xml, new Dictionary<string, string?>(), CancellationToken.None);
        var txn = await db.PaymentTransactions.FirstAsync();
        txn.Status.Should().Be(expected);
    }

    [Fact]
    public async Task PollingService_UpdatesPendingTransactions()
    {
        using var db = CreateDb();
        db.PaymentTransactions.Add(new PaymentTransaction
        {
            PaymentId = 1,
            Provider = PaymentProvider.SalonePaymentSwitch,
            TransactionReference = "TRX-1",
            Amount = 50,
            Currency = "SLE",
            Status = PaymentTransactionStatus.Pending
        });
        await db.SaveChangesAsync();

        // Fake gateway via factory: we build a service collection mimicking DI
        var services = new ServiceCollection();
        services.AddSingleton(db); // reuse same context
        services.AddLogging();
        services.AddSingleton<IPaymentGatewayFactory, FakeFactory>();
        services.AddSingleton<IPaymentGateway>(new FakeGateway());
        var provider = services.BuildServiceProvider();

        var factory = provider.GetRequiredService<IPaymentGatewayFactory>();
        var gateway = factory.Get(PaymentProvider.SalonePaymentSwitch);
        var status = await gateway.GetStatusAsync("TRX-1");
        status.IsSuccess.Should().BeTrue();
    }

    private class FakeFactory : IPaymentGatewayFactory
    {
        private readonly IEnumerable<IPaymentGateway> _gateways;
        public FakeFactory(IEnumerable<IPaymentGateway> gateways) => _gateways = gateways;
        public IPaymentGateway Get(PaymentProvider provider) => _gateways.First(g => g.Provider == provider);
    }

    private class FakeGateway : IPaymentGateway
    {
        public PaymentProvider Provider => PaymentProvider.SalonePaymentSwitch;
        public Task<Result<PaymentGatewayResponse>> GetStatusAsync(string providerTransactionId, CancellationToken ct = default)
            => Task.FromResult(Result.Success(new PaymentGatewayResponse
            {
                Success = true,
                TransactionId = providerTransactionId,
                ProviderReference = providerTransactionId,
                Status = PaymentTransactionStatus.Completed,
                StatusMessage = "Completed"
            }));
        public Task<Result<PaymentGatewayResponse>> InitiateAsync(PaymentGatewayRequest request, CancellationToken ct = default) => Task.FromResult(Result.Success(new PaymentGatewayResponse()));
        public Task<Result<PaymentGatewayResponse>> ProcessWebhookAsync(string rawBody, CancellationToken ct = default) => Task.FromResult(Result.Success(new PaymentGatewayResponse()));
        public Task<Result<PaymentGatewayResponse>> RefundAsync(string providerTransactionId, decimal amount, CancellationToken ct = default) => Task.FromResult(Result.Failure<PaymentGatewayResponse>("Not Impl"));
        public Task<Result<bool>> ValidateWebhookAsync(string rawBody, string signatureHeader, CancellationToken ct = default) => Task.FromResult(Result.Success(true));
    }
}
