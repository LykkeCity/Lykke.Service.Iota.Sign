using Lykke.Service.Iota.Sign.Core.Settings.ServiceSettings;
using Lykke.Service.Iota.Sign.Core.Settings.SlackNotifications;

namespace Lykke.Service.Iota.Sign.Core.Settings
{
    public class AppSettings
    {
        public IotaSignSettings IotaSign { get; set; }
        public SlackNotificationsSettings SlackNotifications { get; set; }
    }
}
