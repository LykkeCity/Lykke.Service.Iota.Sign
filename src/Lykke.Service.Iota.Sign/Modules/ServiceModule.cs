using Autofac;
using Lykke.Service.Iota.Sign.Services;
using Lykke.Service.Iota.Sign.Settings;
using Lykke.SettingsReader;

namespace Lykke.Service.Iota.Sign.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<AppSettings> _settings;

        public ServiceModule(IReloadingManager<AppSettings> settings)
        {
            _settings = settings;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<IotaService>()
                .As<IIotaService>()
                .WithParameter("apiUrl", _settings.CurrentValue.IotaSign.ApiUrl)
                .SingleInstance();
        }
    }
}
