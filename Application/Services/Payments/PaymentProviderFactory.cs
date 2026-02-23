using Core.Enums;
using Core.Interfaces;

namespace Application.Services.Payments;

public class PaymentProviderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public PaymentProviderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IPaymentProvider GetProvider(EnumPaymentGateway gateway)
    {
        return gateway switch
        {
            EnumPaymentGateway.Paystack => (IPaymentProvider)_serviceProvider.GetService(typeof(PaystackPaymentProvider)),
            EnumPaymentGateway.Flutterwave => (IPaymentProvider)_serviceProvider.GetService(typeof(FlutterwavePaymentProvider)),
            EnumPaymentGateway.Remita => (IPaymentProvider)_serviceProvider.GetService(typeof(RemitaPaymentProvider)),
            _ => (IPaymentProvider)_serviceProvider.GetService(typeof(PaystackPaymentProvider)),
        };
    }
}
