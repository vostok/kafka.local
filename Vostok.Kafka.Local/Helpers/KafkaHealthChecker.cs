using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Confluent.Kafka;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Kafka.Local.Helpers
{
    internal class KafkaHealthChecker
    {
        private readonly ILog log;
        private readonly string serverEndpoint;

        public KafkaHealthChecker(ILog log, string serverEndpoint)
        {
            this.log = log.ForContext<KafkaHealthChecker>();
            this.serverEndpoint = serverEndpoint;
        }

        public bool WaitStarted(TimeSpan timeout)
        {
            log.Debug("Waiting for the Kafka to start..");

            var messageTimeout = (timeout.TotalSeconds / 10).Seconds();

            var sw = Stopwatch.StartNew();

            while (sw.Elapsed < timeout)
            {
                if (TryProduceMessage(messageTimeout))
                {
                    log.Debug($"Kafka has successfully started in {sw.Elapsed.TotalSeconds:0.##} second(s).");
                    return true;
                }
            }

            log.Warn($"Kafka hasn't started in {sw.Elapsed.TotalSeconds:0.##} second(s).");
            return false;
        }

        private bool TryProduceMessage(TimeSpan timeout)
        {
            const string topic = "health-check-topic";
            const string message = "health-check-message";

            log.Debug($"Producing message `{message}` to topic `{topic}`...");
            var config = new ProducerConfig
            {
                BootstrapServers = serverEndpoint,
                Acks = Acks.All,
                MessageSendMaxRetries = 1,
                MessageTimeoutMs = (int)timeout.TotalMilliseconds
            };

            try
            {
                using (var p = new ProducerBuilder<Null, string>(config).Build())
                {
                    var result = p.ProduceAsync(topic, new Message<Null, string> {Value = message}).GetAwaiter().GetResult();
                    log.Debug($"Producing message complete with status = {result.Status}.");
                    return result.Status == PersistenceStatus.Persisted;
                }
            }
            catch (Exception error)
            {
                log.Error(error, $"Failed to produce message `{message}` to topic `{topic}`.");
                return false;
            }
        }
    }
}