using Microsoft.Extensions.Configuration;
using WaterControlHub.Configs;
using WaterControlHub.Jobs;

namespace WaterControlHub
{
    class Program
    {
        private static readonly TaskCompletionSource<object> TaskCompletionSrc = new TaskCompletionSource<object>();
        private static Settings? _settings = new Settings();
        static async Task<int> Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables()
                .Build();
            _settings = config.Get<Settings>();
            if (_settings == null) return 1;
            var threadState = new Thread(StartStateJob);
            var threadCommand = new Thread(StartCommandJob);
            threadCommand.Start();
            threadState.Start();
            await TaskCompletionSrc.Task;

            Console.WriteLine("Task completed. Exiting...");
            return 0;
        }

        private static void StartStateJob()
        {
            if (_settings == null) return;
            ListenPumpStateJob jobListen = new ListenPumpStateJob(_settings);
            Task.Run(jobListen.Start);
        }

        private static void StartCommandJob()
        {
            if (_settings == null) return;
            ListenTelegramCommandJob jobCommand = new ListenTelegramCommandJob(_settings);
            Task.Run(jobCommand.Start);
        }
    }
}