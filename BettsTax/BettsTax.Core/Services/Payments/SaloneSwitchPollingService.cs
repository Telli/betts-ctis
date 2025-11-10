using BettsTax.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BettsTax.Shared; // metrics
using System.Threading;
using System.Threading.Tasks;

namespace BettsTax.Core.Services.Payments;

public class SaloneSwitchPollingService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SaloneSwitchPollingService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(2);
    private readonly int _maxAttemptsPerCycle = 3;

    public SaloneSwitchPollingService(IServiceScopeFactory scopeFactory, ILogger<SaloneSwitchPollingService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Salone switch polling cycle failed");
            }
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IPaymentGatewayFactory>();
        var gateway = factory.Get(BettsTax.Data.PaymentProvider.SalonePaymentSwitch);

        var pending = await db.PaymentTransactions
            .Where(t => t.Provider == BettsTax.Data.PaymentProvider.SalonePaymentSwitch && (t.Status == BettsTax.Data.PaymentTransactionStatus.Pending || t.Status == BettsTax.Data.PaymentTransactionStatus.Processing))
            .OrderBy(t => t.CreatedDate)
            .Take(25)
            .ToListAsync(ct);

        if (pending.Count == 0) return;
        _logger.LogInformation("Polling {Count} Salone switch transactions", pending.Count);

        int success = 0, failed = 0;
        foreach (var txn in pending)
        {
            for (var attempt = 1; attempt <= _maxAttemptsPerCycle; attempt++)
            {
                try
                {
                    var status = await gateway.GetStatusAsync(txn.TransactionReference, ct);
                    if (status.IsSuccess && status.Value != null)
                    {
                        txn.Status = status.Value.Status;
                        if (txn.Status == BettsTax.Data.PaymentTransactionStatus.Completed)
                            txn.CompletedDate = DateTime.UtcNow;
                        txn.ProviderResponse = status.Value.StatusMessage;
                        success++;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (attempt == _maxAttemptsPerCycle)
                    {
                        failed++;
                        _logger.LogWarning(ex, "Failed status update for txn {Ref} after {Attempts} attempts", txn.TransactionReference, attempt);
                    }
                    await Task.Delay(TimeSpan.FromSeconds(2 * attempt), ct); // simple backoff
                }
            }
        }

    if (success > 0) PaymentMetrics.PollingSuccess.Add(success);
    if (failed > 0) PaymentMetrics.PollingFailed.Add(failed);
    _logger.LogInformation("Salone switch polling cycle done. Success={Success} Failed={Failed}", success, failed);

        await db.SaveChangesAsync(ct);
    }
}
