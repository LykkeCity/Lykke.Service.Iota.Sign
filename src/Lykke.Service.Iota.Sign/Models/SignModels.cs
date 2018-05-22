using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Lykke.Service.Iota.Sign.Core.Services;

namespace Lykke.Service.Iota.Sign.Models
{
    [DataContract]
    public class SignTransactionRequest : IValidatableObject
    {
        [DataMember]
        [Required]
        public string[] PrivateKeys { get; set; }

        [DataMember]
        [Required]
        public string TransactionContext { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var result = new List<ValidationResult>();
            var service = (IIotaService)validationContext.GetService(typeof(IIotaService));

            if (PrivateKeys == null || !PrivateKeys.Any())
            {
                result.Add(new ValidationResult(
                    $"{nameof(PrivateKeys)} array can not be empty",
                    new[] { nameof(PrivateKeys) }));
            }

            var num = 0;
            foreach (var key in PrivateKeys)
            {
                if (!service.IsValidSeed(key))
                {
                    result.Add(new ValidationResult(
                        $"{nameof(PrivateKeys)}.[{num}] is not a valid", 
                        new[] { nameof(PrivateKeys) }));
                }

                num++;
            }

            return result;
        }
    }

    public class SignResponse
    {
        public string SignedTransaction { get; set; }
    }
}
