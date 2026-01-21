using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using BLEDataReceiver.Interfaces;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Services
{
    /// <summary>
    /// 連接管理器服務實現
    /// 負責管理BLE設備連接狀態、自動重連機制和連接超時處理
    /// </summary>
    public class ConnectionManagerService : IConnectionManager, IDisposable
    {
        private readonly ILogger<ConnectionManagerService> _logger;
        private readonly ConcurrentDictionary<ulong, DeviceConnectionInfo> _connections;
        private readonly ConcurrentDictionary<ulong, CancellationTokenSource> _connectionCancellationTokens;
        private readonly SemaphoreSlim _connectionSemaphore;
        private bool _disposed = false;

        // 重連策略配置
        private readonly int[] _retryIntervals = { 1000, 2000, 4000 }; // 指數退避：1s, 2s, 4s
        private const int MaxRetries = 3;
        private const int ConnectionTimeoutMs = 30000; // 30秒連接超時

        public ConnectionManagerService(ILogger<ConnectionManagerService> logger)
        {
            _logger = logger;
            _connections = new ConcurrentDictionary<ulong, DeviceConnectionInfo>();
            _connectionCancellationTokens = new ConcurrentDictionary<ulong, CancellationTokenSource>();
            _connectionSemaphore = new SemaphoreSlim(5, 5); // 最多同時處理5個連接
        }

        public event EventHandler<ConnectionStatusChangedEventArgs>? ConnectionStatusChanged;

        /// <summary>
        /// 連接到BLE設備
        /// </summary>
        public async Task<bool> ConnectAsync(BluetoothLEDevice device)
        {
            if (device == null)
            {
                _logger.LogError("Device is null, cannot connect");
                return false;
            }

            var deviceId = device.BluetoothAddress;
            _logger.LogInformation("Attempting to connect to device: {DeviceId} ({DeviceName})", 
                deviceId, device.Name ?? "Unknown");

            // 檢查是否已經連接
            if (_connections.TryGetValue(deviceId, out var existingConnection) && 
                existingConnection.Status == ConnectionStatus.Connected)
            {
                _logger.LogInformation("Device {DeviceId} is already connected", deviceId);
                return true;
            }

            await _connectionSemaphore.WaitAsync();
            try
            {
                // 創建或更新連接信息
                var connectionInfo = new DeviceConnectionInfo
                {
                    Device = device,
                    Status = ConnectionStatus.Connecting,
                    LastConnectionAttempt = DateTime.UtcNow,
                    RetryCount = 0
                };

                _connections.AddOrUpdate(deviceId, connectionInfo, (key, oldValue) => connectionInfo);
                OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Connecting);

                // 創建連接取消令牌
                var cts = new CancellationTokenSource(ConnectionTimeoutMs);
                _connectionCancellationTokens.AddOrUpdate(deviceId, cts, (key, oldValue) =>
                {
                    oldValue?.Cancel();
                    oldValue?.Dispose();
                    return cts;
                });

                try
                {
                    // 嘗試連接到設備
                    var result = await ConnectToDeviceInternalAsync(device, cts.Token);
                    
                    if (result)
                    {
                        connectionInfo.Status = ConnectionStatus.Connected;
                        connectionInfo.ConnectedAt = DateTime.UtcNow;
                        connectionInfo.RetryCount = 0;
                        
                        _logger.LogInformation("Successfully connected to device: {DeviceId}", deviceId);
                        OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Connected);
                        
                        // 開始監控連接狀態
                        _ = Task.Run(() => MonitorConnectionAsync(deviceId, cts.Token));
                        
                        return true;
                    }
                    else
                    {
                        connectionInfo.Status = ConnectionStatus.Failed;
                        _logger.LogWarning("Failed to connect to device: {DeviceId}", deviceId);
                        OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Failed, "Connection failed");
                        return false;
                    }
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                    connectionInfo.Status = ConnectionStatus.Failed;
                    _logger.LogWarning("Connection to device {DeviceId} timed out after {TimeoutMs}ms", 
                        deviceId, ConnectionTimeoutMs);
                    OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Failed, "Connection timeout");
                    return false;
                }
                catch (Exception ex)
                {
                    connectionInfo.Status = ConnectionStatus.Failed;
                    _logger.LogError(ex, "Error connecting to device: {DeviceId}", deviceId);
                    OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Failed, ex.Message);
                    return false;
                }
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }

        /// <summary>
        /// 斷開設備連接
        /// </summary>
        public async Task DisconnectAsync(ulong deviceId)
        {
            _logger.LogInformation("Disconnecting from device: {DeviceId}", deviceId);

            if (_connections.TryGetValue(deviceId, out var connectionInfo))
            {
                // 取消連接監控
                if (_connectionCancellationTokens.TryRemove(deviceId, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }

                // Windows平台激進斷開策略
                await ForceDisconnectDeviceAsync(connectionInfo);

                // 更新連接狀態
                connectionInfo.Status = ConnectionStatus.Disconnected;
                connectionInfo.DisconnectedAt = DateTime.UtcNow;

                _logger.LogInformation("Successfully disconnected from device: {DeviceId}", deviceId);
                OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Disconnected);
            }
            else
            {
                _logger.LogWarning("Device {DeviceId} not found in connection list", deviceId);
            }
        }

        /// <summary>
        /// 強制斷開設備連接（Windows平台特殊處理）
        /// </summary>
        private async Task ForceDisconnectDeviceAsync(DeviceConnectionInfo connectionInfo)
        {
            try
            {
                var device = connectionInfo.Device;
                _logger.LogDebug("Force disconnecting device {DeviceId}", device.BluetoothAddress);

                // 1. 清理所有GATT特徵值訂閱
                if (connectionInfo.GattServices != null)
                {
                    foreach (var service in connectionInfo.GattServices)
                    {
                        try
                        {
                            var characteristicsResult = await service.GetCharacteristicsAsync();
                            if (characteristicsResult.Status == GattCommunicationStatus.Success)
                            {
                                foreach (var characteristic in characteristicsResult.Characteristics)
                                {
                                    try
                                    {
                                        // 強制禁用所有通知
                                        await characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                            GattClientCharacteristicConfigurationDescriptorValue.None);
                                    }
                                    catch
                                    {
                                        // 忽略錯誤，繼續清理
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // 忽略錯誤，繼續清理
                        }
                    }
                }

                // 2. 清理GATT服務
                await CleanupGattServicesAsync(connectionInfo);

                // 3. Windows特殊處理：嘗試多次獲取服務以觸發系統清理
                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        var gattResult = await device.GetGattServicesAsync();
                        if (gattResult.Status == GattCommunicationStatus.Success)
                        {
                            foreach (var service in gattResult.Services)
                            {
                                service?.Dispose();
                            }
                        }
                        await Task.Delay(200);
                    }
                    catch
                    {
                        // 忽略錯誤
                    }
                }

                // 4. 嘗試釋放設備對象
                try
                {
                    if (device != null)
                    {
                        device.Dispose();
                    }
                }
                catch
                {
                    // 忽略錯誤
                }

                _logger.LogDebug("Force disconnect completed for device {DeviceId}", device?.BluetoothAddress ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during force disconnect for device {DeviceId}", 
                    connectionInfo.Device.BluetoothAddress);
            }
        }

        /// <summary>
        /// 重新連接設備
        /// </summary>
        public async Task<bool> ReconnectAsync(ulong deviceId)
        {
            _logger.LogInformation("Attempting to reconnect to device: {DeviceId}", deviceId);

            if (!_connections.TryGetValue(deviceId, out var connectionInfo))
            {
                _logger.LogError("Device {DeviceId} not found in connection list, cannot reconnect", deviceId);
                return false;
            }

            // 先斷開現有連接
            await DisconnectAsync(deviceId);

            // 等待短暫時間後重新連接
            await Task.Delay(500);

            return await ConnectAsync(connectionInfo.Device);
        }

        /// <summary>
        /// 獲取連接狀態
        /// </summary>
        public ConnectionStatus GetConnectionStatus(ulong deviceId)
        {
            if (_connections.TryGetValue(deviceId, out var connectionInfo))
            {
                return connectionInfo.Status;
            }
            return ConnectionStatus.Disconnected;
        }

        /// <summary>
        /// 內部連接方法
        /// </summary>
        private async Task<bool> ConnectToDeviceInternalAsync(BluetoothLEDevice device, CancellationToken cancellationToken)
        {
            try
            {
                // 獲取GATT服務
                var gattResult = await device.GetGattServicesAsync();
                
                if (gattResult.Status != GattCommunicationStatus.Success)
                {
                    _logger.LogError("Failed to get GATT services for device {DeviceId}: {Status}", 
                        device.BluetoothAddress, gattResult.Status);
                    return false;
                }

                if (gattResult.Services.Count == 0)
                {
                    _logger.LogWarning("No GATT services found for device {DeviceId}", device.BluetoothAddress);
                    return false;
                }

                // 存儲GATT服務以供後續使用
                var connectionInfo = _connections[device.BluetoothAddress];
                connectionInfo.GattServices = gattResult.Services;

                _logger.LogInformation("Successfully retrieved {ServiceCount} GATT services for device {DeviceId}", 
                    gattResult.Services.Count, device.BluetoothAddress);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during internal connection to device {DeviceId}", device.BluetoothAddress);
                return false;
            }
        }

        /// <summary>
        /// 監控連接狀態
        /// </summary>
        private async Task MonitorConnectionAsync(ulong deviceId, CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!_connections.TryGetValue(deviceId, out var connectionInfo))
                    {
                        break;
                    }

                    // Windows平台主動連接檢測：通過嘗試讀取服務來檢測連接狀態
                    var isConnected = await CheckConnectionActivelyAsync(connectionInfo);
                    
                    if (!isConnected)
                    {
                        _logger.LogWarning("Connection lost to device {DeviceId}, attempting reconnection", deviceId);
                        
                        // 更新狀態為重連中
                        connectionInfo.Status = ConnectionStatus.Reconnecting;
                        OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Reconnecting);

                        // 嘗試自動重連
                        var reconnected = await AttemptAutoReconnectAsync(deviceId);
                        
                        if (!reconnected)
                        {
                            connectionInfo.Status = ConnectionStatus.Failed;
                            OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Failed, 
                                "Auto-reconnection failed after maximum retries");
                            break;
                        }
                    }

                    // Windows平台：更頻繁的檢查（每2秒而不是5秒）
                    await Task.Delay(2000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常取消，不需要記錄錯誤
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error monitoring connection for device {DeviceId}", deviceId);
            }
        }

        /// <summary>
        /// 主動檢測連接狀態（Windows平台優化）
        /// </summary>
        private async Task<bool> CheckConnectionActivelyAsync(DeviceConnectionInfo connectionInfo)
        {
            try
            {
                var device = connectionInfo.Device;
                
                // 方法1: 檢查設備的系統連接狀態
                if (device.ConnectionStatus != BluetoothConnectionStatus.Connected)
                {
                    _logger.LogDebug("Device {DeviceId} system connection status: {Status}", 
                        device.BluetoothAddress, device.ConnectionStatus);
                    return false;
                }

                // 方法2: 嘗試快速獲取GATT服務來測試連接
                try
                {
                    using var cts = new CancellationTokenSource(3000); // 3秒超時
                    var gattResult = await device.GetGattServicesAsync().AsTask(cts.Token);
                    
                    if (gattResult.Status != GattCommunicationStatus.Success)
                    {
                        _logger.LogDebug("Device {DeviceId} GATT services check failed: {Status}", 
                            device.BluetoothAddress, gattResult.Status);
                        return false;
                    }

                    // 清理獲取的服務
                    foreach (var service in gattResult.Services)
                    {
                        service?.Dispose();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Device {DeviceId} GATT services check timed out", device.BluetoothAddress);
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Device {DeviceId} GATT services check failed with exception", device.BluetoothAddress);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Error during active connection check for device {DeviceId}", 
                    connectionInfo.Device.BluetoothAddress);
                return false;
            }
        }

        /// <summary>
        /// 自動重連機制（指數退避策略）
        /// </summary>
        private async Task<bool> AttemptAutoReconnectAsync(ulong deviceId)
        {
            if (!_connections.TryGetValue(deviceId, out var connectionInfo))
            {
                return false;
            }

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    _logger.LogInformation("Reconnection attempt {Attempt}/{MaxRetries} for device {DeviceId}", 
                        attempt + 1, MaxRetries, deviceId);

                    // 指數退避延遲
                    if (attempt > 0)
                    {
                        await Task.Delay(_retryIntervals[Math.Min(attempt - 1, _retryIntervals.Length - 1)]);
                    }

                    // 嘗試重新連接
                    var result = await ConnectToDeviceInternalAsync(connectionInfo.Device, CancellationToken.None);
                    
                    if (result)
                    {
                        connectionInfo.Status = ConnectionStatus.Connected;
                        connectionInfo.ConnectedAt = DateTime.UtcNow;
                        connectionInfo.RetryCount = 0;
                        
                        _logger.LogInformation("Successfully reconnected to device {DeviceId} on attempt {Attempt}", 
                            deviceId, attempt + 1);
                        OnConnectionStatusChanged(deviceId.ToString(), ConnectionStatus.Connected);
                        
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Reconnection attempt {Attempt} failed for device {DeviceId}", 
                        attempt + 1, deviceId);
                }

                connectionInfo.RetryCount = attempt + 1;
            }

            _logger.LogError("Failed to reconnect to device {DeviceId} after {MaxRetries} attempts", 
                deviceId, MaxRetries);
            return false;
        }

        /// <summary>
        /// 清理GATT服務
        /// </summary>
        private Task CleanupGattServicesAsync(DeviceConnectionInfo connectionInfo)
        {
            try
            {
                if (connectionInfo.GattServices != null)
                {
                    foreach (var service in connectionInfo.GattServices)
                    {
                        service?.Dispose();
                    }
                    connectionInfo.GattServices = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up GATT services for device {DeviceId}", 
                    connectionInfo.Device.BluetoothAddress);
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// 觸發連接狀態變化事件
        /// </summary>
        private void OnConnectionStatusChanged(string deviceId, ConnectionStatus status, string? errorMessage = null)
        {
            try
            {
                ConnectionStatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(deviceId, status, errorMessage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking ConnectionStatusChanged event");
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

            // 取消所有連接操作
            foreach (var cts in _connectionCancellationTokens.Values)
            {
                try
                {
                    cts.Cancel();
                    cts.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing cancellation token");
                }
            }

            _connectionCancellationTokens.Clear();

            // 清理所有連接
            foreach (var connection in _connections.Values)
            {
                try
                {
                    CleanupGattServicesAsync(connection).Wait(1000);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error cleaning up connection during disposal");
                }
            }

            _connections.Clear();
            _connectionSemaphore?.Dispose();

            _logger.LogInformation("ConnectionManagerService disposed");
        }
    }

    /// <summary>
    /// 設備連接信息
    /// </summary>
    internal class DeviceConnectionInfo
    {
        public BluetoothLEDevice Device { get; set; } = null!;
        public ConnectionStatus Status { get; set; }
        public DateTime LastConnectionAttempt { get; set; }
        public DateTime? ConnectedAt { get; set; }
        public DateTime? DisconnectedAt { get; set; }
        public int RetryCount { get; set; }
        public IReadOnlyList<GattDeviceService>? GattServices { get; set; }
    }
}