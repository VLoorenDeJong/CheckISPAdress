using CheckISPAdress.Interfaces;

namespace CheckISPAdress.Services
{
    public class ISPAddressService : IISPAddressService
    {
        private string CurrentISPAddress = string.Empty;
        private string NewISPAddress = string.Empty;
        private string OldISPAddress = string.Empty;
        private string ExternalISPAddress = string.Empty;

        public void SetCurrentISPAddress(string currentISPAddress)
        {
            if (!string.IsNullOrWhiteSpace(currentISPAddress))
            {
                CurrentISPAddress = currentISPAddress;
            };
        }
        public string GetCurrentISPAddress()
        {
            return CurrentISPAddress;
        }
        public void ClearCurrentISPAddress()
        {
            CurrentISPAddress = string.Empty;
        }

        public void SetNewISPAddress(string newISPAddress)
        {
            if (!string.IsNullOrWhiteSpace(newISPAddress))
            {
                NewISPAddress = newISPAddress;
            };
        }
        public string GetNewISPAddress()
        {
            return NewISPAddress;
        }
        public void ClearNewISPAddress()
        {
            NewISPAddress = string.Empty;
        }


        public void SetOldISPAddress(string oldISPAddress)
        {
            if (!string.IsNullOrWhiteSpace(oldISPAddress))
            {
                OldISPAddress = oldISPAddress;
            };
        }
        public string GetOldISPAddress()
        {
            return OldISPAddress;
        }
        public void ClearOldISPAddress()
        {
            OldISPAddress = string.Empty;
        }

        public void SetExternalISPAddress(string externalISPAddress)
        {
            if (!string.IsNullOrWhiteSpace(externalISPAddress))
            {
                ExternalISPAddress = externalISPAddress;
            };
        }
        public string GetExternalISPAddress()
        {
            return ExternalISPAddress;
        }
        public void ClearExternalISPAddress()
        {
            ExternalISPAddress = string.Empty;
        }
    }
}
