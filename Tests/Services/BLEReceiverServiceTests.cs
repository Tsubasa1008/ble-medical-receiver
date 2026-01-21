using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using BLEDataReceiver.Interfaces;
using BLEDataReceiver.Models;
using BLEDataReceiver.Services;

namespace BLEDataReceiver.Tests.Services
{
    [TestFixture]
    public class BLEReceiverServiceTests : TestBase
    {
        private TestPairingManager _testPairingManager = null!;
        private TestConnectionManager _testConnectionManager = null!;
        private TestDataProcessor _testDataProcessor = null!;
        private ILogger<BLEReceiverService> _logger = null!;
        private BLEReceiverService _bleReceiverService = null!;

        public override void SetUp()
        {
            base.SetUp();
            
            _testPairingManager = new TestPairingManager();
            _testConnectionManager = new TestConnectionManager();
            _testDataProcessor = new TestDataProcessor();
            _logger = ServiceProvider.GetRequiredService<ILogger<BLEReceiverService>>();

            _bleReceiverService = new BLEReceiverService(
                _testPairingManager,
                _testConnectionManager,
                _testDataProcessor,
                _logger);
        }

        public override void TearDown()
        {
            _bleReceiverService?.Dispose();
            base.TearDown();
        }

        [Test]
        public async Task StartAsync_ShouldStartPairingManager_WhenCalled()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)); // 2秒超時
            var cancellationToken = cts.Token;

            try
            {
                // Act
                await _bleReceiverService.StartAsync(cancellationToken);

                // Assert
                Assert.That(_testPairingManager.StartAdvertisementWatcherCalled, Is.True);
            }
            catch (OperationCanceledException)
            {
                Assert.Fail("StartAsync operation timed out");
            }
        }

        [Test]
        public async Task StartAsync_ShouldNotStartTwice_WhenCalledMultipleTimes()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)); // 2秒超時
            var cancellationToken = cts.Token;

            try
            {
                // Act
                await _bleReceiverService.StartAsync(cancellationToken);
                await _bleReceiverService.StartAsync(cancellationToken);

                // Assert
                Assert.That(_testPairingManager.StartAdvertisementWatcherCallCount, Is.EqualTo(1));
            }
            catch (OperationCanceledException)
            {
                Assert.Fail("StartAsync operation timed out");
            }
        }

        [Test]
        public async Task StopAsync_ShouldCompleteSuccessfully_WhenStarted()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)); // 2秒超時
            var cancellationToken = cts.Token;
            
            try
            {
                await _bleReceiverService.StartAsync(cancellationToken);

                // Act & Assert
                await _bleReceiverService.StopAsync();
                Assert.Pass("StopAsync completed successfully");
            }
            catch (OperationCanceledException)
            {
                Assert.Fail("Operation timed out");
            }
        }

        [Test]
        public void DataReceived_ShouldBeTriggered_WhenValidDataProcessed()
        {
            // Arrange
            var testData = new BloodPressureData
            {
                DeviceId = "TestDevice",
                DeviceType = DeviceType.BloodPressureMonitor,
                SystolicPressure = 120,
                DiastolicPressure = 80,
                HeartRate = 70,
                Timestamp = DateTime.Now,
                IsValid = true
            };

            DeviceDataReceivedEventArgs? receivedArgs = null;
            _bleReceiverService.DataReceived += (sender, args) => receivedArgs = args;

            // Act
            _testDataProcessor.TriggerDataProcessed(testData, true);

            // Assert
            Assert.That(receivedArgs, Is.Not.Null);
            Assert.That(receivedArgs!.DeviceId, Is.EqualTo(testData.DeviceId));
            Assert.That(receivedArgs.Data, Is.EqualTo(testData));
        }

        [Test]
        public void DataReceived_ShouldNotBeTriggered_WhenInvalidDataProcessed()
        {
            // Arrange
            var testData = new BloodPressureData
            {
                DeviceId = "TestDevice",
                DeviceType = DeviceType.BloodPressureMonitor,
                IsValid = false
            };

            DeviceDataReceivedEventArgs? receivedArgs = null;
            _bleReceiverService.DataReceived += (sender, args) => receivedArgs = args;

            // Act
            _testDataProcessor.TriggerDataProcessed(testData, false, "Invalid data");

            // Assert
            Assert.That(receivedArgs, Is.Null);
        }

        [Test]
        public void ConnectionStatusChanged_ShouldBeForwarded_WhenConnectionManagerRaisesEvent()
        {
            // Arrange
            ConnectionStatusChangedEventArgs? receivedArgs = null;
            _bleReceiverService.ConnectionStatusChanged += (sender, args) => receivedArgs = args;

            // Act
            _testConnectionManager.TriggerConnectionStatusChanged("TestDevice", ConnectionStatus.Connected);

            // Assert
            Assert.That(receivedArgs, Is.Not.Null);
            Assert.That(receivedArgs!.DeviceId, Is.EqualTo("TestDevice"));
            Assert.That(receivedArgs.Status, Is.EqualTo(ConnectionStatus.Connected));
        }

        [Test]
        public async Task StartAsync_ShouldCleanupResources_WhenPairingManagerFails()
        {
            // Arrange
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            var cancellationToken = cts.Token;
            _testPairingManager.ShouldThrowOnStart = true;

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(
                () => _bleReceiverService.StartAsync(cancellationToken));

            // 驗證清理是否正確執行
            // 注意：在實際實現中，我們無法直接驗證內部狀態，
            // 但可以通過後續操作來間接驗證
            Assert.DoesNotThrowAsync(async () => await _bleReceiverService.StopAsync());
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenPairingManagerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BLEReceiverService(
                null!,
                _testConnectionManager,
                _testDataProcessor,
                _logger));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenConnectionManagerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BLEReceiverService(
                _testPairingManager,
                null!,
                _testDataProcessor,
                _logger));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenDataProcessorIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BLEReceiverService(
                _testPairingManager,
                _testConnectionManager,
                null!,
                _logger));
        }

        [Test]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new BLEReceiverService(
                _testPairingManager,
                _testConnectionManager,
                _testDataProcessor,
                null!));
        }

        [Test]
        public void Dispose_ShouldNotThrow_WhenCalledMultipleTimes()
        {
            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                _bleReceiverService.Dispose();
                _bleReceiverService.Dispose();
            });
        }

        [Test]
        public async Task StopAsync_ShouldNotThrow_WhenNotStarted()
        {
            // Act & Assert
            await Task.Run(async () =>
            {
                await _bleReceiverService.StopAsync();
            });
            
            Assert.Pass("StopAsync completed without throwing");
        }
    }

    // Test helper classes
    internal class TestPairingManager : IPairingManager
    {
        public bool StartAdvertisementWatcherCalled { get; private set; }
        public int StartAdvertisementWatcherCallCount { get; private set; }
        public bool ShouldThrowOnStart { get; set; }

        public event EventHandler<DeviceDiscoveredEventArgs>? DeviceDiscovered;

        public Task StartAdvertisementWatcherAsync()
        {
            StartAdvertisementWatcherCalled = true;
            StartAdvertisementWatcherCallCount++;

            if (ShouldThrowOnStart)
            {
                throw new InvalidOperationException("Test exception");
            }

            return Task.CompletedTask;
        }

        public Task StopAdvertisementWatcherAsync()
        {
            return Task.CompletedTask;
        }

        public Task<bool> PairDeviceAsync(Windows.Devices.Bluetooth.BluetoothLEDevice device)
        {
            return Task.FromResult(true);
        }

        public bool IsTargetDevice(Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisement advertisement)
        {
            return true;
        }

        public void TriggerDeviceDiscovered(Windows.Devices.Bluetooth.BluetoothLEDevice device, DeviceType deviceType)
        {
            DeviceDiscovered?.Invoke(this, new DeviceDiscoveredEventArgs(device, deviceType));
        }
    }

    internal class TestConnectionManager : IConnectionManager
    {
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        public Task<bool> ConnectAsync(Windows.Devices.Bluetooth.BluetoothLEDevice device)
        {
            return Task.FromResult(true);
        }

        public Task DisconnectAsync(ulong deviceId)
        {
            return Task.CompletedTask;
        }

        public Task<bool> ReconnectAsync(ulong deviceId)
        {
            return Task.FromResult(true);
        }

        public ConnectionStatus GetConnectionStatus(ulong deviceId)
        {
            return ConnectionStatus.Connected;
        }

        public void TriggerConnectionStatusChanged(string deviceId, ConnectionStatus status, string? errorMessage = null)
        {
            ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(deviceId, status, errorMessage));
        }
    }

    internal class TestDataProcessor : IDataProcessor
    {
        public event EventHandler<DataProcessedEventArgs>? DataProcessed;

        public Task<MedicalData> ProcessDataAsync(Windows.Devices.Bluetooth.GenericAttributeProfile.GattCharacteristic characteristic, byte[] rawData)
        {
            var data = new BloodPressureData
            {
                DeviceId = "TestDevice",
                DeviceType = DeviceType.BloodPressureMonitor,
                Timestamp = DateTime.Now,
                IsValid = true
            };
            return Task.FromResult<MedicalData>(data);
        }

        public bool ValidateData(MedicalData data)
        {
            return data?.IsValid ?? false;
        }

        public void TriggerDataProcessed(MedicalData data, bool isValid, string? errorMessage = null)
        {
            DataProcessed?.Invoke(this, new DataProcessedEventArgs(data, isValid, errorMessage));
        }
    }
}