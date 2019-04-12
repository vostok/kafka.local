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
        private ZooKeeperEnsemble zooKeeperEnsemble;
        private string zkConnectionString;

        [SetUp]
        public void SetUp()
        {
            zooKeeperEnsemble = ZooKeeperEnsemble.DeployNew(1, log);
            zkConnectionString = $"localhost:{zooKeeperEnsemble.Instances[0].ClientPort}";
        }

        [TearDown]
        public void TearDown()
        {
            zooKeeperEnsemble.Dispose();
        }
        
        [Test]
        public void DeployNew_should_run_kafka_by_default()
        {
            using (var kafka = KafkaInstance.DeployNew(zkConnectionString, log))
            {
                kafka.IsRunning.Should().BeTrue();
            }
        }

        [Test]
        public void DeployNew_should_not_run_kafka_if_specified()
        {
            using (var kafka = KafkaInstance.DeployNew(zkConnectionString, log, false))
            {
                kafka.IsRunning.Should().BeFalse();
            }
        }

        [Test]
        public void DeployNew_should_run_multiple_times()
        {
            using (var kafka = KafkaInstance.DeployNew(zkConnectionString, log))
            {
                kafka.IsRunning.Should().BeTrue();
            }
            
            using (var kafka = KafkaInstance.DeployNew(zkConnectionString, log))
            {
                kafka.IsRunning.Should().BeTrue();
            }
        }
    }
}