using Common.Log;
using Lykke.Service.Iota.Sign.Core.Services;

namespace Lykke.Service.Iota.Sign.Services
{
    public class IotaService : IIotaService
    {
        private readonly ILog _log;

        public IotaService(ILog log)
        {
            _log = log;
        }

        public bool IsValidPrivateKey(string privateKey)
        {
            return true;
        }

        public string GetPrivateKey()
        {
            return "";
        }

        public string GetPublicAddress(string privateKey)
        {
            return "";
        }

        public string SignTransaction()
        {
            return "";
        }
    }
}
