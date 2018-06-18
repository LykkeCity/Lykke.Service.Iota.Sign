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
            var inputs = await GetTxInputs(seeds, transactionContext);

            AddOutputs(bundle, transactionContext.Outputs);
            AddInputs(bundle, inputs);
            AddReminder(bundle, inputs, transactionContext.Type);

            bundle.Finalize();
            bundle.Sign();

            var result = new
            {
                Hash = bundle.Hash.Value,
                Transactions = bundle.Transactions.Select(f => f.ToTrytes().Value)
            }.ToJson();

            await CreateNewRealAddresses(inputs, transactionContext);

            return result;
        }

        private void AddOutputs(Bundle bundle, TransactionOutput[] outputs)
        {
            foreach (var output in outputs)
            {
                bundle.AddTransfer((new Transfer
                {
                    Address = GetOutputAddress(output.Address),
                    ValueToTransfer = output.Value,
                    Tag = Tag.Empty,
                    Timestamp = Timestamp.UnixSecondsTimestamp,
                }));
            }
        }

        private void AddInputs(Bundle bundle, List<VirtualAddressInfo> inputs)
        {
            foreach (var input in inputs)
            {
                var inputsWithBalance = input.VirtualAddressInputs.Where(f => f.Balance > 0);
                if (!inputsWithBalance.Any())
                {
                    throw new ArgumentException($"There are no inputs with positive balance for {input.Address} address");
                }

                foreach (var inputWithBalance in inputsWithBalance)
                {
                    var inputAddress = GetAddress(input.Seed, inputWithBalance.Index);

                    inputAddress.Balance = inputWithBalance.Balance;

                    bundle.AddInput(new Address[] { inputAddress });
                }                
            }
        }

        private void AddReminder(Bundle bundle, List<VirtualAddressInfo> inputs, TransactionType transactionType)
        {
            if (bundle.Balance == 0)
            {
                return;
            }

            if (bundle.Balance < 0)
            {
                throw new ArgumentException($"Input amount is less than Output amount on {bundle.Balance}");
            }
            if (transactionType == TransactionType.Cashin && bundle.Balance > 0)
            {
                throw new ArgumentException($"Input amount must equal Output amount for cash-in operation");
            }
            if (transactionType == TransactionType.Cashout && inputs.Count > 1)
            {
                throw new ArgumentException($"Only one input is allowed with positive reminder amount {bundle.Balance}");
            }

            var input = inputs[0];

            var reminder = input.FirstNotLockedInput == null ?
                GetAddress(input.Seed, input.VirtualAddressInputs.Max(f => f.Index) + 1) :
                new Address(input.FirstNotLockedInput.Address);

            reminder.Balance = bundle.Balance;

            bundle.AddRemainder(reminder);
        }

        private async Task<List<VirtualAddressInfo>> GetTxInputs(string[] seeds, TransactionContext transactionContext)
        {
            var inputs = new List<VirtualAddressInfo>();
            var seedDictionary = new Dictionary<string, string>(
                seeds.Select(f => KeyValuePair.Create(GetVirtualAddress(f), f))
            );

            foreach (var input in transactionContext.Inputs)
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

                var inputInfo = new VirtualAddressInfo
                {
                    Seed = seedDictionary[input.VirtualAddress],
                    Address = input.VirtualAddress,
                    VirtualAddressInputs = virtualAddressInputs.ToList()
                };

                inputs.Add(inputInfo);
            }

            return inputs;
        }

        private async Task CreateNewRealAddresses(List<VirtualAddressInfo> inputs, TransactionContext transactionContext)
        {
            foreach (var input in inputs)
            {
                if (input.FirstNotLockedInput == null)
                {
                    var inputAddress = GetAddress(input.Seed, input.VirtualAddressInputs.Max(f => f.Index) + 1);

                    await SaveAddress(input.Address, inputAddress.ToAddressWithChecksum(), inputAddress.KeyIndex);
                }                
            }
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

        private Address GetOutputAddress(string address)
        {
            if (address.StartsWith(Consts.VirtualAddressPrefix))
            {
                address = FlurlHelper.GetStringAsync($"{_apiUrl}/api/internal/virtual-address/{address}/real").Result;
            }

            var canRecieve = FlurlHelper.GetJsonAsync<bool>($"{_apiUrl}/api/internal/address/{address}/can-spent").Result;
            if (!canRecieve)
            {
                throw new ArgumentException($"The {address} address can not recieve iota. Private key reuse detected.");
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

        private class VirtualAddressInfo
        {
            public string Seed { get; set; }

            public string Address { get; set; }

            public List<AddressInput> VirtualAddressInputs { get; set; }

            public AddressInput FirstNotLockedInput
            {
                get
                {
                    var inputsWithoutBalance = VirtualAddressInputs.Where(f => f.Balance == 0);

                    return inputsWithoutBalance.Any() ? inputsWithoutBalance.OrderBy(f => f.Index).First() : null;
                }
            }
        }
    }
}
