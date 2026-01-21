using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using BLEDataReceiver.Interfaces;
using BLEDataReceiver.Models;
using BLEDataReceiver.Parsers;

namespace BLEDataReceiver.Services
{
    /// <summary>
    /// 數據處理器服務實現
    /// 負責處理和驗證來自BLE設備的醫療數據
    /// </summary>
    public class DataProcessorService : IDataProcessor
    {
        private readonly IEEE11073Parser _parser;
        private readonly ILogger<DataProcessorService> _logger;

        /// <summary>
        /// 數據處理事件
        /// </summary>
        public event EventHandler<DataProcessedEventArgs>? DataProcessed;

        public DataProcessorService(ILogger<DataProcessorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _parser = new IEEE11073Parser();
        }

        /// <summary>
        /// 處理接收到的數據
        /// </summary>
        /// <param name="characteristic">GATT特徵值</param>
        /// <param name="rawData">原始數據</param>
        /// <returns>處理後的醫療數據</returns>
        public async Task<MedicalData> ProcessDataAsync(GattCharacteristic characteristic, byte[] rawData)
        {
            if (characteristic == null)
                throw new ArgumentNullException(nameof(characteristic));
            
            if (rawData == null || rawData.Length == 0)
                throw new ArgumentException("原始數據不能為空", nameof(rawData));

            try
            {
                _logger.LogDebug("開始處理數據，長度: {Length} 字節", rawData.Length);

                // 根據特徵值UUID確定設備類型和數據格式
                var deviceType = DetermineDeviceType(characteristic.Uuid);
                var deviceId = characteristic.Service.Device.BluetoothAddress.ToString("X");

                MedicalData processedData;

                // 根據設備類型解析數據
                switch (deviceType)
                {
                    case DeviceType.BloodPressureMonitor:
                        processedData = await ProcessBloodPressureDataAsync(rawData, deviceId);
                        break;
                    
                    case DeviceType.Thermometer:
                        processedData = await ProcessTemperatureDataAsync(rawData, deviceId);
                        break;
                    
                    default:
                        throw new NotSupportedException($"不支持的設備類型: {deviceType}");
                }

                // 驗證處理後的數據
                var isValid = ValidateData(processedData);
                processedData.IsValid = isValid;

                _logger.LogInformation("數據處理完成，設備: {DeviceId}, 類型: {DeviceType}, 有效: {IsValid}", 
                    deviceId, deviceType, isValid);

                // 觸發數據處理事件
                OnDataProcessed(new DataProcessedEventArgs(processedData, isValid));

                return processedData;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "數據處理失敗，原始數據長度: {Length}", rawData?.Length ?? 0);
                
                // 創建錯誤數據對象
                var errorData = CreateErrorData(characteristic, ex.Message);
                OnDataProcessed(new DataProcessedEventArgs(errorData, false, ex.Message));
                
                throw;
            }
        }

        /// <summary>
        /// 驗證數據有效性
        /// </summary>
        /// <param name="data">要驗證的數據</param>
        /// <returns>數據是否有效</returns>
        public bool ValidateData(MedicalData data)
        {
            if (data == null)
            {
                _logger.LogWarning("數據驗證失敗：數據為空");
                return false;
            }

            try
            {
                // 使用數據模型內建的驗證邏輯
                var isValid = data.ValidateData();
                
                if (!isValid)
                {
                    _logger.LogWarning("數據驗證失敗，設備: {DeviceId}, 類型: {DeviceType}", 
                        data.DeviceId, data.DeviceType);
                }
                else
                {
                    _logger.LogDebug("數據驗證成功，設備: {DeviceId}, 類型: {DeviceType}", 
                        data.DeviceId, data.DeviceType);
                }

                return isValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "數據驗證過程中發生異常，設備: {DeviceId}", data.DeviceId);
                return false;
            }
        }

        /// <summary>
        /// 處理血壓數據
        /// </summary>
        /// <param name="rawData">原始數據</param>
        /// <param name="deviceId">設備ID</param>
        /// <returns>處理後的血壓數據</returns>
        private async Task<BloodPressureData> ProcessBloodPressureDataAsync(byte[] rawData, string deviceId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 驗證數據格式
                    if (!_parser.ValidateDataFormat(rawData, DeviceType.BloodPressureMonitor))
                    {
                        throw new ArgumentException("血壓數據格式無效");
                    }

                    // 解析血壓數據
                    var bloodPressureData = _parser.ParseBloodPressure(rawData);
                    bloodPressureData.DeviceId = deviceId;

                    _logger.LogDebug("血壓數據解析成功 - 收縮壓: {Systolic} mmHg, 舒張壓: {Diastolic} mmHg, 心率: {HeartRate} bpm",
                        bloodPressureData.SystolicPressure, bloodPressureData.DiastolicPressure, bloodPressureData.HeartRate);

                    // 檢查是否在正常範圍內
                    if (!bloodPressureData.IsInNormalRange())
                    {
                        _logger.LogWarning("血壓數值超出正常範圍 - 收縮壓: {Systolic}, 舒張壓: {Diastolic}, 心率: {HeartRate}",
                            bloodPressureData.SystolicPressure, bloodPressureData.DiastolicPressure, bloodPressureData.HeartRate);
                    }

                    return bloodPressureData;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "血壓數據處理失敗");
                    throw;
                }
            });
        }

        /// <summary>
        /// 處理體溫數據
        /// </summary>
        /// <param name="rawData">原始數據</param>
        /// <param name="deviceId">設備ID</param>
        /// <returns>處理後的體溫數據</returns>
        private async Task<TemperatureData> ProcessTemperatureDataAsync(byte[] rawData, string deviceId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 記錄原始數據以便調試
                    var rawDataHex = BitConverter.ToString(rawData).Replace("-", " ");
                    _logger.LogInformation("收到體溫原始數據 - 設備: {DeviceId}, 長度: {Length}, 數據: {RawData}", 
                        deviceId, rawData.Length, rawDataHex);

                    // 驗證數據格式
                    if (!_parser.ValidateDataFormat(rawData, DeviceType.Thermometer))
                    {
                        throw new ArgumentException("體溫數據格式無效");
                    }

                    // 解析體溫數據
                    var temperatureData = _parser.ParseTemperature(rawData);
                    temperatureData.DeviceId = deviceId;

                    _logger.LogDebug("體溫數據解析成功 - 溫度: {Temperature}°{Unit}",
                        temperatureData.Temperature, temperatureData.Unit);

                    // 檢查是否在正常範圍內
                    if (!temperatureData.IsInNormalRange())
                    {
                        _logger.LogWarning("體溫數值超出正常範圍 - 溫度: {Temperature}°{Unit}",
                            temperatureData.Temperature, temperatureData.Unit);
                    }

                    return temperatureData;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "體溫數據處理失敗");
                    throw;
                }
            });
        }

        /// <summary>
        /// 根據特徵值UUID確定設備類型
        /// </summary>
        /// <param name="characteristicUuid">特徵值UUID</param>
        /// <returns>設備類型</returns>
        private DeviceType DetermineDeviceType(Guid characteristicUuid)
        {
            // 標準BLE特徵值UUID
            var uuidString = characteristicUuid.ToString().ToUpperInvariant();
            
            // 血壓測量特徵值 (Blood Pressure Measurement)
            if (uuidString.Contains("2A35"))
            {
                return DeviceType.BloodPressureMonitor;
            }
            
            // 體溫測量特徵值 (Temperature Measurement)
            if (uuidString.Contains("2A1C"))
            {
                return DeviceType.Thermometer;
            }

            // 如果無法從特徵值UUID確定，嘗試從服務UUID確定
            var serviceUuid = characteristicUuid.ToString("N")[..4]; // 取前4位作為服務UUID
            
            try
            {
                return _parser.DetectDeviceType(serviceUuid);
            }
            catch
            {
                _logger.LogWarning("無法確定設備類型，UUID: {UUID}", characteristicUuid);
                throw new NotSupportedException($"不支持的特徵值UUID: {characteristicUuid}");
            }
        }

        /// <summary>
        /// 創建錯誤數據對象
        /// </summary>
        /// <param name="characteristic">GATT特徵值</param>
        /// <param name="errorMessage">錯誤消息</param>
        /// <returns>錯誤數據對象</returns>
        private MedicalData CreateErrorData(GattCharacteristic characteristic, string errorMessage)
        {
            var deviceId = characteristic.Service.Device.BluetoothAddress.ToString("X");
            
            try
            {
                var deviceType = DetermineDeviceType(characteristic.Uuid);
                
                return deviceType switch
                {
                    DeviceType.BloodPressureMonitor => new BloodPressureData
                    {
                        DeviceId = deviceId,
                        DeviceType = deviceType,
                        Timestamp = DateTime.Now,
                        IsValid = false
                    },
                    DeviceType.Thermometer => new TemperatureData
                    {
                        DeviceId = deviceId,
                        DeviceType = deviceType,
                        Timestamp = DateTime.Now,
                        IsValid = false
                    },
                    _ => new BloodPressureData // 默認返回血壓數據作為錯誤對象
                    {
                        DeviceId = deviceId,
                        DeviceType = DeviceType.BloodPressureMonitor,
                        Timestamp = DateTime.Now,
                        IsValid = false
                    }
                };
            }
            catch
            {
                // 如果無法確定設備類型，返回默認的血壓數據對象
                return new BloodPressureData
                {
                    DeviceId = deviceId,
                    DeviceType = DeviceType.BloodPressureMonitor,
                    Timestamp = DateTime.Now,
                    IsValid = false
                };
            }
        }

        /// <summary>
        /// 觸發數據處理事件
        /// </summary>
        /// <param name="args">事件參數</param>
        protected virtual void OnDataProcessed(DataProcessedEventArgs args)
        {
            DataProcessed?.Invoke(this, args);
        }

        /// <summary>
        /// 格式化數據用於顯示
        /// </summary>
        /// <param name="data">醫療數據</param>
        /// <returns>格式化後的字符串</returns>
        public string FormatDataForDisplay(MedicalData data)
        {
            if (data == null)
                return "無效數據";

            var timestamp = data.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
            var validityIndicator = data.IsValid ? "✓" : "✗";
            var normalRangeIndicator = data.IsInNormalRange() ? "" : " ⚠️";

            return data switch
            {
                BloodPressureData bp => $"[{timestamp}] {validityIndicator} 血壓: {bp.SystolicPressure:F1}/{bp.DiastolicPressure:F1} mmHg, 心率: {bp.HeartRate} bpm{normalRangeIndicator}",
                TemperatureData temp => $"[{timestamp}] {validityIndicator} 體溫: {temp.Temperature:F1}°{temp.Unit}{normalRangeIndicator}",
                _ => $"[{timestamp}] {validityIndicator} 未知數據類型"
            };
        }
    }
}