using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using BLEDataReceiver.Interfaces;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Services
{
    /// <summary>
    /// BLE接收器核心服務實現
    /// 協調配對管理器、連接管理器和數據處理器，實現完整的BLE數據接收功能
    /// </summary>
    public class BLEReceiverService : IBLEReceiver, IDisposable
    {
        private readonly IPairingManager _pairingManager;
        private readonly IConnectionManager _connectionManager;
        private readonly IDataProcessor _dataProcessor;
        private readonly ILogger<BLEReceiverService> _logger;
        
        private readonly ConcurrentDictionary<ulong, DeviceSubscriptionInfo> _deviceSubscriptions;
        private readonly SemaphoreSlim _operationSemaphore;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isStarted = false;
        private bool _disposed = false;

        // IEEE 11073 標準特徵值 UUID
        private static readonly Guid BloodPressureMeasurementUuid = new("00002a35-0000-1000-8000-00805f9b34fb");
        private static readonly Guid TemperatureMeasurementUuid = new("00002a1c-0000-1000-8000-00805f9b34fb");
        
        // 常見的體溫計特徵值UUID（一些設備可能使用不同的UUID）
        private static readonly Guid[] TemperatureCharacteristicUuids = {
            new("00002a1c-0000-1000-8000-00805f9b34fb"), // 標準 Temperature Measurement
            new("00002a1e-0000-1000-8000-00805f9b34fb"), // Intermediate Temperature
            new("0000fff1-0000-1000-8000-00805f9b34fb"), // 一些廠商自定義UUID
            new("0000fff4-0000-1000-8000-00805f9b34fb"), // 另一個常見的自定義UUID
        };

        public BLEReceiverService(
            IPairingManager pairingManager,
            IConnectionManager connectionManager,
            IDataProcessor dataProcessor,
            ILogger<BLEReceiverService> logger)
        {
            _pairingManager = pairingManager ?? throw new ArgumentNullException(nameof(pairingManager));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _dataProcessor = dataProcessor ?? throw new ArgumentNullException(nameof(dataProcessor));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _deviceSubscriptions = new ConcurrentDictionary<ulong, DeviceSubscriptionInfo>();
            _operationSemaphore = new SemaphoreSlim(1, 1);

            // 註冊事件處理器
            _pairingManager.DeviceDiscovered += OnDeviceDiscovered;
            _connectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            _dataProcessor.DataProcessed += OnDataProcessed;
        }

        public event EventHandler<DeviceDataReceivedEventArgs>? DataReceived;
        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// 手動斷開所有設備連接
        /// </summary>
        public async Task DisconnectAllDevicesAsync()
        {
            try
            {
                _logger.LogInformation("Manually disconnecting all devices");
                
                var deviceIds = _deviceSubscriptions.Keys.ToList();
                var disconnectTasks = new List<Task>();

                foreach (var deviceId in deviceIds)
                {
                    disconnectTasks.Add(_connectionManager.DisconnectAsync(deviceId));
                }

                await Task.WhenAll(disconnectTasks);
                _logger.LogInformation("All devices disconnected manually");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during manual disconnect of all devices");
            }
        }

        /// <summary>
        /// 獲取當前連接的設備信息
        /// </summary>
        public List<(ulong DeviceId, string DeviceName, DeviceType DeviceType, int ActiveSubscriptions)> GetConnectedDevices()
        {
            var result = new List<(ulong, string, DeviceType, int)>();
            
            foreach (var kvp in _deviceSubscriptions)
            {
                var deviceId = kvp.Key;
                var subscriptionInfo = kvp.Value;
                var deviceName = subscriptionInfo.Device.Name ?? "Unknown";
                var deviceType = subscriptionInfo.DeviceType;
                var activeSubscriptions = subscriptionInfo.Characteristics.Count;
                
                result.Add((deviceId, deviceName, deviceType, activeSubscriptions));
            }
            
            return result;
        }

        /// <summary>
        /// 啟動BLE接收器
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await _operationSemaphore.WaitAsync(cancellationToken);
            try
            {
                if (_isStarted)
                {
                    _logger.LogWarning("BLE Receiver is already started");
                    return;
                }

                _logger.LogInformation("Starting BLE Receiver...");

                // 創建取消令牌源
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                // 啟動配對管理器
                await _pairingManager.StartAdvertisementWatcherAsync();

                // 啟動連接健康檢查
                _ = Task.Run(() => ConnectionHealthCheckAsync(_cancellationTokenSource.Token));

                _isStarted = true;
                _logger.LogInformation("BLE Receiver started successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to start BLE Receiver");
                
                // 在異常情況下進行清理，但不重新獲取semaphore
                await CleanupOnStartFailureAsync();
                
                throw;
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// 連接健康檢查（Windows平台優化）
        /// </summary>
        private async Task ConnectionHealthCheckAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var now = DateTime.UtcNow;
                    var devicesToCheck = _deviceSubscriptions.ToList();

                    foreach (var kvp in devicesToCheck)
                    {
                        var deviceId = kvp.Key;
                        var subscriptionInfo = kvp.Value;

                        // 如果設備有訂閱但超過30秒沒有收到數據，進行主動檢查
                        if (subscriptionInfo.Characteristics.Count > 0 && 
                            (now - subscriptionInfo.LastDataReceived).TotalSeconds > 30)
                        {
                            _logger.LogDebug("Performing health check for device {DeviceId} - no data for {Seconds} seconds", 
                                deviceId, (now - subscriptionInfo.LastDataReceived).TotalSeconds);

                            var isHealthy = await CheckDeviceHealthAsync(subscriptionInfo);
                            if (!isHealthy)
                            {
                                _logger.LogWarning("Device {DeviceId} failed health check, cleaning up connection", deviceId);
                                await CleanupDeviceSubscriptionAsync(deviceId);
                            }
                        }
                    }

                    // 每10秒檢查一次
                    await Task.Delay(10000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in connection health check");
            }
        }

        /// <summary>
        /// 檢查設備健康狀態
        /// </summary>
        private async Task<bool> CheckDeviceHealthAsync(DeviceSubscriptionInfo subscriptionInfo)
        {
            try
            {
                var device = subscriptionInfo.Device;

                // 快速檢查：嘗試讀取設備信息
                using var cts = new CancellationTokenSource(2000); // 2秒超時
                
                // 嘗試獲取GATT服務
                var gattResult = await device.GetGattServicesAsync().AsTask(cts.Token);
                
                if (gattResult.Status != GattCommunicationStatus.Success)
                {
                    return false;
                }

                // 清理獲取的服務
                foreach (var service in gattResult.Services)
                {
                    service?.Dispose();
                }

                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Device health check timed out for device {DeviceId}", 
                    subscriptionInfo.Device.BluetoothAddress);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Device health check failed for device {DeviceId}", 
                    subscriptionInfo.Device.BluetoothAddress);
                return false;
            }
        }

        /// <summary>
        /// 在啟動失敗時進行清理（不需要semaphore）
        /// </summary>
        private async Task CleanupOnStartFailureAsync()
        {
            try
            {
                _logger.LogInformation("Cleaning up after start failure...");

                // 使用相同的清理邏輯，但不檢查_isStarted狀態
                // 因為在啟動失敗時，狀態可能不一致

                // 取消所有操作
                _cancellationTokenSource?.Cancel();

                // 停止配對管理器（如果已啟動）
                if (_pairingManager is PairingManagerService pairingService)
                {
                    try
                    {
                        await pairingService.StopAdvertisementWatcherAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error stopping pairing manager during cleanup");
                    }
                }

                // 斷開所有設備連接
                await DisconnectAllDevicesAsync();

                // 清理訂閱
                await CleanupAllSubscriptionsAsync();

                // 清理狀態
                _isStarted = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;

                _logger.LogInformation("Cleanup after start failure completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during start failure cleanup");
            }
        }

        /// <summary>
        /// 停止BLE接收器
        /// </summary>
        public async Task StopAsync()
        {
            await _operationSemaphore.WaitAsync();
            try
            {
                if (!_isStarted)
                {
                    _logger.LogWarning("BLE Receiver is not started");
                    return;
                }

                await StopInternalAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping BLE Receiver");
            }
            finally
            {
                _operationSemaphore.Release();
            }
        }

        /// <summary>
        /// 內部停止邏輯（不需要semaphore）
        /// </summary>
        private async Task StopInternalAsync()
        {
            _logger.LogInformation("Stopping BLE Receiver...");

            // 取消所有操作
            _cancellationTokenSource?.Cancel();

            // 停止配對管理器
            if (_pairingManager is PairingManagerService pairingService)
            {
                try
                {
                    await pairingService.StopAdvertisementWatcherAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error stopping pairing manager");
                }
            }

            // 斷開所有設備連接
            await DisconnectAllDevicesAsync();

            // 清理訂閱
            await CleanupAllSubscriptionsAsync();

            // 清理狀態
            _isStarted = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _logger.LogInformation("BLE Receiver stopped successfully");
        }

        /// <summary>
        /// 設備發現事件處理器
        /// </summary>
        private async void OnDeviceDiscovered(object? sender, DeviceDiscoveredEventArgs e)
        {
            try
            {
                _logger.LogInformation("Device discovered: {DeviceName} ({DeviceId}), Type: {DeviceType}",
                    e.Device.Name ?? "Unknown", e.Device.BluetoothAddress, e.DeviceType);

                // 檢查設備是否已經連接並且有活躍的訂閱
                if (_deviceSubscriptions.TryGetValue(e.Device.BluetoothAddress, out var existingSubscription))
                {
                    if (existingSubscription.Characteristics.Count > 0)
                    {
                        _logger.LogDebug("Device {DeviceId} already has active subscriptions, skipping reconnection", 
                            e.Device.BluetoothAddress);
                        return;
                    }
                }

                // Windows平台特殊處理：確保前一次連接完全清理
                await EnsureDeviceFullyDisconnectedAsync(e.Device);

                // 嘗試連接到設備
                var connected = await _connectionManager.ConnectAsync(e.Device);
                if (!connected)
                {
                    _logger.LogWarning("Failed to connect to discovered device: {DeviceId}", e.Device.BluetoothAddress);
                    return;
                }

                // 等待一小段時間讓連接穩定
                await Task.Delay(500);

                // 訂閱GATT特徵值
                await SubscribeToDeviceCharacteristicsAsync(e.Device, e.DeviceType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling device discovery for device: {DeviceId}", e.Device.BluetoothAddress);
            }
        }

        /// <summary>
        /// 確保設備完全斷開連接（Windows平台特殊處理）
        /// </summary>
        private async Task EnsureDeviceFullyDisconnectedAsync(BluetoothLEDevice device)
        {
            try
            {
                _logger.LogDebug("Ensuring device {DeviceId} is fully disconnected before new connection", device.BluetoothAddress);

                // 1. 清理現有訂閱
                await CleanupDeviceSubscriptionAsync(device.BluetoothAddress);

                // 2. 確保連接管理器斷開連接
                await _connectionManager.DisconnectAsync(device.BluetoothAddress);

                // 3. Windows特殊處理：嘗試清理GATT服務緩存
                try
                {
                    // 獲取並立即釋放GATT服務以清理緩存
                    var gattResult = await device.GetGattServicesAsync();
                    if (gattResult.Status == GattCommunicationStatus.Success)
                    {
                        foreach (var service in gattResult.Services)
                        {
                            service?.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Error during GATT cache cleanup for device {DeviceId}", device.BluetoothAddress);
                }

                // 4. 等待系統完成清理
                await Task.Delay(1000);

                _logger.LogDebug("Device {DeviceId} cleanup completed", device.BluetoothAddress);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during device cleanup for {DeviceId}", device.BluetoothAddress);
            }
        }

        /// <summary>
        /// 連接狀態變化事件處理器
        /// </summary>
        private void OnConnectionStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            try
            {
                _logger.LogInformation("Connection status changed for device {DeviceId}: {Status}",
                    e.DeviceId, e.Status);

                // 轉發連接狀態變化事件
                ConnectionStatusChanged?.Invoke(this, e);

                // 根據連接狀態處理訂閱
                if (ulong.TryParse(e.DeviceId, out var deviceId))
                {
                    switch (e.Status)
                    {
                        case ConnectionStatus.Disconnected:
                        case ConnectionStatus.Failed:
                            // 連接斷開時清理訂閱，但不立即刪除，以便重連時可以重用
                            _ = Task.Run(() => CleanupDeviceSubscriptionAsync(deviceId));
                            break;
                            
                        case ConnectionStatus.Connected:
                            // 連接成功時，檢查是否需要重新訂閱
                            if (_deviceSubscriptions.TryGetValue(deviceId, out var subscription))
                            {
                                if (subscription.Characteristics.Count == 0)
                                {
                                    _logger.LogInformation("Device {DeviceId} reconnected, attempting to resubscribe to characteristics", deviceId);
                                    _ = Task.Run(async () =>
                                    {
                                        await Task.Delay(1000); // 等待連接穩定
                                        await SubscribeToDeviceCharacteristicsAsync(subscription.Device, subscription.DeviceType);
                                    });
                                }
                            }
                            break;
                            
                        case ConnectionStatus.Reconnecting:
                            // 重連中，不做任何操作
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling connection status change for device: {DeviceId}", e.DeviceId);
            }
        }

        /// <summary>
        /// 數據處理事件處理器
        /// </summary>
        private void OnDataProcessed(object? sender, DataProcessedEventArgs e)
        {
            try
            {
                if (e.IsValid)
                {
                    _logger.LogInformation("Valid data received from device {DeviceId}: {DeviceType}",
                        e.ProcessedData.DeviceId, e.ProcessedData.DeviceType);

                    // 觸發數據接收事件
                    DataReceived?.Invoke(this, new DeviceDataReceivedEventArgs(e.ProcessedData, e.ProcessedData.DeviceId));

                    // Windows BLE現實策略：
                    // 由於Windows無法像iOS(1秒)或Android(3秒)那樣快速檢測斷線，
                    // 我們採用主動斷開策略來確保下次測量的成功率
                    if (ulong.TryParse(e.ProcessedData.DeviceId, NumberStyles.HexNumber, null, out var deviceId))
                    {
                        _logger.LogInformation("Scheduling smart disconnect for device {DeviceId} - Windows BLE optimization", 
                            deviceId);
                        
                        _ = Task.Run(async () =>
                        {
                            // 策略1: 短期內允許多次測量 (30秒)
                            await Task.Delay(30000);
                            
                            // 檢查是否在這30秒內有新的數據
                            if (_deviceSubscriptions.TryGetValue(deviceId, out var subscriptionInfo))
                            {
                                var timeSinceLastData = DateTime.UtcNow - subscriptionInfo.LastDataReceived;
                                if (timeSinceLastData.TotalSeconds >= 25) // 如果25秒內沒有新數據
                                {
                                    _logger.LogInformation("Auto-disconnecting device {DeviceId} after 30 seconds - preventing Windows BLE cache issues", 
                                        deviceId);
                                    await _connectionManager.DisconnectAsync(deviceId);
                                    
                                    // 策略2: 強制等待讓Windows系統清理
                                    await Task.Delay(5000);
                                    _logger.LogDebug("Windows BLE cleanup delay completed for device {DeviceId}", deviceId);
                                }
                                else
                                {
                                    _logger.LogDebug("Device {DeviceId} still active, extending connection time", deviceId);
                                    
                                    // 策略3: 如果仍在使用，再給30秒，然後強制斷開
                                    await Task.Delay(30000);
                                    _logger.LogInformation("Force disconnecting device {DeviceId} after extended use - Windows BLE maintenance", 
                                        deviceId);
                                    await _connectionManager.DisconnectAsync(deviceId);
                                    await Task.Delay(5000); // 強制清理時間
                                }
                            }
                        });
                    }
                }
                else
                {
                    _logger.LogWarning("Invalid data received from device {DeviceId}: {ErrorMessage}",
                        e.ProcessedData.DeviceId, e.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling processed data from device: {DeviceId}", e.ProcessedData?.DeviceId);
            }
        }

        /// <summary>
        /// 訂閱設備GATT特徵值
        /// </summary>
        private async Task SubscribeToDeviceCharacteristicsAsync(BluetoothLEDevice device, DeviceType deviceType)
        {
            try
            {
                _logger.LogInformation("Subscribing to characteristics for device: {DeviceId}, Type: {DeviceType}",
                    device.BluetoothAddress, deviceType);

                // 檢查設備是否已經有訂閱
                if (_deviceSubscriptions.TryGetValue(device.BluetoothAddress, out var existingSubscription))
                {
                    if (existingSubscription.Characteristics.Count > 0)
                    {
                        _logger.LogInformation("Device {DeviceId} already has {Count} active subscriptions, skipping", 
                            device.BluetoothAddress, existingSubscription.Characteristics.Count);
                        return;
                    }
                }

                // 獲取GATT服務，增加重試機制
                GattDeviceServicesResult gattResult;
                int retryCount = 0;
                const int maxRetries = 3;
                
                do
                {
                    gattResult = await device.GetGattServicesAsync();
                    if (gattResult.Status == GattCommunicationStatus.Success)
                        break;
                        
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        _logger.LogWarning("Failed to get GATT services for device {DeviceId} (attempt {Attempt}/{MaxRetries}): {Status}",
                            device.BluetoothAddress, retryCount, maxRetries, gattResult.Status);
                        await Task.Delay(1000); // 等待1秒後重試
                    }
                } while (retryCount < maxRetries);

                if (gattResult.Status != GattCommunicationStatus.Success)
                {
                    _logger.LogError("Failed to get GATT services for device {DeviceId} after {MaxRetries} attempts: {Status}",
                        device.BluetoothAddress, maxRetries, gattResult.Status);
                    return;
                }

                _logger.LogInformation("Found {ServiceCount} GATT services for device {DeviceId}",
                    gattResult.Services.Count, device.BluetoothAddress);

                var subscriptionInfo = new DeviceSubscriptionInfo
                {
                    Device = device,
                    DeviceType = deviceType,
                    Characteristics = new List<GattCharacteristic>()
                };

                // 根據設備類型訂閱相應的特徵值
                foreach (var service in gattResult.Services)
                {
                    _logger.LogDebug("Checking service {ServiceUuid} on device {DeviceId}",
                        service.Uuid, device.BluetoothAddress);

                    // 獲取特徵值，增加重試機制
                    GattCharacteristicsResult characteristicsResult;
                    retryCount = 0;
                    
                    do
                    {
                        characteristicsResult = await service.GetCharacteristicsAsync();
                        if (characteristicsResult.Status == GattCommunicationStatus.Success)
                            break;
                            
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            _logger.LogWarning("Failed to get characteristics for service {ServiceId} on device {DeviceId} (attempt {Attempt}/{MaxRetries}): {Status}",
                                service.Uuid, device.BluetoothAddress, retryCount, maxRetries, characteristicsResult.Status);
                            await Task.Delay(500); // 等待0.5秒後重試
                        }
                    } while (retryCount < maxRetries);

                    if (characteristicsResult.Status != GattCommunicationStatus.Success)
                    {
                        _logger.LogWarning("Failed to get characteristics for service {ServiceId} on device {DeviceId} after {MaxRetries} attempts: {Status}",
                            service.Uuid, device.BluetoothAddress, maxRetries, characteristicsResult.Status);
                        continue;
                    }

                    _logger.LogDebug("Found {CharacteristicCount} characteristics in service {ServiceUuid}",
                        characteristicsResult.Characteristics.Count, service.Uuid);

                    foreach (var characteristic in characteristicsResult.Characteristics)
                    {
                        _logger.LogDebug("Checking characteristic {CharacteristicUuid} with properties {Properties}",
                            characteristic.Uuid, characteristic.CharacteristicProperties);

                        if (await ShouldSubscribeToCharacteristic(characteristic, deviceType))
                        {
                            _logger.LogInformation("Attempting to subscribe to characteristic {CharacteristicUuid}",
                                characteristic.Uuid);

                            var subscribed = await SubscribeToCharacteristicWithRetryAsync(characteristic);
                            if (subscribed)
                            {
                                subscriptionInfo.Characteristics.Add(characteristic);
                                _logger.LogInformation("Successfully subscribed to characteristic {CharacteristicId} on device {DeviceId}",
                                    characteristic.Uuid, device.BluetoothAddress);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to subscribe to characteristic {CharacteristicUuid} on device {DeviceId}",
                                    characteristic.Uuid, device.BluetoothAddress);
                            }
                        }
                        else
                        {
                            _logger.LogDebug("Skipping characteristic {CharacteristicUuid} - not target for device type {DeviceType}",
                                characteristic.Uuid, deviceType);
                        }
                    }

                    // 如果沒有找到匹配的特徵值，且是體溫計，嘗試訂閱所有支持通知的特徵值
                    if (deviceType == DeviceType.Thermometer && subscriptionInfo.Characteristics.Count == 0)
                    {
                        _logger.LogWarning("No standard temperature characteristics found for thermometer device {DeviceId}. " +
                            "Attempting to subscribe to all notification-capable characteristics in service {ServiceUuid}",
                            device.BluetoothAddress, service.Uuid);

                        foreach (var characteristic in characteristicsResult.Characteristics)
                        {
                            var hasNotify = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);
                            var hasIndicate = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate);

                            if (hasNotify || hasIndicate)
                            {
                                _logger.LogInformation("Attempting fallback subscription to characteristic {CharacteristicUuid}",
                                    characteristic.Uuid);

                                var subscribed = await SubscribeToCharacteristicWithRetryAsync(characteristic);
                                if (subscribed)
                                {
                                    subscriptionInfo.Characteristics.Add(characteristic);
                                    _logger.LogInformation("Successfully subscribed to fallback characteristic {CharacteristicId} on device {DeviceId}",
                                        characteristic.Uuid, device.BluetoothAddress);
                                }
                            }
                        }
                    }
                }

                // 存儲訂閱信息
                _deviceSubscriptions.AddOrUpdate(device.BluetoothAddress, subscriptionInfo,
                    (key, oldValue) => subscriptionInfo);

                _logger.LogInformation("Completed characteristic subscription for device {DeviceId}. Subscribed to {Count} characteristics",
                    device.BluetoothAddress, subscriptionInfo.Characteristics.Count);

                if (subscriptionInfo.Characteristics.Count == 0)
                {
                    _logger.LogWarning("No characteristics were subscribed for device {DeviceId} of type {DeviceType}. " +
                        "This may indicate the device doesn't support the expected characteristics or has different UUIDs.",
                        device.BluetoothAddress, deviceType);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subscribing to characteristics for device: {DeviceId}", device.BluetoothAddress);
            }
        }

        /// <summary>
        /// 檢查是否應該訂閱特定特徵值
        /// </summary>
        private Task<bool> ShouldSubscribeToCharacteristic(GattCharacteristic characteristic, DeviceType deviceType)
        {
            try
            {
                // 檢查特徵值是否支持通知或指示
                var hasNotify = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);
                var hasIndicate = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate);
                
                if (!hasNotify && !hasIndicate)
                {
                    _logger.LogDebug("Characteristic {CharacteristicUuid} does not support notifications or indications", 
                        characteristic.Uuid);
                    return Task.FromResult(false);
                }

                _logger.LogDebug("Characteristic {CharacteristicUuid} supports: Notify={Notify}, Indicate={Indicate}",
                    characteristic.Uuid, hasNotify, hasIndicate);

                // 根據設備類型和特徵值UUID決定是否訂閱
                var characteristicUuid = characteristic.Uuid;

                var shouldSubscribe = deviceType switch
                {
                    DeviceType.BloodPressureMonitor => characteristicUuid == BloodPressureMeasurementUuid,
                    DeviceType.Thermometer => IsTemperatureCharacteristic(characteristicUuid),
                    _ => false
                };

                if (shouldSubscribe)
                {
                    _logger.LogInformation("Will subscribe to characteristic {CharacteristicUuid} for device type {DeviceType}",
                        characteristicUuid, deviceType);
                }
                else
                {
                    // 如果是體溫計但UUID不匹配，記錄所有找到的特徵值以便調試
                    if (deviceType == DeviceType.Thermometer)
                    {
                        _logger.LogDebug("Thermometer characteristic {CharacteristicUuid} does not match any known temperature UUIDs",
                            characteristicUuid);
                    }
                }

                return Task.FromResult(shouldSubscribe);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if should subscribe to characteristic {CharacteristicId}",
                    characteristic.Uuid);
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// 檢查UUID是否為體溫測量特徵值
        /// </summary>
        private bool IsTemperatureCharacteristic(Guid uuid)
        {
            foreach (var tempUuid in TemperatureCharacteristicUuids)
            {
                if (uuid == tempUuid)
                {
                    _logger.LogDebug("Matched temperature characteristic UUID: {Uuid}", uuid);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 訂閱特徵值通知（帶重試機制）
        /// </summary>
        private async Task<bool> SubscribeToCharacteristicWithRetryAsync(GattCharacteristic characteristic)
        {
            const int maxRetries = 3;
            const int retryDelayMs = 1000;
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("Attempting to subscribe to characteristic {CharacteristicUuid} (attempt {Attempt}/{MaxRetries})", 
                        characteristic.Uuid, attempt, maxRetries);

                    // 檢查特徵值支持的通知類型
                    var hasNotify = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify);
                    var hasIndicate = characteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Indicate);

                    GattClientCharacteristicConfigurationDescriptorValue descriptorValue;
                    
                    if (hasIndicate)
                    {
                        // 優先使用 Indicate，因為它更可靠
                        descriptorValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;
                        _logger.LogDebug("Using Indicate for characteristic {CharacteristicUuid}", characteristic.Uuid);
                    }
                    else if (hasNotify)
                    {
                        descriptorValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                        _logger.LogDebug("Using Notify for characteristic {CharacteristicUuid}", characteristic.Uuid);
                    }
                    else
                    {
                        _logger.LogError("Characteristic {CharacteristicUuid} does not support Notify or Indicate", characteristic.Uuid);
                        return false;
                    }

                    // Windows平台特殊處理：檢查是否已經訂閱
                    try
                    {
                        var currentDescriptor = await characteristic.ReadClientCharacteristicConfigurationDescriptorAsync();
                        if (currentDescriptor.Status == GattCommunicationStatus.Success)
                        {
                            if (currentDescriptor.ClientCharacteristicConfigurationDescriptor != GattClientCharacteristicConfigurationDescriptorValue.None)
                            {
                                _logger.LogWarning("Characteristic {CharacteristicUuid} already has active subscription, clearing first", 
                                    characteristic.Uuid);
                                
                                // 先清除現有訂閱
                                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                    GattClientCharacteristicConfigurationDescriptorValue.None);
                                await Task.Delay(500); // 等待清除完成
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Could not read current descriptor value for characteristic {CharacteristicUuid}", 
                            characteristic.Uuid);
                    }

                    // 設置通知
                    var status = await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(descriptorValue);

                    if (status == GattCommunicationStatus.Success)
                    {
                        // 註冊數據接收事件處理器
                        characteristic.ValueChanged += OnCharacteristicValueChanged;

                        _logger.LogInformation("Successfully enabled {NotificationType} for characteristic {CharacteristicUuid}",
                            descriptorValue, characteristic.Uuid);

                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to enable notifications for characteristic {CharacteristicId} (attempt {Attempt}/{MaxRetries}): {Status}",
                            characteristic.Uuid, attempt, maxRetries, status);
                        
                        // 特殊錯誤處理
                        if (status == GattCommunicationStatus.AccessDenied)
                        {
                            _logger.LogWarning("AccessDenied error - device may have stale connection state");
                            if (attempt < maxRetries)
                            {
                                // 對於AccessDenied錯誤，等待更長時間
                                await Task.Delay(retryDelayMs * 2);
                            }
                        }
                        else if (status == GattCommunicationStatus.Unreachable)
                        {
                            _logger.LogWarning("Unreachable error - device connection may be unstable");
                            if (attempt < maxRetries)
                            {
                                await Task.Delay(retryDelayMs);
                            }
                        }
                        else if (attempt < maxRetries)
                        {
                            await Task.Delay(retryDelayMs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error subscribing to characteristic {CharacteristicId} (attempt {Attempt}/{MaxRetries})", 
                        characteristic.Uuid, attempt, maxRetries);
                    
                    if (attempt < maxRetries)
                    {
                        await Task.Delay(retryDelayMs);
                    }
                }
            }
            
            _logger.LogError("Failed to subscribe to characteristic {CharacteristicId} after {MaxRetries} attempts", 
                characteristic.Uuid, maxRetries);
            return false;
        }

        /// <summary>
        /// 訂閱特徵值通知
        /// </summary>
        private async Task<bool> SubscribeToCharacteristicAsync(GattCharacteristic characteristic)
        {
            return await SubscribeToCharacteristicWithRetryAsync(characteristic);
        }

        /// <summary>
        /// 特徵值數據變化事件處理器
        /// </summary>
        private async void OnCharacteristicValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            try
            {
                _logger.LogDebug("Characteristic value changed: {CharacteristicId}, Data length: {Length}",
                    sender.Uuid, args.CharacteristicValue.Length);

                // 讀取原始數據
                var rawData = new byte[args.CharacteristicValue.Length];
                using (var dataReader = DataReader.FromBuffer(args.CharacteristicValue))
                {
                    dataReader.ReadBytes(rawData);
                }

                // 處理數據
                await _dataProcessor.ProcessDataAsync(sender, rawData);

                // Windows平台優化：重置連接超時計時器
                // 成功接收數據表示連接仍然活躍
                var deviceId = sender.Service.Device.BluetoothAddress;
                if (_deviceSubscriptions.TryGetValue(deviceId, out var subscriptionInfo))
                {
                    subscriptionInfo.LastDataReceived = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing characteristic value change for {CharacteristicId}", sender.Uuid);
            }
        }

        /// <summary>
        /// 清理所有訂閱
        /// </summary>
        private async Task CleanupAllSubscriptionsAsync()
        {
            try
            {
                var cleanupTasks = new List<Task>();

                foreach (var deviceId in _deviceSubscriptions.Keys.ToList())
                {
                    cleanupTasks.Add(CleanupDeviceSubscriptionAsync(deviceId));
                }

                await Task.WhenAll(cleanupTasks);
                _deviceSubscriptions.Clear();
                _logger.LogInformation("Cleaned up all device subscriptions");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up subscriptions");
            }
        }

        /// <summary>
        /// 清理特定設備的訂閱
        /// </summary>
        private async Task CleanupDeviceSubscriptionAsync(ulong deviceId)
        {
            try
            {
                if (_deviceSubscriptions.TryGetValue(deviceId, out var subscriptionInfo))
                {
                    // 清理特徵值訂閱，但保留設備信息以便重連
                    var characteristicsToCleanup = subscriptionInfo.Characteristics.ToList();
                    subscriptionInfo.Characteristics.Clear();
                    
                    foreach (var characteristic in characteristicsToCleanup)
                    {
                        try
                        {
                            // 取消訂閱事件
                            characteristic.ValueChanged -= OnCharacteristicValueChanged;

                            // 嘗試禁用通知（可能會失敗，因為連接已斷開）
                            try
                            {
                                await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                    GattClientCharacteristicConfigurationDescriptorValue.None);
                            }
                            catch
                            {
                                // 忽略禁用通知的錯誤，因為設備可能已經斷開連接
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error cleaning up characteristic {CharacteristicId} for device {DeviceId}",
                                characteristic.Uuid, deviceId);
                        }
                    }

                    _logger.LogInformation("Cleaned up subscriptions for device: {DeviceId}", deviceId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up device subscription: {DeviceId}", deviceId);
            }
        }

        /// <summary>
        /// 釋放資源
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                // 取消註冊事件處理器
                _pairingManager.DeviceDiscovered -= OnDeviceDiscovered;
                _connectionManager.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _dataProcessor.DataProcessed -= OnDataProcessed;

                // 如果服務正在運行，進行清理
                if (_isStarted)
                {
                    try
                    {
                        // 使用內部清理邏輯，避免semaphore問題
                        var cleanupTask = StopInternalAsync();
                        if (!cleanupTask.Wait(2000)) // 最多等待2秒
                        {
                            _logger.LogWarning("Internal cleanup did not complete within timeout during disposal");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error during internal cleanup in disposal");
                    }
                }

                // 釋放資源
                _cancellationTokenSource?.Dispose();
                _operationSemaphore?.Dispose();

                _logger.LogInformation("BLEReceiverService disposed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing BLEReceiverService");
            }
        }
    }

    /// <summary>
    /// 設備訂閱信息
    /// </summary>
    internal class DeviceSubscriptionInfo
    {
        public BluetoothLEDevice Device { get; set; } = null!;
        public DeviceType DeviceType { get; set; }
        public List<GattCharacteristic> Characteristics { get; set; } = new();
        public DateTime LastDataReceived { get; set; } = DateTime.UtcNow;
    }
}