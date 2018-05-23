using Common;
using Common.Log;
using Lykke.Service.Dash.Sign.Core.Domain;
using Lykke.Service.Dash.Sign.Services.Helpers;
using Lykke.Service.Iota.Sign.Core.Services;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

            return address.Value;
        }

        public async Task<string> SignTransaction(string[] seeds, TransactionContext transactionContext)
        {
            var bundle = new Bundle();
            var seedDictionary = new Dictionary<string, string>(
                seeds.Select(f => KeyValuePair.Create(GetVirtualAddress(f), f))
            );

            var inputs = await GetTxInputs(seedDictionary, transactionContext);
            bundle.AddInput(inputs.Select(f => f.Address));

            foreach (var output in transactionContext.Outputs)
            {
                bundle.AddTransfer((new Transfer
                {
                    Address = new Address(output.RealAddress),
                    ValueToTransfer = output.Value,
                    Tag = Tag.Empty,
                    Timestamp = Timestamp.UnixSecondsTimestamp,
                }));
            }

            var reminder = GetReminder(inputs, transactionContext);
            if (reminder != null)
            {
                bundle.AddRemainder(reminder);
            }

            bundle.Finalize();
            bundle.Sign();

            var result = new
            {
                bundle.Hash.Value,
                transactions = bundle.Transactions.Select(f => f.ToTrytes().Value)
            }.ToJson();

            await CreateNewRealAddresses(inputs, transactionContext);

            return result;
        }

        private async Task<List<InputInfo>> GetTxInputs(Dictionary<string, string> seeds, 
            TransactionContext transactionContext)
        {
            var inputs = new List<InputInfo>();

            foreach (var input in transactionContext.Inputs)
            {
                if (!seeds.ContainsKey(input.VirtualAddress))
                {
                    throw new ArgumentException($"The private key for {input.VirtualAddress} address was not provided");
                }

                var inputSeed = seeds[input.VirtualAddress];
                var inputIndex = await GetVirtualAddressLatestIndex(input.VirtualAddress);
                var inputAddress = GetAddress(inputSeed, inputIndex);

                inputAddress.Balance = input.Value;

                inputs.Add(new InputInfo
                {
                    Seed = inputSeed,
                    VirtualAddress = input.VirtualAddress,
                    Address = inputAddress
                });
            }

            return inputs;
        }

        private Address GetReminder(List<InputInfo> inputs, TransactionContext transactionContext)
        {
            var inputAmount = transactionContext.Inputs.Sum(f => f.Value);
            var outputAmount = transactionContext.Outputs.Sum(f => f.Value);
            var reminderAmount = inputAmount - outputAmount;

            if (reminderAmount > 0)
            {
                if (transactionContext.Inputs.Length > 1)
                {
                    throw new ArgumentException($"We must have only one input when there is positive reminder amount: {reminderAmount}");
                }

                var input = inputs[0];
                var reminder = GetAddress(input.Seed, input.Address.KeyIndex + 1);

                reminder.Balance = reminderAmount;

                return reminder;
            }

            return null;
        }

        private async Task CreateNewRealAddresses(List<InputInfo> inputs, TransactionContext transactionContext)
        {
            foreach (var input in inputs)
            {
                var inputAddress = GetAddress(input.Seed, input.Address.KeyIndex + 1);

                await SaveAddress(input.VirtualAddress, inputAddress.Value, inputAddress.KeyIndex);
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

        private async Task<int> GetVirtualAddressLatestIndex(string virtualAddress)
        {
            return await FlurlHelper.GetJsonAsync<int>($"{_apiUrl}/api/internal/virtual-address/{virtualAddress}/index");
        }

        private Address GetAddress(string seed, int index)
        {
            var addressGenerator = new AddressGenerator();
            var seedObj = new Seed(seed);

            return addressGenerator.GetAddress(seedObj, SecurityLevel.Low, index);
        }

        private string CalculateHash(string input)
        {
            var salt = new byte[0];
            var bytes = KeyDerivation.Pbkdf2(input, salt, KeyDerivationPrf.HMACSHA512, 10000, 24);

            return string.Concat(bytes.Select(f => f.ToString("X2")));
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

        private class InputInfo
        {
            public string Seed { get; set; }
            public string VirtualAddress { get; set; }
            public Address Address { get; set; }
        }
    }
}
