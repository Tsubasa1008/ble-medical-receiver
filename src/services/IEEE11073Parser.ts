import { TemperatureData, BloodPressureData, DeviceType, TemperatureUnit } from '../types';

/**
 * IEEE 11073 標準醫療設備數據解析器
 * 移植自 C# 版本，針對 React Native 優化
 */
export class IEEE11073Parser {
  
  /**
   * 解析體溫計數據 (IEEE 11073-10408標準)
   */
  parseTemperature(data: Uint8Array, deviceId: string): TemperatureData {
    if (!data || data.length < 5) {
      throw new Error('體溫數據長度不足，至少需要5個字節');
    }

    console.log(`[IEEE11073Parser] 解析體溫數據: ${Array.from(data).map(b => b.toString(16).padStart(2, '0')).join(' ')}`);

    const flags = data[0];
    console.log(`[IEEE11073Parser] Flags: 0x${flags.toString(16)}`);
    
    let temperature: number;
    
    try {
      // 方法1: 標準 IEEE 11073 FLOAT 格式 (32位)
      temperature = this.parseFloat(data, 1);
      console.log(`[IEEE11073Parser] IEEE FLOAT 結果: ${temperature}`);
      
      // 如果結果無效，嘗試其他方法
      if (!isFinite(temperature) || temperature <= 0 || temperature > 100) {
        throw new Error('IEEE FLOAT parsing failed');
      }
    } catch {
      try {
        // 方法2: SFLOAT 格式 (16位)
        temperature = this.parseSFloat(data, 1);
        console.log(`[IEEE11073Parser] SFLOAT 結果: ${temperature}`);
        
        if (!isFinite(temperature) || temperature <= 0 || temperature > 100) {
          throw new Error('SFLOAT parsing failed');
        }
      } catch {
        // 方法3: 簡單整數解析 (FORA IR40 格式)
        if (data.length >= 3) {
          const tempRaw = (data[2] << 8) | data[1]; // Little-endian
          temperature = tempRaw / 10.0;
          console.log(`[IEEE11073Parser] 整數解析結果: ${temperature} (原始值: ${tempRaw})`);
          
          if (temperature <= 0 || temperature > 100) {
            // 嘗試不同的縮放因子
            temperature = tempRaw / 100.0;
            console.log(`[IEEE11073Parser] 替代縮放結果: ${temperature}`);
            
            if (temperature <= 0 || temperature > 100) {
              // 最後嘗試：單字節解析
              temperature = data[1] + (data[2] / 10.0);
              console.log(`[IEEE11073Parser] 字節解析結果: ${temperature}`);
            }
          }
        } else {
          throw new Error('無法解析溫度數據 - 所有方法都失敗');
        }
      }
    }

    const temperatureData: TemperatureData = {
      deviceId,
      deviceType: DeviceType.THERMOMETER,
      temperature,
      unit: TemperatureUnit.CELSIUS,
      timestamp: new Date(),
      isValid: this.validateTemperature(temperature)
    };

    console.log(`[IEEE11073Parser] 最終體溫數據:`, temperatureData);
    return temperatureData;
  }

  /**
   * 解析血壓計數據 (IEEE 11073-10407標準)
   */
  parseBloodPressure(data: Uint8Array, deviceId: string): BloodPressureData {
    if (!data || data.length < 7) {
      throw new Error('血壓數據長度不足，至少需要7個字節');
    }

    console.log(`[IEEE11073Parser] 解析血壓數據: ${Array.from(data).map(b => b.toString(16).padStart(2, '0')).join(' ')}`);

    const flags = data[0];
    
    // 解析收縮壓和舒張壓
    const systolic = this.parseSFloat(data, 1);
    const diastolic = this.parseSFloat(data, 3);
    
    // 解析心率 (如果數據長度足夠)
    let heartRate = 0;
    if (data.length >= 15) {
      heartRate = Math.round(this.parseSFloat(data, 13));
    } else if (data.length >= 7) {
      heartRate = Math.round(this.parseSFloat(data, 5));
    }

    const bloodPressureData: BloodPressureData = {
      deviceId,
      deviceType: DeviceType.BLOOD_PRESSURE_MONITOR,
      systolicPressure: systolic,
      diastolicPressure: diastolic,
      heartRate,
      timestamp: new Date(),
      isValid: this.validateBloodPressure(systolic, diastolic, heartRate)
    };

    console.log(`[IEEE11073Parser] 最終血壓數據:`, bloodPressureData);
    return bloodPressureData;
  }

  /**
   * 解析 IEEE 11073 SFLOAT 格式 (16位浮點數)
   */
  private parseSFloat(data: Uint8Array, offset: number): number {
    if (offset + 1 >= data.length) {
      throw new Error('數據偏移量超出範圍');
    }

    // SFLOAT格式: 16位，其中12位尾數，4位指數
    const value = (data[offset + 1] << 8) | data[offset]; // Little-endian
    
    // 提取尾數 (低12位)
    let mantissa = value & 0x0FFF;
    
    // 提取指數 (高4位)
    let exponent = (value & 0xF000) >> 12;
    
    // 處理負指數 (4位補碼)
    if (exponent >= 8) {
      exponent = exponent - 16;
    }
    
    // 處理特殊值
    if (mantissa === 0x07FF) return NaN;
    if (mantissa === 0x0800) return Infinity;
    if (mantissa === 0x0801) return -Infinity;
    if (mantissa === 0x0802) return NaN;
    
    // 處理負數 (12位補碼)
    if (mantissa >= 0x0800) {
      mantissa = mantissa - 0x1000;
    }
    
    // 計算最終值
    return mantissa * Math.pow(10, exponent);
  }

  /**
   * 解析 IEEE 11073 FLOAT 格式 (32位浮點數)
   */
  private parseFloat(data: Uint8Array, offset: number): number {
    if (offset + 3 >= data.length) {
      throw new Error('數據偏移量超出範圍');
    }

    // FLOAT格式: 32位，其中24位尾數，8位指數
    const value = (data[offset + 3] << 24) | (data[offset + 2] << 16) | 
                  (data[offset + 1] << 8) | data[offset]; // Little-endian
    
    // 提取尾數 (低24位)
    let mantissa = value & 0x00FFFFFF;
    
    // 提取指數 (高8位)
    let exponent = (value & 0xFF000000) >> 24;
    
    // 處理負指數 (8位補碼)
    if (exponent >= 128) {
      exponent = exponent - 256;
    }
    
    // 處理特殊值
    if (mantissa === 0x007FFFFF) return NaN;
    if (mantissa === 0x00800000) return Infinity;
    if (mantissa === 0x00800001) return -Infinity;
    if (mantissa === 0x00800002) return NaN;
    
    // 處理負數 (24位補碼)
    if (mantissa >= 0x00800000) {
      mantissa = mantissa - 0x01000000;
    }
    
    // 計算最終值
    return mantissa * Math.pow(10, exponent);
  }

  /**
   * 驗證體溫數據
   */
  private validateTemperature(temperature: number): boolean {
    return isFinite(temperature) && temperature >= 30 && temperature <= 45;
  }

  /**
   * 驗證血壓數據
   */
  private validateBloodPressure(systolic: number, diastolic: number, heartRate: number): boolean {
    return isFinite(systolic) && isFinite(diastolic) &&
           systolic >= 70 && systolic <= 250 &&
           diastolic >= 40 && diastolic <= 150 &&
           systolic > diastolic &&
           heartRate >= 30 && heartRate <= 200;
  }

  /**
   * 檢測設備類型
   */
  detectDeviceType(serviceUuid: string): DeviceType {
    const uuid = serviceUuid.toLowerCase();
    
    if (uuid.includes('1809')) {
      return DeviceType.THERMOMETER;
    }
    
    if (uuid.includes('1810')) {
      return DeviceType.BLOOD_PRESSURE_MONITOR;
    }
    
    return DeviceType.UNKNOWN;
  }
}