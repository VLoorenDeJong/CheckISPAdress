namespace CheckISPAdress.Interfaces
{
    public interface IISPAddressService
    {
        void ClearCurrentISPAddress();
        void ClearExternalISPAddress();
        void ClearNewISPAddress();
        void ClearOldISPAddress();
        string GetCurrentISPAddress();
        string GetExternalISPAddress();
        string GetNewISPAddress();
        string GetOldISPAddress();
        void SetCurrentISPAddress(string currentISPAddress);
        void SetExternalISPAddress(string externalISPAddress);
        void SetNewISPAddress(string newISPAddress);
        void SetOldISPAddress(string oldISPAddress);
    }
}