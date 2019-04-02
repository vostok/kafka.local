namespace Vostok.Kafka.Local
{
    public class KafkaSettings
    {
        public string ZooKeeperConnectionString { get; set; }
        public string BaseDirectory { get; set; }
        public bool DeleteTopicEnable { get; set; } = false;
    }
}