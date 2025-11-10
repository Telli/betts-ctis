using Microsoft.Extensions.DependencyInjection;
using BettsTax.Data;

namespace BettsTax.Core.Services.Payments;

public interface IPaymentGatewayFactory
{
    IPaymentGateway Get(PaymentProvider provider);
}

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IEnumerable<IPaymentGateway> _gateways;

    public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways)
    {
        _gateways = gateways;
    }

    public IPaymentGateway Get(PaymentProvider provider)
    {
        var gateway = _gateways.FirstOrDefault(g => g.Provider == provider);
        if (gateway == null)
            throw new NotSupportedException($"Gateway not registered for provider {provider}");
        return gateway;
    }
}