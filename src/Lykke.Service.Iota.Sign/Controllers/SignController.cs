using Lykke.Service.Iota.Sign.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Service.Iota.Api.Shared;
using Lykke.Service.Iota.Sign.Helpers;
using Lykke.Service.Iota.Sign.Services;

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
        public async Task<IActionResult> SignTransaction([FromBody]SignTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState.ToErrorResponse());
            }

            var transactionContext = JsonConvert.DeserializeObject<TransactionContext>(request.TransactionContext);
            if (transactionContext == null)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(transactionContext)} can not be null"));
            }
            if (transactionContext.Inputs == null || transactionContext.Inputs.Length == 0)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(transactionContext)}{nameof(transactionContext.Inputs)} must have at least one record"));
            }
            if (transactionContext.Outputs == null || transactionContext.Outputs.Length == 0)
            {
                return BadRequest(ErrorResponse.Create($"{nameof(transactionContext)}{nameof(transactionContext.Outputs)} must have at least one record"));
            }

            var hex = await _iotaService.SignTransaction(request.PrivateKeys, transactionContext);

            return Ok(new SignResponse()
            {
                SignedTransaction = hex
            });
        }        
    }
}
