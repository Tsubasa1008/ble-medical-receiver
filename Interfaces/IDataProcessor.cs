using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Interfaces
{
    /// <summary>
    /// 數據處理器接口
    /// </summary>
    public interface IDataProcessor
    {
        /// <summary>
        /// 處理接收到的數據
        /// </summary>
        /// <param name="characteristic">GATT特徵值</param>
        /// <param name="rawData">原始數據</param>
        /// <returns>處理後的醫療數據</returns>
        Task<MedicalData> ProcessDataAsync(GattCharacteristic characteristic, byte[] rawData);

        /// <summary>
        /// 驗證數據有效性
        /// </summary>
        /// <param name="data">要驗證的數據</param>
        /// <returns>數據是否有效</returns>
        bool ValidateData(MedicalData data);

        /// <summary>
        /// 數據處理事件
        /// </summary>
        event EventHandler<DataProcessedEventArgs> DataProcessed;
    }
}