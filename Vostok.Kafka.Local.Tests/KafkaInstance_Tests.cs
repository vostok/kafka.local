using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;
using Vostok.ZooKeeper.LocalEnsemble;

namespace Vostok.Kafka.Local.Tests
{
    [TestFixture]
    internal class KafkaInstance_Tests
    {
        private readonly ILog log = new SynchronousConsoleLog();
        
        [Test]
        public void DeployNew_should_run_kafka_by_default()
        {
            using (var zooKeeperEnsemble = DeployZooKeeper())
            using (var kafka = KafkaInstance.DeployNew(zooKeeperEnsemble.ConnectionString, log))
            {
                kafka.IsRunning.Should().BeTrue();
            }
        }

        [Test]
        public void DeployNew_should_not_run_kafka_if_specified()
        {
            using (var zooKeeperEnsemble = DeployZooKeeper())
            using (var kafka = KafkaInstance.DeployNew(zooKeeperEnsemble.ConnectionString, log, false))
            {
                kafka.IsRunning.Should().BeFalse();
            }
        }

        [Test]
        public void DeployNew_should_run_multiple_times()
        {
            using (var zooKeeperEnsemble = DeployZooKeeper())
            using (var kafka = KafkaInstance.DeployNew(zooKeeperEnsemble.ConnectionString, log))
            {
                kafka.IsRunning.Should().BeTrue();
            }
            
            using (var zooKeeperEnsemble = DeployZooKeeper())
            using (var kafka = KafkaInstance.DeployNew(zooKeeperEnsemble.ConnectionString, log))
            {
                kafka.IsRunning.Should().BeTrue();
            }
        }

        private ZooKeeperEnsemble DeployZooKeeper()
        {
            return ZooKeeperEnsemble.DeployNew(1, log);
        }
    }
}