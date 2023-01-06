namespace WaterControlHub.Configs
{
    public sealed class Mqtt
    {
        public string Host { get; set; } = String.Empty;
        public int Port { get; set; } = 1883;
        public string Username { get; set; } = String.Empty;
        public string Password { get; set; } = String.Empty;
    }
}