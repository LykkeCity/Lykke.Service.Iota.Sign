namespace Lykke.Service.Iota.Sign.Core.Services
{
    public interface IIotaService
    {
        bool IsValidPrivateKey(string privateKey);
        string GetPrivateKey();
        string GetPublicAddress(string privateKey);
        string SignTransaction();
    }
}
