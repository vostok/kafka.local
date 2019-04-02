using System;
using System.IO;
using System.Threading;
using Vostok.Commons.Helpers.Network;
using Vostok.Kafka.Local.Helpers;
using Vostok.Logging.Abstractions;

namespace Vostok.Kafka.Local
{
    internal static class KafkaDeployer
    {
        private const string KafkaDirectoryName = "kafka_2.11-2.1.0";

        public static KafkaInstance DeployNew(KafkaSettings settings, ILog log, bool started = true)
        {
            var baseDirectory = CreateBaseDirectory(settings);
            var kafkaDirectory = Path.Combine(baseDirectory, KafkaDirectoryName);
            if (Directory.Exists(kafkaDirectory))
                Directory.Delete(kafkaDirectory, true);

            var deployerLog = log.ForContext(nameof(KafkaDeployer));
            
            deployerLog.Debug("Started Kafka extraction...");
            ResourceHelper.ExtractResource<KafkaInstance>($"Vostok.Kafka.Local.Resources.{KafkaDirectoryName}.tgz", baseDirectory);
            deployerLog.Debug("Finished Kafka extraction.");

            var kafkaPort = FreeTcpPortFinder.GetFreePort();

            var instance = new KafkaInstance(kafkaDirectory, kafkaPort, log);

            GenerateConfig(instance, settings);

            if (started)
                instance.Start();

            return instance;
        }

        public static void CleanupInstance(KafkaInstance instance)
        {
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(instance.BaseDirectory, true);
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(500);
                }
            }
        }

        private static string CreateBaseDirectory(KafkaSettings settings)
        {
            var baseDirectory = settings.BaseDirectory;
            if (string.IsNullOrEmpty(baseDirectory))
                baseDirectory = Directory.GetCurrentDirectory();

            Directory.CreateDirectory(baseDirectory);

            return baseDirectory;
        }

        private static void GenerateConfig(KafkaInstance instance, KafkaSettings settings)
        {
            var properties = new JavaProperties();
            properties.SetProperty("broker.id", "0");
            properties.SetProperty("listeners", $"PLAINTEXT://:{instance.Port}");
            properties.SetProperty("log.dirs", instance.LogDataDirectory);
            properties.SetProperty("offsets.topic.replication.factor", "1");
            properties.SetProperty("transaction.state.log.replication.factor", "1");
            properties.SetProperty("transaction.state.log.min.isr", "1");
            properties.SetProperty("log.flush.interval.messages", "10000");
            properties.SetProperty("log.flush.interval.ms", "1000");
            properties.SetProperty("zookeeper.connect", settings.ZooKeeperConnectionString);
            properties.SetProperty("zookeeper.connection.timeout.ms", "6000");
            properties.SetProperty("group.initial.rebalance.delay.ms", "0");
            properties.SetProperty("delete.topic.enable", $"{settings.DeleteTopicEnable}");

            properties.Save(instance.KafkaPropertiesFile);
        }
    }
}