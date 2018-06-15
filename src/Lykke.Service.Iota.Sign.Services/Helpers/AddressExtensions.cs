using Tangle.Net.Entity;

namespace Lykke.Service.Iota.Sign.Services.Helpers
{
    public static class AddressExtensions
    {
        public static string ToAddressWithChecksum(this Address self)
        {
            return $"{self.Value}{Checksum.FromAddress(self).Value}";
        }
    }
}
