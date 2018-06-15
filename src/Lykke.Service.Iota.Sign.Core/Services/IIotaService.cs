using Lykke.Service.Iota.Sign.Core.Domain;
using System.Threading.Tasks;

namespace Lykke.Service.Iota.Sign.Core.Services
{
    public interface IIotaService
    {
        bool IsValidSeed(string seed);
        string GetSeed();
        string GetVirtualAddress(string seed);
        string GetRealAddress(string seed, int index);
        Task SaveAddress(string virtualAddress, string realAddress, int index);
        Task<string> SignTransaction(string[] seeds, TransactionContext transactionContext);
    }
}
