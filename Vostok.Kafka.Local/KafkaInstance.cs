using System;
using System.IO;
using Vostok.Commons.Local;
using Vostok.Commons.Time;
using Vostok.Kafka.Local.Helpers;
using Vostok.Logging.Abstractions;

// ReSharper disable InconsistentNaming

namespace Vostok.Kafka.Local
{
    public class KafkaInstance : ProcessWrapper, IDisposable
    {
        private readonly KafkaHealthChecker healthChecker;

        internal KafkaInstance(string baseDirectory, int port, ILog log)
            : base(log, "Kafka", true)
        {
            Port = port;
            BaseDirectory = baseDirectory;
            healthChecker = new KafkaHealthChecker(log, $"localhost:{port}");
        }
        
        public static KafkaInstance DeployNew(string zooKeeperConnectionString, ILog log, bool started = true)
        {
            return DeployNew(new KafkaSettings {ZooKeeperConnectionString = zooKeeperConnectionString}, log, started);
        }

        public static KafkaInstance DeployNew(KafkaSettings settings, ILog log, bool started = true)
        {
            KafkaInstance kafkaInstance = null;
            try
            {
                kafkaInstance = KafkaDeployer.DeployNew(settings, log);
                
                if (started)
                    kafkaInstance.Start();

                return kafkaInstance;
            }
            catch (Exception error)
            {
                log.Error(error, "Error in deploy. Will try to stop and cleanup.");
                kafkaInstance?.Dispose();
                KafkaDeployer.Cleanup(settings);
                throw;
            }
        }

        public int Port { get; }
        public string ConnectionString  => $"localhost:{Port}";
        public string BaseDirectory { get; }
        public string LibDirectory => Path.Combine(BaseDirectory, "libs");
        public string Log4jDirectory => Path.Combine(BaseDirectory, "logs");
        public string ConfigDirectory => Path.Combine(BaseDirectory, "config");
        public string LogDataDirectory => Path.Combine(BaseDirectory, "kafka-logs");
        public string Log4jPropertiesFile => Path.Combine(ConfigDirectory, "log4j.properties");
        public string KafkaPropertiesFile => Path.Combine(ConfigDirectory, "server.properties");

        public void Dispose()
        {
            Stop();
            KafkaDeployer.Cleanup(BaseDirectory);
        }

        protected override string FileName => "java";
        protected override string Arguments => BuildKafkaArguments();
        protected override string WorkingDirectory => BaseDirectory;

        public override void Start()
        {
            base.Start();
            
            if (!healthChecker.WaitStarted(20.Seconds()))
                throw new TimeoutException("Kafka has not warmed up in 20 seconds..");
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