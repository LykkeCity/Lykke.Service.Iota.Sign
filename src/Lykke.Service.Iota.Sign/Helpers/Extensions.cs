using Lykke.Common.Api.Contract.Responses;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using Tangle.Net.Entity;

namespace Lykke.Service.Iota.Sign.Helpers
{
    public static class Extensions
    {
        public static string ValueWithChecksum(this Address self)
        {
            return $"{self.Value}{Checksum.FromAddress(self).Value}";
        }

        public static ErrorResponse ToErrorResponse(this ModelStateDictionary modelState)
        {
            var response = new ErrorResponse();

            foreach (var state in modelState)
            {
                var messages = state.Value.Errors
                    .Where(e => !string.IsNullOrWhiteSpace(e.ErrorMessage))
                    .Select(e => e.ErrorMessage)
                    .Concat(state.Value.Errors
                        .Where(e => string.IsNullOrWhiteSpace(e.ErrorMessage))
                        .Select(e => e.Exception.Message))
                    .ToList();

                response.ModelErrors.Add(state.Key, messages);
            }

            return response;
        }
    }
}
