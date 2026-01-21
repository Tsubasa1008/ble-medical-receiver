using System;
using Windows.Devices.Bluetooth;

namespace BLEDataReceiver.Models
{
    /// <summary>
    /// 設備數據接收事件參數
    /// </summary>
    public class DeviceDataReceivedEventArgs : EventArgs
    {
        public MedicalData Data { get; }
        public string DeviceId { get; }

        public DeviceDataReceivedEventArgs(MedicalData data, string deviceId)
        {
            Data = data;
            DeviceId = deviceId;
        }
    }

    /// <summary>
    /// 連接狀態變化事件參數
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public ConnectionStatus Status { get; }
        public string? ErrorMessage { get; }

        public ConnectionStatusChangedEventArgs(string deviceId, ConnectionStatus status, string? errorMessage = null)
        {
            DeviceId = deviceId;
            Status = status;
            ErrorMessage = errorMessage;
        }
    }

    /// <summary>
    /// 設備發現事件參數
    /// </summary>
    public class DeviceDiscoveredEventArgs : EventArgs
    {
        public BluetoothLEDevice Device { get; }
        public DeviceType DeviceType { get; }

        public DeviceDiscoveredEventArgs(BluetoothLEDevice device, DeviceType deviceType)
        {
            Device = device;
            DeviceType = deviceType;
        }
    }

    /// <summary>
    /// 數據處理事件參數
    /// </summary>
    public class DataProcessedEventArgs : EventArgs
    {
        public MedicalData ProcessedData { get; }
        public bool IsValid { get; }
        public string? ErrorMessage { get; }

        public DataProcessedEventArgs(MedicalData processedData, bool isValid, string? errorMessage = null)
        {
            ProcessedData = processedData;
            IsValid = isValid;
            ErrorMessage = errorMessage;
        }
    }
}