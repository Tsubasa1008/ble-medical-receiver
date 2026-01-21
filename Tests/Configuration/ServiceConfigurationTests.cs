using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using BLEDataReceiver.Configuration;
using BLEDataReceiver.Interfaces;

namespace BLEDataReceiver.Tests.Configuration
{
    /// <summary>
    /// 服務配置測試
    /// </summary>
    [TestFixture]
    public class ServiceConfigurationTests : TestBase
    {
        [Test]
        public void ConfigureServices_ShouldRegisterAllRequiredServices()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();

            // Assert
            Assert.That(serviceProvider.GetService<IBLEReceiver>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IPairingManager>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IConnectionManager>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IDataProcessor>(), Is.Not.Null);
            Assert.That(serviceProvider.GetService<IConsoleInterface>(), Is.Not.Null);
        }

        [Test]
        public void ConfigureServices_ShouldRegisterServicesAsSingleton()
        {
            // Arrange
            var services = new ServiceCollection();
            services.ConfigureServices();
            var serviceProvider = services.BuildServiceProvider();

            // Act
            var bleReceiver1 = serviceProvider.GetService<IBLEReceiver>();
            var bleReceiver2 = serviceProvider.GetService<IBLEReceiver>();

            // Assert
            Assert.That(bleReceiver1, Is.SameAs(bleReceiver2));
        }
    }
}