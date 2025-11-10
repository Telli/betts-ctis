using System.Diagnostics.Metrics;

namespace BettsTax.Core.Services.Payments;

public static class PaymentMetrics
{
    private static readonly Meter Meter = new("BettsTax.Payments", "1.0.0");
    public static readonly Counter<int> WebhooksProcessed = Meter.CreateCounter<int>("payments_webhooks_processed");
    public static readonly Counter<int> WebhooksDuplicate = Meter.CreateCounter<int>("payments_webhooks_duplicate");
    public static readonly Counter<int> PollingSuccess = Meter.CreateCounter<int>("payments_polling_success");
    public static readonly Counter<int> PollingFailed = Meter.CreateCounter<int>("payments_polling_failed");
}
