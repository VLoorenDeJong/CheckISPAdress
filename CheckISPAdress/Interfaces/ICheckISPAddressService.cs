public interface ICheckISPAddressService
{
    Task GetISPAddressAsync();
    Task GetISPAddressFromBackupAPIs(bool heartBeatCheck);
    Task HeartBeatCheck();
}