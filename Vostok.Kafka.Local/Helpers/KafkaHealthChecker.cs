using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Confluent.Kafka;
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

            var sw = Stopwatch.StartNew();
            if (TryProduceMessage(timeout))
            {
                log.Debug($"Kafka has successfully started in {sw.Elapsed.TotalSeconds:0.##} second(s).");
                return true;
            }
            
            log.Warn($"Kafka hasn't started in {timeout}.");
            return false;
        }

        private bool TryProduceMessage(TimeSpan timeout)
        {
            const string topic = "health-check-topic";
            const string message = "health-check-message";

            try
            {
                return ProduceMessage(topic, message, timeout).GetAwaiter().GetResult();
            }
            catch (Exception error)
            {
                log.Error(error, $"Failed to produce message `{message}` to topic `{topic}`.");
                return false;
            }
        }

        private async Task<bool> ProduceMessage(string topic, string message, TimeSpan timeout)
        {
            log.Debug($"Producing message `{message}` to topic `{topic}`...");
            var config = new ProducerConfig
            {
                BootstrapServers = serverEndpoint,
                Acks = Acks.All,
                MessageSendMaxRetries = 10000000,
                RetryBackoffMs = 100,
                MessageTimeoutMs = (int)timeout.TotalMilliseconds
            };

            using (var p = new ProducerBuilder<Null, string>(config).Build())
            {
                var result = await p.ProduceAsync(topic, new Message<Null, string> {Value = message}).ConfigureAwait(false);
                log.Debug($"Producing message complete with status = {result.Status}.");
                return result.Status == PersistenceStatus.Persisted;
            }
        }
    }
}