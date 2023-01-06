namespace WaterControlHub.Configs
{
    public sealed class Settings
    {
        public Mqtt Mqtt { get; set; } = new Mqtt();
        public MqttTopics MqttTopics { get; set; } = new MqttTopics();
        public Telegram Telegram { get; set; } = new Telegram();
    }
}