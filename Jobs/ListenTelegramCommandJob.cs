using WaterControlHub.Configs;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types;
using Telegram.Bot.Exceptions;
using MQTTnet.Protocol;

namespace WaterControlHub.Jobs
{
    public class ListenTelegramCommandJob : IJob
    {
        private Settings _settings;
        private static IManagedMqttClient _mqttClient = new MqttFactory().CreateManagedMqttClient();
        private TelegramBotClient _telegram;
        private ManagedMqttClientOptions _options;
        public ListenTelegramCommandJob(Settings settings)
        {
            _settings = settings;
            MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                        .WithClientId("waterhub-telegram-listener")
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
            await _mqttClient.StartAsync(_options);
            ReceiverOptions receiverOptions = new()
            {
                AllowedUpdates = Array.Empty<UpdateType>() // receive all update types
            };

            _telegram.StartReceiving(
                updateHandler: HandleUpdateAsync,
                pollingErrorHandler: HandlePollingErrorAsync,
                receiverOptions: receiverOptions,
                cancellationToken: new CancellationToken()
            );
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is not { } message)
                return;
            // Only process text messages
            if (message.Text is not { } messageText)
                return;

            var chatId = message.Chat.Id;
            if (chatId != _settings.Telegram.ChatId) return;

            switch (messageText)
            {
                case "/on":
                    await PublishMqttMessage("ON", _settings.MqttTopics.Control);
                    await SendTelegramMessage("PUMP state sets to ON", chatId);
                    break;
                case "/off":
                    await PublishMqttMessage("OFF", _settings.MqttTopics.Control);
                    await SendTelegramMessage("PUMP state sets to OFF", chatId);
                    break;
                case "/state":
                    await PublishMqttMessage("STATE", _settings.MqttTopics.Control);
                    await SendTelegramMessage("Getting pump state...", chatId);
                    break;
                default: break;
            }
        }

        private async Task PublishMqttMessage(string payload, string topic)
        {
            var msg = new MqttApplicationMessageBuilder().WithPayload(payload).WithTopic(topic).Build();
            await _mqttClient.EnqueueAsync(msg);
        }

        private async Task SendTelegramMessage(string msg, long chatId)
        {
            await _telegram.SendTextMessageAsync(chatId, msg);
        }

        Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            var ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                    => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
                _ => exception.ToString()
            };

            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
        private Task onMqttConnected(MqttClientConnectedEventArgs arg)
        {
            Console.WriteLine("Connected to MQTT - Telegram Job");
            return Task.CompletedTask;
        }
        private Task onMqttDisconnected(MqttClientDisconnectedEventArgs arg)
        {
            return Task.CompletedTask;
        }
    }
}