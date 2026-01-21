using System;

namespace BLEDataReceiver.Models
{
    /// <summary>
    /// 基礎醫療數據模型
    /// </summary>
    public abstract class MedicalData
    {
        public DateTime Timestamp { get; set; }
        public string DeviceId { get; set; } = string.Empty;
        public DeviceType DeviceType { get; set; }
        public bool IsValid { get; set; }

        /// <summary>
        /// 驗證數據的有效性
        /// </summary>
        /// <returns>如果數據有效則返回true，否則返回false</returns>
        public abstract bool ValidateData();

        /// <summary>
        /// 檢查數值是否在正常範圍內
        /// </summary>
        /// <returns>如果數值在正常範圍內則返回true，否則返回false</returns>
        public abstract bool IsInNormalRange();
    }

    /// <summary>
    /// 血壓數據模型
    /// </summary>
    public class BloodPressureData : MedicalData
    {
        public float SystolicPressure { get; set; }  // mmHg
        public float DiastolicPressure { get; set; } // mmHg
        public int HeartRate { get; set; }           // bpm
        
        /// <summary>
        /// 檢查是否為高血壓
        /// </summary>
        public bool IsHypertensive => SystolicPressure > 140 || DiastolicPressure > 90;

        /// <summary>
        /// 驗證血壓數據的有效性
        /// </summary>
        /// <returns>如果數據有效則返回true，否則返回false</returns>
        public override bool ValidateData()
        {
            // 檢查基本數據有效性
            if (string.IsNullOrEmpty(DeviceId) || Timestamp == default)
                return false;

            // 檢查血壓數值範圍 (合理的醫學範圍)
            if (SystolicPressure < 50 || SystolicPressure > 300)
                return false;

            if (DiastolicPressure < 30 || DiastolicPressure > 200)
                return false;

            // 收縮壓應該大於舒張壓
            if (SystolicPressure <= DiastolicPressure)
                return false;

            // 檢查心率範圍
            if (HeartRate < 30 || HeartRate > 220)
                return false;

            return true;
        }

        /// <summary>
        /// 檢查血壓數值是否在正常範圍內
        /// </summary>
        /// <returns>如果數值在正常範圍內則返回true，否則返回false</returns>
        public override bool IsInNormalRange()
        {
            // 正常血壓範圍: 收縮壓 90-140 mmHg, 舒張壓 60-90 mmHg
            bool systolicNormal = SystolicPressure >= 90 && SystolicPressure <= 140;
            bool diastolicNormal = DiastolicPressure >= 60 && DiastolicPressure <= 90;
            
            // 正常心率範圍: 60-100 bpm
            bool heartRateNormal = HeartRate >= 60 && HeartRate <= 100;

            return systolicNormal && diastolicNormal && heartRateNormal;
        }
    }

    /// <summary>
    /// 體溫數據模型
    /// </summary>
    public class TemperatureData : MedicalData
    {
        public float Temperature { get; set; }        // Celsius
        public TemperatureUnit Unit { get; set; }
        
        /// <summary>
        /// 檢查是否發燒
        /// </summary>
        public bool IsFever => Temperature > 37.5f;

        /// <summary>
        /// 驗證體溫數據的有效性
        /// </summary>
        /// <returns>如果數據有效則返回true，否則返回false</returns>
        public override bool ValidateData()
        {
            // 檢查基本數據有效性
            if (string.IsNullOrEmpty(DeviceId) || Timestamp == default)
                return false;

            // 檢查體溫數值範圍 (合理的醫學範圍，攝氏度)
            if (Unit == TemperatureUnit.Celsius)
            {
                if (Temperature < 25.0f || Temperature > 50.0f)
                    return false;
            }
            else if (Unit == TemperatureUnit.Fahrenheit)
            {
                if (Temperature < 77.0f || Temperature > 122.0f)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 檢查體溫數值是否在正常範圍內
        /// </summary>
        /// <returns>如果數值在正常範圍內則返回true，否則返回false</returns>
        public override bool IsInNormalRange()
        {
            if (Unit == TemperatureUnit.Celsius)
            {
                // 正常體溫範圍: 36.0-37.5°C
                return Temperature >= 36.0f && Temperature <= 37.5f;
            }
            else if (Unit == TemperatureUnit.Fahrenheit)
            {
                // 正常體溫範圍: 96.8-99.5°F
                return Temperature >= 96.8f && Temperature <= 99.5f;
            }

            return false;
        }
    }

    /// <summary>
    /// 設備類型枚舉
    /// </summary>
    public enum DeviceType
    {
        BloodPressureMonitor,
        Thermometer
    }

    /// <summary>
    /// 溫度單位枚舉
    /// </summary>
    public enum TemperatureUnit
    {
        Celsius,
        Fahrenheit
    }

    /// <summary>
    /// 連接狀態枚舉
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Failed
    }
}