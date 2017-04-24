using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace ConfigService.Interfaces
{
    public interface IKvpConfigService : IService
    {
        Task<KvpConfig> GetConfigSetting(ConfigKey config);
        Task<KvpConfig> GetLatestConfigSetting(string name);
        Task AddConfigSetting(KvpConfig config);
        Task DeleteConfigSetting(ConfigKey config);
    }
}
