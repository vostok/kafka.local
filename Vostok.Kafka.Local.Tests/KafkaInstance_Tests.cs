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
        public void Should_deploy_and_run_Kafka_by_default()
        {
            using (var kafka = KafkaInstance.DeployNew(zkConnectionString, log))
            {
                kafka.IsRunning.Should().BeTrue();
            }
        }
    }
}