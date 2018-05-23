using Common.Log;
using Lykke.Service.Dash.Sign.Services.Helpers;
using Lykke.Service.Iota.Sign.Core.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Tangle.Net.Cryptography;
using Tangle.Net.Entity;

namespace Lykke.Service.Iota.Sign.Services
{
    public class IotaService : IIotaService
    {
        private readonly ILog _log;
        private readonly string _apiUrl;

        public IotaService(ILog log, string apiUrl)
        {
            _log = log;
            _apiUrl = apiUrl;
        }

        public bool IsValidSeed(string seed)
        {
            try
            {
                var seedObj = new Seed(seed);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public string GetSeed()
        {
            var seed = Seed.Random();

            return seed.ToString();
        }

        public string GetVirtualAddress(string seed)
        {
            var hash = CalculateHash(seed);

            return $"IOTA{hash}";
        }

        public string GetRealAddress(string seed, int index)
        {
            var addressGenerator = new AddressGenerator();
            var seedObj = new Seed(seed);
            var address = addressGenerator.GetAddress(seedObj, SecurityLevel.High, index);

            return address.Value;
        }

        public string SignTransaction()
        {
            return "";
        }

        public async Task SaveAddress(string virtualAddress, string realAddress, int index)
        {
            var data = new
            {
                realAddress,
                index
            };

            await FlurlHelper.PostJsonAsync($"{_apiUrl}/api/internal/virtual-address/{virtualAddress}", data);
        }

        private string CalculateHash(string input)
        {
            var salt = GenerateSalt(8);
            var bytes = KeyDerivation.Pbkdf2(input, salt, KeyDerivationPrf.HMACSHA512, 10000, 16);

            return $"{string.Concat(salt.Select(f => f.ToString("X2")))}{string.Concat(bytes.Select(f => f.ToString("X2")))}";
        }

        private static byte[] GenerateSalt(int length)
        {
            var salt = new byte[length];

            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            return salt;
        }
    }
}
