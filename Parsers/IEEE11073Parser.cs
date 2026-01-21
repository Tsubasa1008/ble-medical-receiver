using System;
using BLEDataReceiver.Models;

namespace BLEDataReceiver.Parsers
{
    /// <summary>
    /// IEEE 11073標準醫療設備數據解析器
    /// 支持血壓計和體溫計的數據格式解析
    /// </summary>
    public class IEEE11073Parser
    {
        /// <summary>
        /// 解析血壓計數據 (IEEE 11073-10407標準)
        /// </summary>
        /// <param name="data">原始BLE數據</param>
        /// <returns>解析後的血壓數據</returns>
        /// <exception cref="ArgumentException">當數據格式無效時拋出</exception>
        public BloodPressureData ParseBloodPressure(byte[] data)
        {
            if (data == null || data.Length < 7)
                throw new ArgumentException("血壓數據長度不足，至少需要7個字節");

            try
            {
                // IEEE 11073-10407 血壓數據格式:
                // Byte 0: Flags
                // Byte 1-2: 收縮壓 (SFLOAT)
                // Byte 3-4: 舒張壓 (SFLOAT)
                // Byte 5-6: 平均動脈壓 (SFLOAT) - 可選
                // Byte 7-13: 時間戳 (可選)
                // Byte 14-15: 脈搏率 (SFLOAT) - 可選

                var flags = data[0];
                
                // 解析收縮壓和舒張壓
                var systolic = ParseSFloat(data, 1);
                var diastolic = ParseSFloat(data, 3);
                
                // 解析心率 (如果數據長度足夠)
                int heartRate = 0;
                if (data.Length >= 15)
                {
                    heartRate = (int)ParseSFloat(data, 13);
                }
                else if (data.Length >= 7)
                {
                    // 如果沒有完整的時間戳，嘗試從第5-6字節解析心率
                    heartRate = (int)ParseSFloat(data, 5);
                }

                var bloodPressureData = new BloodPressureData
                {
                    SystolicPressure = systolic,
                    DiastolicPressure = diastolic,
                    HeartRate = heartRate,
                    Timestamp = DateTime.Now,
                    DeviceType = DeviceType.BloodPressureMonitor
                };

                // 驗證解析後的數據
                bloodPressureData.IsValid = bloodPressureData.ValidateData();

                return bloodPressureData;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"血壓數據解析失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析體溫計數據 (IEEE 11073-10408標準)
        /// </summary>
        /// <param name="data">原始BLE數據</param>
        /// <returns>解析後的體溫數據</returns>
        /// <exception cref="ArgumentException">當數據格式無效時拋出</exception>
        public TemperatureData ParseTemperature(byte[] data)
        {
            if (data == null || data.Length < 5)
                throw new ArgumentException("體溫數據長度不足，至少需要5個字節");

            try
            {
                var flags = data[0];
                float temperature;
                
                // 嘗試不同的解析方法
                try
                {
                    // 方法1: 標準IEEE 11073 FLOAT格式 (32位)
                    temperature = ParseFloat(data, 1);
                    
                    // 如果結果是無效值，嘗試其他方法
                    if (float.IsInfinity(temperature) || float.IsNaN(temperature) || temperature <= 0 || temperature > 100)
                    {
                        throw new InvalidOperationException("IEEE FLOAT parsing failed");
                    }
                }
                catch
                {
                    // 方法2: 嘗試SFLOAT格式 (16位)
                    try
                    {
                        temperature = ParseSFloat(data, 1);
                        
                        if (float.IsInfinity(temperature) || float.IsNaN(temperature) || temperature <= 0 || temperature > 100)
                        {
                            throw new InvalidOperationException("SFLOAT parsing failed");
                        }
                    }
                    catch
                    {
                        // 方法3: 嘗試簡單的整數解析 (常見於某些設備)
                        // 假設溫度值存儲為 16位整數，單位為0.1度
                        if (data.Length >= 3)
                        {
                            var tempRaw = BitConverter.ToUInt16(data, 1);
                            temperature = tempRaw / 10.0f;
                            
                            if (temperature <= 0 || temperature > 100)
                            {
                                // 方法4: 嘗試不同的縮放因子
                                temperature = tempRaw / 100.0f;
                                
                                if (temperature <= 0 || temperature > 100)
                                {
                                    // 方法5: 嘗試單字節解析
                                    temperature = data[1] + (data[2] / 10.0f);
                                }
                            }
                        }
                        else
                        {
                            throw new ArgumentException("無法解析溫度數據 - 所有方法都失敗");
                        }
                    }
                }
                
                // 檢查溫度單位 (通常IEEE 11073使用攝氏度)
                var unit = TemperatureUnit.Celsius;

                var temperatureData = new TemperatureData
                {
                    Temperature = temperature,
                    Unit = unit,
                    Timestamp = DateTime.Now,
                    DeviceType = DeviceType.Thermometer
                };

                // 驗證解析後的數據
                temperatureData.IsValid = temperatureData.ValidateData();

                return temperatureData;
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"體溫數據解析失敗: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 解析IEEE 11073 SFLOAT格式 (16位浮點數)
        /// </summary>
        /// <param name="data">數據數組</param>
        /// <param name="offset">起始偏移量</param>
        /// <returns>解析後的浮點數值</returns>
        private float ParseSFloat(byte[] data, int offset)
        {
            if (offset + 1 >= data.Length)
                throw new ArgumentException("數據偏移量超出範圍");

            // SFLOAT格式: 16位，其中12位尾數，4位指數
            var value = BitConverter.ToUInt16(data, offset);
            
            // 提取尾數 (低12位)
            var mantissa = value & 0x0FFF;
            
            // 提取指數 (高4位)
            var exponent = (value & 0xF000) >> 12;
            
            // 處理負指數 (4位補碼)
            if (exponent >= 8)
                exponent = exponent - 16;
            
            // 處理特殊值
            if (mantissa == 0x07FF) // NaN
                return float.NaN;
            if (mantissa == 0x0800) // +INFINITY
                return float.PositiveInfinity;
            if (mantissa == 0x0801) // -INFINITY
                return float.NegativeInfinity;
            if (mantissa == 0x0802) // Reserved
                return float.NaN;
            
            // 處理負數 (12位補碼)
            if (mantissa >= 0x0800)
                mantissa = mantissa - 0x1000;
            
            // 計算最終值
            return mantissa * (float)Math.Pow(10, exponent);
        }

        /// <summary>
        /// 解析IEEE 11073 FLOAT格式 (32位浮點數)
        /// </summary>
        /// <param name="data">數據數組</param>
        /// <param name="offset">起始偏移量</param>
        /// <returns>解析後的浮點數值</returns>
        private float ParseFloat(byte[] data, int offset)
        {
            if (offset + 3 >= data.Length)
                throw new ArgumentException("數據偏移量超出範圍");

            // FLOAT格式: 32位，其中24位尾數，8位指數
            var value = BitConverter.ToUInt32(data, offset);
            
            // 提取尾數 (低24位)
            var mantissa = value & 0x00FFFFFF;
            
            // 提取指數 (高8位)
            var exponent = (value & 0xFF000000) >> 24;
            
            // 處理負指數 (8位補碼)
            if (exponent >= 128)
                exponent = exponent - 256;
            
            // 處理特殊值
            if (mantissa == 0x007FFFFF) // NaN
                return float.NaN;
            if (mantissa == 0x00800000) // +INFINITY
                return float.PositiveInfinity;
            if (mantissa == 0x00800001) // -INFINITY
                return float.NegativeInfinity;
            if (mantissa == 0x00800002) // Reserved
                return float.NaN;
            
            // 處理負數 (24位補碼)
            if (mantissa >= 0x00800000)
                mantissa = mantissa - 0x01000000;
            
            // 計算最終值
            return mantissa * (float)Math.Pow(10, exponent);
        }

        /// <summary>
        /// 檢測數據類型 (血壓或體溫)
        /// </summary>
        /// <param name="serviceUuid">BLE服務UUID</param>
        /// <returns>檢測到的設備類型</returns>
        public DeviceType DetectDeviceType(string serviceUuid)
        {
            // 標準BLE服務UUID
            switch (serviceUuid.ToUpperInvariant())
            {
                case "0x1810":
                case "1810":
                    return DeviceType.BloodPressureMonitor;
                
                case "0x1809":
                case "1809":
                    return DeviceType.Thermometer;
                
                default:
                    throw new ArgumentException($"未知的服務UUID: {serviceUuid}");
            }
        }

        /// <summary>
        /// 驗證數據格式是否符合IEEE 11073標準
        /// </summary>
        /// <param name="data">原始數據</param>
        /// <param name="deviceType">設備類型</param>
        /// <returns>如果格式有效則返回true</returns>
        public bool ValidateDataFormat(byte[] data, DeviceType deviceType)
        {
            if (data == null || data.Length == 0)
                return false;

            switch (deviceType)
            {
                case DeviceType.BloodPressureMonitor:
                    // 血壓數據至少需要7個字節 (flags + systolic + diastolic)
                    return data.Length >= 7;
                
                case DeviceType.Thermometer:
                    // 體溫數據至少需要5個字節 (flags + temperature)
                    return data.Length >= 5;
                
                default:
                    return false;
            }
        }
    }
}