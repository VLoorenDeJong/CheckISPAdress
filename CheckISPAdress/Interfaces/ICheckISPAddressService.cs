using CheckISPAdress.Services;

public interface ICheckISPAddressService
{
    Task StartISPCheckTimers(CancellationToken cancellationToken);
}