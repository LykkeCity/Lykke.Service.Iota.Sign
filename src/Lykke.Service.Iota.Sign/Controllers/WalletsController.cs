using Lykke.Service.Iota.Sign.Core.Services;
using Lykke.Service.Iota.Sign.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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
        public WalletResponse Post()
        {
            var privateKey = _iotaService.GetPrivateKey();
            var publicAddress = _iotaService.GetPublicAddress(privateKey);

            return new WalletResponse()
            {
                PrivateKey = privateKey,
                PublicAddress = publicAddress
            };
        }
    }
}
