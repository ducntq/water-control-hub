using System.Security.Cryptography;
using WaterControlHub.Configs;

namespace WaterControlHub.Jobs {
    public class DailyJob : IJob
    {
        private Settings _settings;
        public DailyJob(Settings settings)
        {
            _settings = settings;
        }
        public async Task Start()
        {
            
        }
    }
}