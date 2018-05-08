using Common.Log;

namespace Lykke.Service.Iota.Sign.Tests
{
    public class IotaServiceTests
    {
        private ILog _log;

        public IotaServiceTests()
        {
            _log = new LogToMemory();
        }
    }
}
