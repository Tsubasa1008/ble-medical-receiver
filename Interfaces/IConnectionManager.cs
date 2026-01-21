using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Interfaces
{
    /// <summary>
    /// BLE連接管理器接口
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// 連接到設備
        /// </summary>
        /// <param name="device">要連接的設備</param>
        /// <returns>連接是否成功</returns>
        Task<bool> ConnectAsync(BluetoothLEDevice device);

        /// <summary>
        /// 斷開設備連接
        /// </summary>
        /// <param name="deviceId">設備ID</param>
        /// <returns>斷開任務</returns>
        Task DisconnectAsync(ulong deviceId);

        /// <summary>
        /// 重新連接設備
        /// </summary>
        /// <param name="deviceId">設備ID</param>
        /// <returns>重連是否成功</returns>
        Task<bool> ReconnectAsync(ulong deviceId);

        /// <summary>
        /// 獲取連接狀態
        /// </summary>
        /// <param name="deviceId">設備ID</param>
        /// <returns>連接狀態</returns>
        ConnectionStatus GetConnectionStatus(ulong deviceId);

        /// <summary>
        /// 連接狀態變化事件
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
    }
}