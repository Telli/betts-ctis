using Microsoft.Extensions.Logging;
using Quartz;
using BettsTax.Core.Services;

namespace BettsTax.Core.Jobs;

[DisallowConcurrentExecution]
public class PaymentReconciliationJob : IJob
{
    private readonly ILogger<PaymentReconciliationJob> _logger;
    private readonly IPaymentIntegrationService _paymentService;

    public PaymentReconciliationJob(
        ILogger<PaymentReconciliationJob> logger,
        IPaymentIntegrationService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting payment reconciliation job at {Timestamp}", DateTime.UtcNow);

        try
        {
            // Reconcile pending payments with gateway status
            await _paymentService.ReconcilePendingPaymentsAsync();
            
            // Process failed payment retries
            await _paymentService.ProcessPaymentRetriesAsync();
            
            _logger.LogInformation("Payment reconciliation job completed successfully at {Timestamp}", DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing payment reconciliation job");
            throw;
        }
    }
}
