using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BettsTax.Data
{
    public static class PaymentIntegrationSeeder
    {
        public static async Task SeedPaymentProvidersAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Check if payment provider configs already exist
                if (await context.Set<PaymentProviderConfig>().AnyAsync())
                {
                    logger.LogInformation("Payment provider configurations already exist, skipping seeding");
                    return;
                }

                var providers = new List<PaymentProviderConfig>
                {
                    // Orange Money Sierra Leone
                    new PaymentProviderConfig
                    {
                        Provider = PaymentProvider.OrangeMoney,
                        Name = "Orange Money SL",
                        Description = "Orange Money mobile payment service for Sierra Leone",
                        ApiUrl = "https://api.orange.sl/payment/v1",
                        // API credentials should be configured via environment variables in production
                        ApiKey = "ORANGE_API_KEY", 
                        ApiSecret = "ORANGE_API_SECRET",
                        MerchantId = "ORANGE_MERCHANT_ID",
                        WebhookSecret = "ORANGE_WEBHOOK_SECRET",
                        WebhookUrl = "/api/payment-integration/webhook/orange-money",
                        FeePercentage = 2.5m, // 2.5% transaction fee
                        FixedFee = 0m,
                        MinAmount = 100m, // 100 SLE minimum
                        MaxAmount = 50000m, // 50,000 SLE maximum
                        DailyLimit = 500000m, // 500,000 SLE daily limit
                        MonthlyLimit = 10000000m, // 10,000,000 SLE monthly limit
                        IsActive = true,
                        IsTestMode = true,
                        Priority = 1,
                        SupportedCurrency = "SLE"
                    },
                    
                    // Africell Money Sierra Leone
                    new PaymentProviderConfig
                    {
                        Provider = PaymentProvider.AfricellMoney,
                        Name = "Africell Money SL",
                        Description = "Africell Money mobile payment service for Sierra Leone",
                        ApiUrl = "https://api.africell.sl/payment/v1",
                        ApiKey = "AFRICELL_API_KEY",
                        ApiSecret = "AFRICELL_API_SECRET",
                        MerchantId = "AFRICELL_MERCHANT_ID",
                        WebhookSecret = "AFRICELL_WEBHOOK_SECRET",
                        WebhookUrl = "/api/payment-integration/webhook/africell-money",
                        FeePercentage = 2.0m, // 2.0% transaction fee
                        FixedFee = 50m, // 50 SLE fixed fee
                        MinAmount = 100m, // 100 SLE minimum
                        MaxAmount = 75000m, // 75,000 SLE maximum
                        DailyLimit = 750000m, // 750,000 SLE daily limit
                        MonthlyLimit = 15000000m, // 15,000,000 SLE monthly limit
                        IsActive = true,
                        IsTestMode = true,
                        Priority = 2,
                        SupportedCurrency = "SLE"
                    },
                    
                    // Sierra Leone Commercial Bank
                    new PaymentProviderConfig
                    {
                        Provider = PaymentProvider.SierraLeoneCommercialBank,
                        Name = "SLCB Bank Transfer",
                        Description = "Sierra Leone Commercial Bank direct transfer",
                        ApiUrl = "https://api.slcb.sl/banking/v2",
                        ApiKey = "SLCB_API_KEY",
                        ApiSecret = "SLCB_API_SECRET",
                        MerchantId = "SLCB_MERCHANT_ID",
                        FeePercentage = 1.5m, // 1.5% transaction fee
                        FixedFee = 100m, // 100 SLE fixed fee
                        MinAmount = 1000m, // 1,000 SLE minimum
                        MaxAmount = 10000000m, // 10,000,000 SLE maximum
                        IsActive = false, // Disabled until API integration
                        IsTestMode = true,
                        Priority = 5,
                        SupportedCurrency = "SLE"
                    },
                    
                    // PayPal for diaspora
                    new PaymentProviderConfig
                    {
                        Provider = PaymentProvider.PayPal,
                        Name = "PayPal",
                        Description = "PayPal payments for diaspora clients",
                        ApiUrl = "https://api-m.sandbox.paypal.com", // Sandbox for now
                        ApiKey = "PAYPAL_CLIENT_ID",
                        ApiSecret = "PAYPAL_CLIENT_SECRET",
                        WebhookSecret = "PAYPAL_WEBHOOK_SECRET",
                        WebhookUrl = "/api/diaspora-payment/webhook/paypal",
                        FeePercentage = 3.4m, // 3.4% + fixed fee
                        FixedFee = 0.35m, // $0.35 fixed fee (converted to SLE)
                        MinAmount = 5m, // $5 minimum (converted to SLE)
                        MaxAmount = 10000m, // $10,000 maximum (converted to SLE)
                        IsActive = true, // Enable for diaspora payments
                        IsTestMode = true,
                        Priority = 10,
                        SupportedCurrency = "USD"
                    },
                    
                    // Stripe for diaspora
                    new PaymentProviderConfig
                    {
                        Provider = PaymentProvider.Stripe,
                        Name = "Stripe",
                        Description = "Stripe payments for diaspora clients with card processing",
                        ApiUrl = "https://api.stripe.com", // Live API
                        ApiKey = "STRIPE_PUBLISHABLE_KEY",
                        ApiSecret = "STRIPE_SECRET_KEY",
                        WebhookSecret = "STRIPE_WEBHOOK_SECRET",
                        WebhookUrl = "/api/diaspora-payment/webhook/stripe",
                        FeePercentage = 2.9m, // 2.9% + fixed fee
                        FixedFee = 0.30m, // $0.30 fixed fee (converted to SLE)
                        MinAmount = 1m, // $1 minimum (converted to SLE)
                        MaxAmount = 25000m, // $25,000 maximum (converted to SLE)
                        IsActive = true, // Enable for diaspora payments
                        IsTestMode = true,
                        Priority = 11,
                        SupportedCurrency = "USD"
                    },
                    // Salone National Payment Switch (ISO 20022)
                    new PaymentProviderConfig
                    {
                        Provider = PaymentProvider.SalonePaymentSwitch,
                        Name = "Salone Payment Switch",
                        Description = "National payment switch (ISO 20022)",
                        ApiUrl = "https://api.salone-switch.sl/iso20022", // Placeholder
                        ApiKey = "SWITCH_CLIENT_ID",
                        ApiSecret = "SWITCH_CLIENT_SECRET",
                        WebhookSecret = "SWITCH_WEBHOOK_SECRET",
                        WebhookUrl = "/api/payment-integration/webhook/salone-switch",
                        FeePercentage = 1.2m,
                        FixedFee = 25m,
                        MinAmount = 100m,
                        MaxAmount = 1000000m,
                        DailyLimit = 5000000m,
                        MonthlyLimit = 75000000m,
                        IsActive = true,
                        IsTestMode = true,
                        Priority = 3,
                        SupportedCurrency = "SLE"
                    }
                };

                context.Set<PaymentProviderConfig>().AddRange(providers);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} payment provider configurations", providers.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding payment provider configurations");
                throw;
            }
        }

        public static async Task SeedPaymentMethodsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Check if payment method configs already exist
                if (await context.Set<PaymentMethodConfig>().AnyAsync())
                {
                    logger.LogInformation("Payment method configurations already exist, skipping seeding");
                    return;
                }

                var paymentMethods = new List<PaymentMethodConfig>
                {
                    // Orange Money
                    new PaymentMethodConfig
                    {
                        Name = "Orange Money",
                        Description = "Pay with Orange Money mobile wallet",
                        Provider = PaymentProvider.OrangeMoney,
                        IconUrl = "/images/payment-methods/orange-money.png",
                        BrandColor = "#FF6600",
                        CountryCode = "SL",
                        Currency = "SLE",
                        DisplayOrder = 1,
                        IsVisible = true,
                        IsEnabled = true,
                        RequiresPhone = true,
                        RequiresAccount = false,
                        MinAmount = 100m,
                        MaxAmount = 50000m,
                        AvailableForClients = true,
                        AvailableForDiaspora = false
                    },
                    
                    // Africell Money
                    new PaymentMethodConfig
                    {
                        Name = "Africell Money",
                        Description = "Pay with Africell Money mobile wallet",
                        Provider = PaymentProvider.AfricellMoney,
                        IconUrl = "/images/payment-methods/africell-money.png",
                        BrandColor = "#0066CC",
                        CountryCode = "SL",
                        Currency = "SLE",
                        DisplayOrder = 2,
                        IsVisible = true,
                        IsEnabled = true,
                        RequiresPhone = true,
                        RequiresAccount = false,
                        MinAmount = 100m,
                        MaxAmount = 75000m,
                        AvailableForClients = true,
                        AvailableForDiaspora = false
                    },
                    
                    // Bank Transfer (Traditional)
                    new PaymentMethodConfig
                    {
                        Name = "Bank Transfer",
                        Description = "Direct bank transfer to Betts Firm account",
                        Provider = PaymentProvider.BankTransfer,
                        IconUrl = "/images/payment-methods/bank-transfer.png",
                        BrandColor = "#2E7D32",
                        CountryCode = "SL",
                        Currency = "SLE",
                        DisplayOrder = 5,
                        IsVisible = true,
                        IsEnabled = true,
                        RequiresPhone = false,
                        RequiresAccount = true,
                        MinAmount = 1000m,
                        MaxAmount = 10000000m,
                        AvailableForClients = true,
                        AvailableForDiaspora = false
                    },
                    
                    // Cash Payment
                    new PaymentMethodConfig
                    {
                        Name = "Cash Payment",
                        Description = "Pay in cash at Betts Firm office",
                        Provider = PaymentProvider.Cash,
                        IconUrl = "/images/payment-methods/cash.png",
                        BrandColor = "#4CAF50",
                        CountryCode = "SL",
                        Currency = "SLE",
                        DisplayOrder = 6,
                        IsVisible = true,
                        IsEnabled = true,
                        RequiresPhone = false,
                        RequiresAccount = false,
                        MinAmount = 100m,
                        MaxAmount = 1000000m,
                        AvailableForClients = true,
                        AvailableForDiaspora = false
                    },
                    
                    // PayPal for diaspora
                    new PaymentMethodConfig
                    {
                        Name = "PayPal",
                        Description = "Pay securely with your PayPal account (for diaspora clients)",
                        Provider = PaymentProvider.PayPal,
                        IconUrl = "/images/payment-methods/paypal.png",
                        BrandColor = "#0070BA",
                        CountryCode = "GLOBAL",
                        Currency = "USD",
                        DisplayOrder = 10,
                        IsVisible = true,
                        IsEnabled = true,
                        RequiresPhone = false,
                        RequiresAccount = false,
                        MinAmount = 5m, // $5
                        MaxAmount = 10000m, // $10,000
                        AvailableForClients = false,
                        AvailableForDiaspora = true
                    },
                    
                    // Stripe for diaspora
                    new PaymentMethodConfig
                    {
                        Name = "Credit/Debit Card",
                        Description = "Pay with credit or debit card via Stripe (for diaspora clients)",
                        Provider = PaymentProvider.Stripe,
                        IconUrl = "/images/payment-methods/stripe.png",
                        BrandColor = "#635BFF",
                        CountryCode = "GLOBAL",
                        Currency = "USD",
                        DisplayOrder = 11,
                        IsVisible = true,
                        IsEnabled = true,
                        RequiresPhone = false,
                        RequiresAccount = false,
                        MinAmount = 1m, // $1
                        MaxAmount = 25000m, // $25,000
                        AvailableForClients = false,
                        AvailableForDiaspora = true
                    }
                };

                context.Set<PaymentMethodConfig>().AddRange(paymentMethods);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} payment method configurations", paymentMethods.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding payment method configurations");
                throw;
            }
        }

        public static async Task SeedPaymentStatusMappingsAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

            try
            {
                // Check if payment status mappings already exist
                if (await context.Set<PaymentStatusMapping>().AnyAsync())
                {
                    logger.LogInformation("Payment status mappings already exist, skipping seeding");
                    return;
                }

                var statusMappings = new List<PaymentStatusMapping>();

                // Orange Money status mappings
                var orangeStatuses = new[]
                {
                    ("INITIATED", PaymentTransactionStatus.Initiated, false, false, "Payment request sent to customer"),
                    ("PENDING", PaymentTransactionStatus.Pending, false, false, "Waiting for customer confirmation"),
                    ("PROCESSING", PaymentTransactionStatus.Processing, false, false, "Payment being processed"),
                    ("COMPLETED", PaymentTransactionStatus.Completed, true, true, "Payment completed successfully"),
                    ("FAILED", PaymentTransactionStatus.Failed, false, true, "Payment failed"),
                    ("CANCELLED", PaymentTransactionStatus.Cancelled, false, true, "Payment cancelled by customer"),
                    ("EXPIRED", PaymentTransactionStatus.Expired, false, true, "Payment request expired"),
                    ("REFUNDED", PaymentTransactionStatus.Refunded, true, true, "Payment refunded")
                };

                foreach (var (status, mappedStatus, isSuccess, isFinal, description) in orangeStatuses)
                {
                    statusMappings.Add(new PaymentStatusMapping
                    {
                        Provider = PaymentProvider.OrangeMoney,
                        ProviderStatus = status,
                        MappedStatus = mappedStatus,
                        IsSuccess = isSuccess,
                        IsFinal = isFinal,
                        Description = description
                    });
                }

                // Africell Money status mappings
                var africellStatuses = new[]
                {
                    ("INITIATED", PaymentTransactionStatus.Initiated, false, false, "Payment request initiated"),
                    ("PENDING", PaymentTransactionStatus.Pending, false, false, "Payment pending customer action"),
                    ("PROCESSING", PaymentTransactionStatus.Processing, false, false, "Payment in progress"),
                    ("SUCCESS", PaymentTransactionStatus.Completed, true, true, "Payment successful"),
                    ("COMPLETED", PaymentTransactionStatus.Completed, true, true, "Payment completed"),
                    ("FAILED", PaymentTransactionStatus.Failed, false, true, "Payment failed"),
                    ("CANCELLED", PaymentTransactionStatus.Cancelled, false, true, "Payment cancelled"),
                    ("EXPIRED", PaymentTransactionStatus.Expired, false, true, "Payment expired"),
                    ("REFUNDED", PaymentTransactionStatus.Refunded, true, true, "Payment refunded")
                };

                foreach (var (status, mappedStatus, isSuccess, isFinal, description) in africellStatuses)
                {
                    statusMappings.Add(new PaymentStatusMapping
                    {
                        Provider = PaymentProvider.AfricellMoney,
                        ProviderStatus = status,
                        MappedStatus = mappedStatus,
                        IsSuccess = isSuccess,
                        IsFinal = isFinal,
                        Description = description
                    });
                }

                // Salone Payment Switch (pain.002) status mappings (TxSts codes)
                var saloneStatuses = new[]
                {
                    ("PDNG", PaymentTransactionStatus.Pending, false, false, "Pending processing"),
                    ("PENDING", PaymentTransactionStatus.Pending, false, false, "Pending processing"),
                    ("ACSC", PaymentTransactionStatus.Completed, true, true, "Accepted settlement completed"),
                    ("COMPLETED", PaymentTransactionStatus.Completed, true, true, "Payment completed"),
                    ("RJCT", PaymentTransactionStatus.Failed, false, true, "Rejected"),
                    ("FAILED", PaymentTransactionStatus.Failed, false, true, "Failed"),
                };

                foreach (var (status, mappedStatus, isSuccess, isFinal, description) in saloneStatuses)
                {
                    statusMappings.Add(new PaymentStatusMapping
                    {
                        Provider = PaymentProvider.SalonePaymentSwitch,
                        ProviderStatus = status,
                        MappedStatus = mappedStatus,
                        IsSuccess = isSuccess,
                        IsFinal = isFinal,
                        Description = description
                    });
                }

                context.Set<PaymentStatusMapping>().AddRange(statusMappings);
                await context.SaveChangesAsync();

                logger.LogInformation("Seeded {Count} payment status mappings", statusMappings.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding payment status mappings");
                throw;
            }
        }
    }
}