using Common;
using Common.Log;
using Lykke.Service.Iota.Sign.Core.Domain;
using Lykke.Service.Iota.Sign.Services.Helpers;
using Lykke.Service.Iota.Sign.Core.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tangle.Net.Cryptography;
using Tangle.Net.Entity;
using Tangle.Net.Utils;
using Lykke.Service.Iota.Api.Shared;

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
            var address = GetAddress(seed, index);

            return address.ToAddressWithChecksum();
        }

        public async Task<string> SignTransaction(string[] seeds, TransactionContext transactionContext)
        {
            var bundle = new Bundle();
            var virtualInputs = await GetVirtualInputs(seeds, transactionContext.Inputs);

            await AddOutputs(bundle, transactionContext.Outputs);
            await AddInputs(bundle, virtualInputs);

            AddReminder(bundle, virtualInputs, transactionContext.Type);

            bundle.Finalize();
            bundle.Sign();

            var result = new
            {
                Hash = bundle.Hash.Value,
                Transactions = bundle.Transactions.Select(f => f.ToTrytes().Value)
            }.ToJson();

            return result;
        }

        private async Task AddOutputs(Bundle bundle, TransactionOutput[] outputs)
        {
            foreach (var output in outputs)
            {
                var address = await GetOutputAddress(output.Address);

                bundle.AddTransfer((new Transfer
                {
                    Address = address,
                    ValueToTransfer = output.Value,
                    Tag = Tag.Empty,
                    Timestamp = Timestamp.UnixSecondsTimestamp,
                }));
            }
        }

        private async Task AddInputs(Bundle bundle, List<VirtualInput> virtualInputs)
        {
            foreach (var virtualInput in virtualInputs)
            {
                var inputsWithBalance = virtualInput.Inputs.Where(f => f.Balance > 0);
                if (!inputsWithBalance.Any())
                {
                    throw new ArgumentException($"There are no inputs with positive balance for {virtualInput.VirtualAddress} address");
                }

                foreach (var inputWithBalance in inputsWithBalance)
                {
                    var inputAddress = GetAddress(virtualInput.Seed, inputWithBalance.Index);

                    inputAddress.Balance = inputWithBalance.Balance;

                    bundle.AddInput(new Address[] { inputAddress });
                }

                var inputsWithoutBalance = virtualInput.Inputs.Where(f => f.Balance == 0).OrderBy(f => f.Index);
                if (!inputsWithoutBalance.Any())
                {
                    var addressNext = GetAddress(virtualInput.Seed, virtualInput.Inputs.Max(f => f.Index) + 1);

                    virtualInput.NextAddress = addressNext;

                    await SaveAddress(virtualInput.VirtualAddress, addressNext.ToAddressWithChecksum(), addressNext.KeyIndex);

                    break;
                }

                foreach (var inputWithoutBalance in inputsWithoutBalance)
                {
                    var canRecieve = await CanAddressRecieveFunds(inputWithoutBalance.Address);
                    if (canRecieve)
                    {
                        virtualInput.NextAddress = new Address(inputWithoutBalance.Address);

                        break;
                    }
                }

                if (virtualInput.NextAddress == null)
                {
                    throw new ArgumentException($"The {virtualInput.VirtualAddress} has inputs with 0 balance, " +
                        $"but there in no any address that can recieve funds");
                }
            }
        }

        private void AddReminder(Bundle bundle, List<VirtualInput> virtualInputs, TransactionType transactionType)
        {
            if (bundle.Balance > 0)
            {
                throw new ArgumentException($"Input amount is less than Output amount on {-bundle.Balance}");
            }

            if (bundle.Balance < 0)
            {
                bundle.AddRemainder(virtualInputs.First().NextAddress);
            }            
        }

        private async Task<List<VirtualInput>> GetVirtualInputs(string[] seeds, TransactionInput[] txInputs)
        {
            var virtualInputs = new List<VirtualInput>();
            var seedDictionary = new Dictionary<string, string>(
                seeds.Select(f => KeyValuePair.Create(GetVirtualAddress(f), f))
            );

            foreach (var input in txInputs)
            {
                if (!seedDictionary.ContainsKey(input.VirtualAddress))
                {
                    throw new ArgumentException($"The private key for {input.VirtualAddress} address was not provided");
                }

                var virtualAddressInputs = await GetVirtualAddressInputs(input.VirtualAddress);
                if (virtualAddressInputs == null || !virtualAddressInputs.Any())
                {
                    throw new ArgumentException($"There are no inputs for {input.VirtualAddress} address");
                }

                virtualInputs.Add(new VirtualInput
                {
                    Seed = seedDictionary[input.VirtualAddress],
                    VirtualAddress = input.VirtualAddress,
                    Inputs = virtualAddressInputs.ToList()
                });
            }

            return virtualInputs;
        }

        private async Task<Address> GetOutputAddress(string address)
        {
            if (address.StartsWith(Consts.VirtualAddressPrefix))
            {
                var response = await FlurlHelper.GetJsonAsync<UnderlyingAddressResponse>($"{_apiUrl}/api/addresses/{address}/underlying");

                address = response.UnderlyingAddress;
            }

            return new Address(address);
        }

        private Address GetAddress(string seed, int index)
        {
            var addressGenerator = new AddressGenerator();
            var seedObj = new Seed(seed);

            return addressGenerator.GetAddress(seedObj, SecurityLevel.Medium, index);
        }

        private string CalculateHash(string input)
        {
            var salt = new byte[0];
            var bytes = KeyDerivation.Pbkdf2(input, salt, KeyDerivationPrf.HMACSHA512, 10000, 24);

            return string.Concat(bytes.Select(f => f.ToString("X2")));
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

        private async Task<AddressInput[]> GetVirtualAddressInputs(string virtualAddress)
        {
            return await FlurlHelper.GetJsonAsync<AddressInput[]>($"{_apiUrl}/api/internal/virtual-address/{virtualAddress}/inputs");
        }

        private async Task<bool> CanAddressRecieveFunds(string address)
        {
            return await FlurlHelper.GetJsonAsync<bool>($"{_apiUrl}/api/internal/address/{address}/can-recieve");
        }

        private class VirtualInput
        {
            public string Seed { get; set; }

            public string VirtualAddress { get; set; }

            public Address NextAddress { get; set; }

            public List<AddressInput> Inputs { get; set; }
        }

        private class UnderlyingAddressResponse
        {
            public string UnderlyingAddress { get; set; }
        }
    }
}
