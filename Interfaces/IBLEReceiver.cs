using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Interfaces
{
    /// <summary>
    /// BLE接收器核心接口
    /// </summary>
    public interface IBLEReceiver
    {
        /// <summary>
        /// 啟動BLE接收器
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>啟動任務</returns>
        Task StartAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 停止BLE接收器
        /// </summary>
        /// <returns>停止任務</returns>
        Task StopAsync();

        /// <summary>
        /// 手動斷開所有設備連接
        /// </summary>
        /// <returns>斷開任務</returns>
        Task DisconnectAllDevicesAsync();

        /// <summary>
        /// 獲取當前連接的設備信息
        /// </summary>
        /// <returns>設備信息列表</returns>
        List<(ulong DeviceId, string DeviceName, DeviceType DeviceType, int ActiveSubscriptions)> GetConnectedDevices();

        /// <summary>
        /// 數據接收事件
        /// </summary>
        event EventHandler<DeviceDataReceivedEventArgs> DataReceived;

        /// <summary>
        /// 連接狀態變化事件
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
    }
}