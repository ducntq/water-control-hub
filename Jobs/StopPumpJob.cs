using Coravel.Invocable;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using Telegram.Bot.Types.Enums;
using WaterControlHub.Configs;

namespace WaterControlHub.Jobs
{
    public class StopPumpJob : IInvocable
    {
        private Settings _settings;
        private IManagedMqttClient _mqttClient = new MqttFactory().CreateManagedMqttClient();
        private ManagedMqttClientOptions _options;
        public StopPumpJob(Settings settings)
        {
            _settings = settings;
            MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                        .WithClientId("waterhub-pump-on")
                                        .WithTcpServer(_settings.Mqtt.Host, _settings.Mqtt.Port)
                                        .WithCredentials(_settings.Mqtt.Username, _settings.Mqtt.Password);
            _options = new ManagedMqttClientOptionsBuilder()
                                    .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
                                    .WithClientOptions(builder.Build())
                                    .Build();
        }
        public async Task Invoke()
        {
            _mqttClient.ConnectedAsync += onMqttConnected;
            _mqttClient.DisconnectedAsync += onMqttDisconnected;
            await _mqttClient.StartAsync(_options);
            var msg = new MqttApplicationMessageBuilder().WithPayload("OFF").WithTopic(_settings.MqttTopics.Control).Build();
            await _mqttClient.EnqueueAsync(msg);
        }

        private Task onMqttConnected(MqttClientConnectedEventArgs arg)
        {
            Console.WriteLine("Connected to MQTT - Telegram Turn on Pump Job");
            return Task.CompletedTask;
        }
        private Task onMqttDisconnected(MqttClientDisconnectedEventArgs arg)
        {
            return Task.CompletedTask;
        }
    }
}