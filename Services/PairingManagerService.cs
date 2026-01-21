using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Enumeration;
using BLEDataReceiver.Interfaces;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Services
{
    /// <summary>
    /// BLE配對管理器服務實現
    /// 負責監聽BLE設備廣播、識別醫療設備並自動配對
    /// </summary>
    public class PairingManagerService : IPairingManager, IDisposable
    {
        private readonly ILogger<PairingManagerService> _logger;
        private BluetoothLEAdvertisementWatcher? _advertisementWatcher;
        private bool _isWatcherStarted = false;
        private bool _disposed = false;

        // IEEE 11073 標準服務 UUID
        private static readonly Guid BloodPressureServiceUuid = new("00001810-0000-1000-8000-00805f9b34fb");
        private static readonly Guid HealthThermometerServiceUuid = new("00001809-0000-1000-8000-00805f9b34fb");

        public PairingManagerService(ILogger<PairingManagerService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<DeviceDiscoveredEventArgs>? DeviceDiscovered;

        /// <summary>
        /// 啟動BLE廣播監聽器
        /// </summary>
        public Task StartAdvertisementWatcherAsync()
        {
            try
            {
                if (_isWatcherStarted)
                {
                    _logger.LogWarning("Advertisement watcher is already started");
                    return Task.CompletedTask;
                }

                _logger.LogInformation("Starting BLE advertisement watcher...");

                // 創建廣播監聽器
                _advertisementWatcher = new BluetoothLEAdvertisementWatcher
                {
                    ScanningMode = BluetoothLEScanningMode.Active,
                    AllowExtendedAdvertisements = true
                };

                // 註冊事件處理器
                _advertisementWatcher.Received += OnAdvertisementReceived;
                _advertisementWatcher.Stopped += OnWatcherStopped;

                // 啟動監聽器
                _advertisementWatcher.Start();
                _isWatcherStarted = true;

                _logger.LogInformation("BLE advertisement watcher started successfully");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start advertisement watcher");
                throw;
            }
        }

        /// <summary>
        /// 停止BLE廣播監聽器
        /// </summary>
        public Task StopAdvertisementWatcherAsync()
        {
            try
            {
                if (!_isWatcherStarted || _advertisementWatcher == null)
                {
                    _logger.LogWarning("Advertisement watcher is not started");
                    return Task.CompletedTask;
                }

                _logger.LogInformation("Stopping BLE advertisement watcher...");

                // 停止監聽器
                _advertisementWatcher.Stop();
                _isWatcherStarted = false;

                _logger.LogInformation("BLE advertisement watcher stopped successfully");
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to stop advertisement watcher");
                throw;
            }
        }

        /// <summary>
        /// 配對BLE設備
        /// </summary>
        /// <param name="device">要配對的設備</param>
        /// <returns>配對是否成功</returns>
        public async Task<bool> PairDeviceAsync(BluetoothLEDevice device)
        {
            if (device == null)
            {
                _logger.LogWarning("Cannot pair null device");
                return false;
            }

            try
            {
                _logger.LogInformation("Attempting to pair device: {DeviceName} ({DeviceId})", 
                    device.Name ?? "Unknown", device.BluetoothAddress);

                // 檢查設備是否已經配對
                if (device.DeviceInformation.Pairing.IsPaired)
                {
                    _logger.LogInformation("Device {DeviceId} is already paired", device.BluetoothAddress);
                    return true;
                }

                // 檢查設備是否可以配對
                if (!device.DeviceInformation.Pairing.CanPair)
                {
                    _logger.LogWarning("Device {DeviceId} cannot be paired", device.BluetoothAddress);
                    return false;
                }

                // 嘗試配對
                var pairingResult = await device.DeviceInformation.Pairing.PairAsync();
                
                if (pairingResult.Status == DevicePairingResultStatus.Paired)
                {
                    _logger.LogInformation("Successfully paired device: {DeviceName} ({DeviceId})", 
                        device.Name ?? "Unknown", device.BluetoothAddress);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Failed to pair device {DeviceId}. Status: {Status}", 
                        device.BluetoothAddress, pairingResult.Status);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while pairing device {DeviceId}", device.BluetoothAddress);
                return false;
            }
        }

        /// <summary>
        /// 檢查是否為目標醫療設備
        /// </summary>
        /// <param name="advertisement">廣播信息</param>
        /// <returns>是否為目標設備</returns>
        public bool IsTargetDevice(BluetoothLEAdvertisement advertisement)
        {
            if (advertisement == null)
                return false;

            try
            {
                // 檢查服務UUID是否包含血壓計或體溫計服務
                var serviceUuids = advertisement.ServiceUuids;
                
                bool isBloodPressureMonitor = serviceUuids.Contains(BloodPressureServiceUuid);
                bool isThermometer = serviceUuids.Contains(HealthThermometerServiceUuid);

                if (isBloodPressureMonitor || isThermometer)
                {
                    _logger.LogDebug("Target device detected: BloodPressure={BloodPressure}, Thermometer={Thermometer}", 
                        isBloodPressureMonitor, isThermometer);
                    return true;
                }

                // 額外檢查：通過設備名稱識別（作為備用方案）
                var localName = advertisement.LocalName?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(localName))
                {
                    bool nameIndicatesBloodPressure = localName.Contains("blood") || localName.Contains("pressure") || localName.Contains("bp");
                    bool nameIndicatesThermometer = localName.Contains("therm") || localName.Contains("temp");
                    
                    if (nameIndicatesBloodPressure || nameIndicatesThermometer)
                    {
                        _logger.LogDebug("Target device detected by name: {LocalName}", localName);
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if device is target device");
                return false;
            }
        }

        /// <summary>
        /// 廣播接收事件處理器
        /// </summary>
        private async void OnAdvertisementReceived(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            try
            {
                // 檢查是否為目標設備
                if (!IsTargetDevice(args.Advertisement))
                    return;

                _logger.LogInformation("Target device advertisement received from {DeviceAddress}", args.BluetoothAddress);

                // 嘗試獲取設備對象
                var device = await BluetoothLEDevice.FromBluetoothAddressAsync(args.BluetoothAddress);
                if (device == null)
                {
                    _logger.LogWarning("Could not get device object for address {DeviceAddress}", args.BluetoothAddress);
                    return;
                }

                // 確定設備類型
                var deviceType = DetermineDeviceType(args.Advertisement);

                // 如果設備未配對，嘗試自動配對
                if (!device.DeviceInformation.Pairing.IsPaired)
                {
                    _logger.LogInformation("Attempting automatic pairing for device: {DeviceName}", device.Name ?? "Unknown");
                    
                    var pairingSuccess = await PairDeviceAsync(device);
                    if (!pairingSuccess)
                    {
                        _logger.LogWarning("Automatic pairing failed for device {DeviceAddress}", args.BluetoothAddress);
                        return;
                    }
                }

                // 觸發設備發現事件
                DeviceDiscovered?.Invoke(this, new DeviceDiscoveredEventArgs(device, deviceType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing advertisement from {DeviceAddress}", args.BluetoothAddress);
            }
        }

        /// <summary>
        /// 監聽器停止事件處理器
        /// </summary>
        private void OnWatcherStopped(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementWatcherStoppedEventArgs args)
        {
            _isWatcherStarted = false;
            _logger.LogInformation("Advertisement watcher stopped. Error: {Error}", args.Error);
        }

        /// <summary>
        /// 根據廣播信息確定設備類型
        /// </summary>
        /// <param name="advertisement">廣播信息</param>
        /// <returns>設備類型</returns>
        private DeviceType DetermineDeviceType(BluetoothLEAdvertisement advertisement)
        {
            var serviceUuids = advertisement.ServiceUuids;

            if (serviceUuids.Contains(BloodPressureServiceUuid))
                return DeviceType.BloodPressureMonitor;

            if (serviceUuids.Contains(HealthThermometerServiceUuid))
                return DeviceType.Thermometer;

            // 備用方案：通過設備名稱判斷
            var localName = advertisement.LocalName?.ToLowerInvariant();
            if (!string.IsNullOrEmpty(localName))
            {
                if (localName.Contains("blood") || localName.Contains("pressure") || localName.Contains("bp"))
                    return DeviceType.BloodPressureMonitor;

                if (localName.Contains("therm") || localName.Contains("temp"))
                    return DeviceType.Thermometer;
            }

            // 默認返回血壓計（如果無法確定）
            return DeviceType.BloodPressureMonitor;
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            try
            {
                if (_isWatcherStarted && _advertisementWatcher != null)
                {
                    _advertisementWatcher.Stop();
                }

                if (_advertisementWatcher != null)
                {
                    _advertisementWatcher.Received -= OnAdvertisementReceived;
                    _advertisementWatcher.Stopped -= OnWatcherStopped;
                    _advertisementWatcher = null;
                }

                _isWatcherStarted = false;
                _disposed = true;

                _logger.LogInformation("PairingManagerService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing PairingManagerService");
            }
        }
    }
}