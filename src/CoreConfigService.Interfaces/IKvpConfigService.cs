using Microsoft.ServiceFabric.Services.Remoting;
using System.Threading.Tasks;

namespace ConfigService.Interfaces
{
    public interface IKvpConfigService : IService
    {
        Task<IKvpConfig> GetConfigSetting(IConfig config);
        Task<IKvpConfig> GetLatestConfigSetting(string name);
    }
}
