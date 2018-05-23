using Autofac;
using Common.Log;
using Lykke.Service.Iota.Sign.Core.Services;
using Lykke.Service.Iota.Sign.Core.Settings.ServiceSettings;
using Lykke.Service.Iota.Sign.Services;
using Lykke.SettingsReader;

namespace Lykke.Service.Iota.Sign.Modules
{
    public class ServiceModule : Module
    {
        private readonly IReloadingManager<IotaSignSettings> _settings;
        private readonly ILog _log;

        public ServiceModule(IReloadingManager<IotaSignSettings> settings, ILog log)
        {
            _settings = settings;
            _log = log;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterInstance(_log)
                .As<ILog>()
                .SingleInstance();

            builder.RegisterType<HealthService>()
                .As<IHealthService>()
                .SingleInstance();

            builder.RegisterType<StartupManager>()
                .As<IStartupManager>();

            builder.RegisterType<ShutdownManager>()
                .As<IShutdownManager>();

            builder.RegisterType<IotaService>()
                .As<IIotaService>()
                .WithParameter("apiUrl", _settings.CurrentValue.ApiUrl)
                .SingleInstance();
        }
    }
}
