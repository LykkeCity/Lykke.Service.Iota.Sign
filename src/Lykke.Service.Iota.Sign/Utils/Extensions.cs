using Lykke.Common.Api.Contract.Responses;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;

namespace Lykke.Service.Iota.Sign.Utils
{
    public static class Extensions
    {
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
