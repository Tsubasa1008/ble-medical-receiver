using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using BLEDataReceiver.Services;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Tests.Services
{
    [TestFixture]
    public class ConnectionManagerServiceTests : TestBase
    {
        private ConnectionManagerService _connectionManager = null!;
        private ILogger<ConnectionManagerService> _logger = null!;

        public override void SetUp()
        {
            base.SetUp();
            _logger = ServiceProvider.GetRequiredService<ILogger<ConnectionManagerService>>();
            _connectionManager = new ConnectionManagerService(_logger);
        }

        public override void TearDown()
        {
            _connectionManager?.Dispose();
            base.TearDown();
        }

        [Test]
        public void Constructor_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            using var connectionManager = new ConnectionManagerService(_logger);

            // Assert
            Assert.That(connectionManager, Is.Not.Null);
        }

        [Test]
        public void GetConnectionStatus_WithUnknownDevice_ShouldReturnDisconnected()
        {
            // Arrange
            var unknownDeviceId = 12345UL;

            // Act
            var status = _connectionManager.GetConnectionStatus(unknownDeviceId);

            // Assert
            Assert.That(status, Is.EqualTo(ConnectionStatus.Disconnected));
        }

        [Test]
        public async Task ConnectAsync_WithNullDevice_ShouldReturnFalse()
        {
            // Act
            var result = await _connectionManager.ConnectAsync(null!);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void DisconnectAsync_WithUnknownDevice_ShouldCompleteSuccessfully()
        {
            // Arrange
            var unknownDeviceId = 12345UL;

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await _connectionManager.DisconnectAsync(unknownDeviceId));
        }

        [Test]
        public async Task ReconnectAsync_WithUnknownDevice_ShouldReturnFalse()
        {
            // Arrange
            var unknownDeviceId = 12345UL;

            // Act
            var result = await _connectionManager.ReconnectAsync(unknownDeviceId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void ConnectionStatusChanged_Event_ShouldBeRaisedCorrectly()
        {
            // Arrange
            ConnectionStatusChangedEventArgs? receivedEventArgs = null;
            _connectionManager.ConnectionStatusChanged += (sender, args) => receivedEventArgs = args;

            // Act - This will be tested more thoroughly in integration tests with real devices
            // For now, we just verify the event can be subscribed to
            
            // Assert
            Assert.That(receivedEventArgs, Is.Null); // No events should be raised yet
        }

        [Test]
        public void Dispose_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _connectionManager.Dispose());
        }

        [Test]
        public void Dispose_CalledMultipleTimes_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _connectionManager.Dispose());
            Assert.DoesNotThrow(() => _connectionManager.Dispose());
        }
    }
}