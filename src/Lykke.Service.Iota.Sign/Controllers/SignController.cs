using Lykke.Service.Iota.Sign.Core.Services;
using Lykke.Service.Iota.Sign.Models;
using Lykke.Service.Iota.Sign.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Lykke.Service.Iota.Sign.Controllers
{
    [Route("api/sign")]
    public class SignController : Controller
    {
        private readonly IIotaService _iotaService;

        public SignController(IIotaService iotaService)
        {
            _iotaService = iotaService;
        }

        [HttpPost]
        [ProducesResponseType(typeof(SignResponse), (int)HttpStatusCode.OK)]
        public IActionResult SignTransaction([FromBody]SignTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToErrorResponse());
            }

            var hex = _iotaService.SignTransaction();

            return Ok(new SignResponse()
            {
                SignedTransaction = hex
            });
        }        
    }
}
