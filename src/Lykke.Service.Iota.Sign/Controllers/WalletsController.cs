using Lykke.Service.Iota.Sign.Core.Services;
using Lykke.Service.Iota.Sign.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;

namespace Lykke.Service.Iota.Sign.Controllers
{
    [Route("api/wallets")]
    public class WalletsController : Controller
    {
        private readonly IIotaService _iotaService;

        public WalletsController(IIotaService iotaService)
        {
            _iotaService = iotaService;
        }

        [HttpPost]
        public async Task<WalletResponse> Post()
        {
            var index = 0;
            var seed = _iotaService.GetSeed();
            var virtualAddress = _iotaService.GetVirtualAddress(seed);
            var realAddress = _iotaService.GetRealAddress(seed, index);

            await _iotaService.SaveAddress(virtualAddress, realAddress, index);

            return new WalletResponse()
            {
                PrivateKey = seed,
                PublicAddress = virtualAddress
            };
        }
    }
}
