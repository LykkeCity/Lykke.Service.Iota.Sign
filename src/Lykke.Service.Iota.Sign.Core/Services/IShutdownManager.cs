using System.Threading.Tasks;

namespace Lykke.Service.Iota.Sign.Core.Services
{
    public interface IShutdownManager
    {
        Task StopAsync();
    }
}
