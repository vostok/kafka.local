using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Vostok.Commons.Helpers.Network;
using Vostok.Commons.Local;
using Vostok.Kafka.Local.Helpers;
using Vostok.Logging.Abstractions;

namespace Vostok.Kafka.Local
{
    internal static class KafkaDeployer
    {
        private const string KafkaDirectoryName = "kafka_2.12-2.2.0";

        public static KafkaInstance DeployNew(KafkaSettings settings, ILog log)
        {
            var baseDirectory = GetBaseDirectory(settings);
            Directory.CreateDirectory(baseDirectory);

            var kafkaDirectory = GetKafkaDirectory(settings);
            if (Directory.Exists(kafkaDirectory))
                Directory.Delete(kafkaDirectory, true);

            var deployerLog = log.ForContext(nameof(KafkaDeployer));
            
            deployerLog.Debug("Started Kafka extraction...");
            ResourceHelper.ExtractResource<KafkaInstance>($"Vostok.Kafka.Local.Resources.{KafkaDirectoryName}.tgz", baseDirectory);
            deployerLog.Debug("Finished Kafka extraction.");

            var kafkaPort = FreeTcpPortFinder.GetFreePort();

            var instance = new KafkaInstance(kafkaDirectory, kafkaPort, log);

            GenerateConfig(instance, settings);

            return instance;
        }

        public static void Cleanup(KafkaSettings settings)
        {
            Cleanup(GetKafkaDirectory(settings));
        }

        public static void Cleanup(string kafkaDirectory)
        {
            if (!Directory.Exists(kafkaDirectory))
                return;
            
            for (var i = 0; i < 3; i++)
            {
                try
                {
                    Directory.Delete(kafkaDirectory, true);
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(500);
                }
            }
        }

        private static string GetBaseDirectory(KafkaSettings settings)
        {
            return string.IsNullOrEmpty(settings.BaseDirectory) ? Directory.GetCurrentDirectory() : settings.BaseDirectory;
        }

        private static string GetKafkaDirectory(KafkaSettings settings)
        {
            return Path.Combine(GetBaseDirectory(settings), KafkaDirectoryName);
        }
        
        private static void GenerateConfig(KafkaInstance instance, KafkaSettings settings)
        {
            var properties = new Dictionary<string, string>
            {
                ["broker.id"] = "0",
                ["listeners"] = $"PLAINTEXT://:{instance.Port}",
                ["log.dirs"] = instance.LogDataDirectory,
                ["offsets.topic.replication.factor"] = "1",
                ["transaction.state.log.replication.factor"] = "1",
                ["transaction.state.log.min.isr"] = "1",
                ["log.flush.interval.messages"] = "10000",
                ["log.flush.interval.ms"] = "1000",
                ["zookeeper.connect"] = settings.ZooKeeperConnectionString,
                ["zookeeper.connection.timeout.ms"] = "6000",
                ["group.initial.rebalance.delay.ms"] = "0",
                ["delete.topic.enable"] = $"{settings.DeleteTopicEnable}"
            };

            new JavaProperties(properties).Save(instance.KafkaPropertiesFile);
        }
    }
}