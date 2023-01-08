using WaterControlHub.Configs;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;
using Telegram.Bot;

namespace WaterControlHub.Jobs
{
    public class ListenPumpStateJob : IJob
    {
        private Settings _settings;
        private static IManagedMqttClient _mqttClient = new MqttFactory().CreateManagedMqttClient();
        private TelegramBotClient _telegram;
        private ManagedMqttClientOptions _options;
        public ListenPumpStateJob(Settings settings)
        {
            _settings = settings;
            MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                        .WithClientId("waterhub-state-listener")
                                        .WithTcpServer(_settings.Mqtt.Host, _settings.Mqtt.Port)
                                        .WithCredentials(_settings.Mqtt.Username, _settings.Mqtt.Password);
            _options = new ManagedMqttClientOptionsBuilder()
                                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
                                    .WithClientOptions(builder.Build())
                                    .Build();
            _telegram = new TelegramBotClient(_settings.Telegram.Token);
        }

        public async Task Start()
        {
            _mqttClient.ConnectedAsync += onMqttConnected;
            _mqttClient.DisconnectedAsync += onMqttDisconnected;
            _mqttClient.ApplicationMessageReceivedAsync += onMqttMessageReceived;
            await _mqttClient.StartAsync(_options);
            await _mqttClient.SubscribeAsync(_settings.MqttTopics.State);
        }

        private Task onMqttDisconnected(MqttClientDisconnectedEventArgs arg)
        {
            return Task.CompletedTask;
        }

        private Task onMqttMessageReceived(MqttApplicationMessageReceivedEventArgs msg)
        {
            var payload = msg.ApplicationMessage.ConvertPayloadToString().ToUpper();
            switch (payload)
            {
                case "ON":
                    _telegram.SendTextMessageAsync(_settings.Telegram.ChatId, "PUMP is now ON");
                    break;
                case "OFF":
                    _telegram.SendTextMessageAsync(_settings.Telegram.ChatId, "PUMP is now OFF");
                    break;
                default:
                    Console.WriteLine("Unknown state from MQTT");
                    break;
            }
            return Task.CompletedTask;
        }

        private Task onMqttConnected(MqttClientConnectedEventArgs arg)
        {
            Console.WriteLine("Connected to MQTT - State Job");
            return Task.CompletedTask;
        }
    }
}