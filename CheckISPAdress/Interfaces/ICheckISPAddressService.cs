using CheckISPAdress.Services;

public interface ICheckISPAddressService
{
    Task CheckISPAddressAsync(CancellationToken cancellationToken);
}