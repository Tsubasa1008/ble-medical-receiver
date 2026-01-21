using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Interfaces
{
    /// <summary>
    /// BLE配對管理器接口
    /// </summary>
    public interface IPairingManager
    {
        /// <summary>
        /// 啟動廣播監聽器
        /// </summary>
        /// <returns>啟動任務</returns>
        Task StartAdvertisementWatcherAsync();

        /// <summary>
        /// 停止廣播監聽器
        /// </summary>
        /// <returns>停止任務</returns>
        Task StopAdvertisementWatcherAsync();

        /// <summary>
        /// 配對設備
        /// </summary>
        /// <param name="device">要配對的設備</param>
        /// <returns>配對是否成功</returns>
        Task<bool> PairDeviceAsync(BluetoothLEDevice device);

        /// <summary>
        /// 檢查是否為目標醫療設備
        /// </summary>
        /// <param name="advertisement">廣播信息</param>
        /// <returns>是否為目標設備</returns>
        bool IsTargetDevice(BluetoothLEAdvertisement advertisement);

        /// <summary>
        /// 設備發現事件
        /// </summary>
        event EventHandler<DeviceDiscoveredEventArgs> DeviceDiscovered;
    }
}