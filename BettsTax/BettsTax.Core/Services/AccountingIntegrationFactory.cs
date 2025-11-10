using BettsTax.Core.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BettsTax.Core.Services
{
    /// <summary>
    /// Factory service for managing multiple accounting system integrations
    /// </summary>
    public interface IAccountingIntegrationFactory
    {
        /// <summary>
        /// Gets an accounting integration service by provider name
        /// </summary>
        IAccountingIntegrationService GetIntegrationService(string providerName);

        /// <summary>
        /// Gets all available accounting integration providers
        /// </summary>
        List<AccountingProviderInfo> GetAvailableProviders();

        /// <summary>
        /// Checks if a provider is supported
        /// </summary>
        bool IsProviderSupported(string providerName);
    }

    /// <summary>
    /// Implementation of accounting integration factory
    /// </summary>
    public class AccountingIntegrationFactory : IAccountingIntegrationFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AccountingIntegrationFactory> _logger;
        private readonly Dictionary<string, Type> _supportedProviders;

        public AccountingIntegrationFactory(
            IServiceProvider serviceProvider, 
            ILogger<AccountingIntegrationFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Register supported providers
            _supportedProviders = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                [AccountingSystems.QuickBooks] = typeof(QuickBooksIntegrationService),
                [AccountingSystems.Xero] = typeof(XeroIntegrationService)
                // Add more providers as they are implemented
                // [AccountingSystems.Sage] = typeof(SageIntegrationService),
                // [AccountingSystems.Wave] = typeof(WaveIntegrationService),
                // [AccountingSystems.FreshBooks] = typeof(FreshBooksIntegrationService)
            };
        }

        /// <summary>
        /// Gets an accounting integration service by provider name
        /// </summary>
        public IAccountingIntegrationService GetIntegrationService(string providerName)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentException("Provider name cannot be null or empty", nameof(providerName));
            }

            if (!_supportedProviders.TryGetValue(providerName, out var serviceType))
            {
                throw new NotSupportedException($"Accounting provider '{providerName}' is not supported");
            }

            var service = _serviceProvider.GetService(serviceType) as IAccountingIntegrationService;
            
            if (service == null)
            {
                throw new InvalidOperationException($"Failed to resolve service for provider '{providerName}'. " +
                    "Ensure the service is registered in the DI container.");
            }

            _logger.LogDebug("Retrieved integration service for provider: {ProviderName}", providerName);
            return service;
        }

        /// <summary>
        /// Gets all available accounting integration providers
        /// </summary>
        public List<AccountingProviderInfo> GetAvailableProviders()
        {
            var providers = new List<AccountingProviderInfo>();

            foreach (var kvp in _supportedProviders)
            {
                var providerName = kvp.Key;
                var providerInfo = GetProviderInfo(providerName);
                providers.Add(providerInfo);
            }

            return providers.OrderBy(p => p.Name).ToList();
        }

        /// <summary>
        /// Checks if a provider is supported
        /// </summary>
        public bool IsProviderSupported(string providerName)
        {
            return !string.IsNullOrEmpty(providerName) && 
                   _supportedProviders.ContainsKey(providerName);
        }

        /// <summary>
        /// Gets detailed information about a specific provider
        /// </summary>
        private AccountingProviderInfo GetProviderInfo(string providerName)
        {
            return providerName.ToLower() switch
            {
                "quickbooks" => new AccountingProviderInfo
                {
                    Name = AccountingSystems.QuickBooks,
                    DisplayName = "QuickBooks Online",
                    Description = "Connect with QuickBooks Online for seamless accounting integration",
                    LogoUrl = "/images/providers/quickbooks-logo.png",
                    SupportedFeatures = new List<string>
                    {
                        "Payment Synchronization",
                        "Tax Filing Integration", 
                        "Journal Entries",
                        "Chart of Accounts",
                        "Customer Management",
                        "Financial Reporting"
                    },
                    AuthMethod = "OAuth 2.0",
                    IsActive = true,
                    SetupComplexity = ProviderSetupComplexity.Medium,
                    PopularityRank = 1
                },
                "xero" => new AccountingProviderInfo
                {
                    Name = AccountingSystems.Xero,
                    DisplayName = "Xero",
                    Description = "Integrate with Xero for comprehensive financial management",
                    LogoUrl = "/images/providers/xero-logo.png",
                    SupportedFeatures = new List<string>
                    {
                        "Bank Transactions",
                        "Contact Management",
                        "Invoicing Integration",
                        "Financial Reporting",
                        "Multi-currency Support",
                        "Tax Management"
                    },
                    AuthMethod = "OAuth 2.0",
                    IsActive = true,
                    SetupComplexity = ProviderSetupComplexity.Medium,
                    PopularityRank = 2
                },
                "sage" => new AccountingProviderInfo
                {
                    Name = AccountingSystems.Sage,
                    DisplayName = "Sage Business Cloud",
                    Description = "Enterprise-grade integration with Sage accounting solutions",
                    LogoUrl = "/images/providers/sage-logo.png",
                    SupportedFeatures = new List<string>
                    {
                        "Advanced Financial Reporting",
                        "Multi-company Support",
                        "Project Accounting",
                        "Compliance Management"
                    },
                    AuthMethod = "OAuth 2.0",
                    IsActive = false, // Not implemented yet
                    SetupComplexity = ProviderSetupComplexity.High,
                    PopularityRank = 3
                },
                "wave" => new AccountingProviderInfo
                {
                    Name = AccountingSystems.Wave,
                    DisplayName = "Wave Accounting",
                    Description = "Free accounting software integration for small businesses",
                    LogoUrl = "/images/providers/wave-logo.png",
                    SupportedFeatures = new List<string>
                    {
                        "Basic Accounting",
                        "Invoicing",
                        "Payment Processing",
                        "Receipt Scanning"
                    },
                    AuthMethod = "OAuth 2.0",
                    IsActive = false, // Not implemented yet
                    SetupComplexity = ProviderSetupComplexity.Low,
                    PopularityRank = 4
                },
                "freshbooks" => new AccountingProviderInfo
                {
                    Name = AccountingSystems.FreshBooks,
                    DisplayName = "FreshBooks",
                    Description = "Cloud-based accounting for service-based businesses",
                    LogoUrl = "/images/providers/freshbooks-logo.png",
                    SupportedFeatures = new List<string>
                    {
                        "Time Tracking",
                        "Project Management",
                        "Expense Management",
                        "Client Invoicing"
                    },
                    AuthMethod = "OAuth 2.0",
                    IsActive = false, // Not implemented yet
                    SetupComplexity = ProviderSetupComplexity.Medium,
                    PopularityRank = 5
                },
                _ => new AccountingProviderInfo
                {
                    Name = providerName,
                    DisplayName = providerName,
                    Description = $"Integration with {providerName}",
                    LogoUrl = "/images/providers/default-logo.png",
                    SupportedFeatures = new List<string>(),
                    AuthMethod = "OAuth 2.0",
                    IsActive = false,
                    SetupComplexity = ProviderSetupComplexity.Medium,
                    PopularityRank = 99
                }
            };
        }
    }

    /// <summary>
    /// Information about an accounting provider
    /// </summary>
    public class AccountingProviderInfo
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public List<string> SupportedFeatures { get; set; } = new();
        public string AuthMethod { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public ProviderSetupComplexity SetupComplexity { get; set; }
        public int PopularityRank { get; set; }
    }

    /// <summary>
    /// Complexity level for setting up a provider
    /// </summary>
    public enum ProviderSetupComplexity
    {
        Low = 1,
        Medium = 2,
        High = 3
    }
}