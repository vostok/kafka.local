using System;
using System.IO;
using Vostok.Commons.Time;
using Vostok.Commons.Local;
using Vostok.Commons.Local.Helpers;
using Vostok.Kafka.Local.Helpers;
using Vostok.Logging.Abstractions;

// ReSharper disable InconsistentNaming

namespace Vostok.Kafka.Local
{
    public class KafkaInstance : IDisposable
    {
        private readonly KafkaHealthChecker healthChecker;
        private readonly ShellRunner shellRunner;

        internal KafkaInstance(string baseDirectory, int port, ILog log)
        {
            log = log.ForContext("KafkaLocal");

            Port = port;
            BaseDirectory = baseDirectory;
            healthChecker = new KafkaHealthChecker(log, $"localhost:{port}");
            shellRunner = new ShellRunner(
                new ShellRunnerSettings("java")
                {
                    Arguments = BuildKafkaArguments(),
                    WorkingDirectory = BaseDirectory
                },
                log);
        }

        public static KafkaInstance DeployNew(string zooKeeperConnectionString, ILog log, bool started = true)
        {
            return DeployNew(new KafkaSettings {ZooKeeperConnectionString = zooKeeperConnectionString}, log, started);
        }

        public static KafkaInstance DeployNew(KafkaSettings settings, ILog log, bool started = true)
        {
            KafkaInstance kafkaInstance = null;
            
            Retrier.RetryOnException(() =>
            {
                kafkaInstance = KafkaDeployer.DeployNew(settings, log);

                if (started)
                    kafkaInstance.Start();
            },
            3,
            "Unable to start Kafka.Local",
            () =>
            {
                log.Warn("Retrying Kafka.Local deployment...");
                kafkaInstance?.Dispose();
                KafkaDeployer.Cleanup(settings);
            });

            return kafkaInstance;
        }

        public int Port { get; }
        public string ConnectionString => $"localhost:{Port}";
        public string BaseDirectory { get; }
        public string LibDirectory => Path.Combine(BaseDirectory, "libs");
        public string Log4jDirectory => Path.Combine(BaseDirectory, "logs");
        public string ConfigDirectory => Path.Combine(BaseDirectory, "config");
        public string LogDataDirectory => Path.Combine(BaseDirectory, "kafka-logs");
        public string Log4jPropertiesFile => Path.Combine(ConfigDirectory, "log4j.properties");
        public string KafkaPropertiesFile => Path.Combine(ConfigDirectory, "server.properties");
        public bool IsRunning => shellRunner.IsRunning;

        public void Dispose()
        {
            shellRunner.Stop();
            KafkaDeployer.Cleanup(BaseDirectory);
        }

        public void Start()
        {
            shellRunner.Start();

            var timeSpan = 150.Seconds();
            if (!healthChecker.WaitStarted(timeSpan))
                throw new TimeoutException($"Kafka has not warmed up in {timeSpan.TotalSeconds} seconds..");
        }

        private string BuildKafkaArguments()
        {
            var heapOptions = "-Xmx1G -Xms1G";
            var jvmPerformanceOptions = "-server -XX:+UseG1GC -XX:MaxGCPauseMillis=20 -XX:InitiatingHeapOccupancyPercent=35 -XX:+ExplicitGCInvokesConcurrent -Djava.awt.headless=true";
            var log4jOptions = $"-Dkafka.logs.dir=\"{Log4jDirectory}\" \"-Dlog4j.configuration=file:{Log4jPropertiesFile}\"";
            var classPathOption = $"-cp \"{Path.Combine(LibDirectory, "*")}\"";
            return $"{heapOptions} {jvmPerformanceOptions} {log4jOptions} {classPathOption} kafka.Kafka \"{KafkaPropertiesFile}\"";
        }
    }
}