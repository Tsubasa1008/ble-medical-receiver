import { BleManager, Device, Characteristic, State } from 'react-native-ble-plx';
import { PermissionsAndroid, Platform, Alert } from 'react-native';
import { PERMISSIONS, request, RESULTS } from 'react-native-permissions';
import { BLEDevice, DeviceType, ConnectionStatus, MedicalData } from '../types';
import { IEEE11073Parser } from './IEEE11073Parser';

/**
 * BLE 服務管理器
 * 處理設備掃描、連接、數據接收等核心功能
 */
export class BLEService {
  private bleManager: BleManager;
  private parser: IEEE11073Parser;
  private connectedDevices: Map<string, Device> = new Map();
  private scanningDevices: Map<string, BLEDevice> = new Map();
  private isScanning = false;

  // BLE 服務和特徵值 UUID
  private readonly TEMPERATURE_SERVICE_UUID = '00001809-0000-1000-8000-00805f9b34fb';
  private readonly TEMPERATURE_CHARACTERISTIC_UUID = '00002a1c-0000-1000-8000-00805f9b34fb';
  private readonly BLOOD_PRESSURE_SERVICE_UUID = '00001810-0000-1000-8000-00805f9b34fb';
  private readonly BLOOD_PRESSURE_CHARACTERISTIC_UUID = '00002a35-0000-1000-8000-00805f9b34fb';

  // 事件回調
  public onDeviceFound?: (device: BLEDevice) => void;
  public onDeviceConnected?: (deviceId: string) => void;
  public onDeviceDisconnected?: (deviceId: string) => void;
  public onDataReceived?: (data: MedicalData) => void;
  public onError?: (error: string) => void;

  constructor() {
    this.bleManager = new BleManager();
    this.parser = new IEEE11073Parser();
    this.initializeBLE();
  }

  /**
   * 初始化 BLE 管理器
   */
  private async initializeBLE() {
    try {
      // 監聽 BLE 狀態變化
      this.bleManager.onStateChange((state) => {
        console.log(`[BLEService] BLE 狀態變化: ${state}`);
        if (state === State.PoweredOn) {
          console.log('[BLEService] BLE 已啟用');
        } else {
          console.log('[BLEService] BLE 未啟用');
          this.onError?.('藍牙未啟用，請開啟藍牙功能');
        }
      }, true);

      console.log('[BLEService] BLE 服務初始化完成');
    } catch (error) {
      console.error('[BLEService] 初始化失敗:', error);
      this.onError?.(`BLE 初始化失敗: ${error}`);
    }
  }

  /**
   * 請求必要的權限
   */
  async requestPermissions(): Promise<boolean> {
    try {
      if (Platform.OS === 'android') {
        // Android 權限
        const permissions = [
          PermissionsAndroid.PERMISSIONS.ACCESS_FINE_LOCATION,
          PermissionsAndroid.PERMISSIONS.BLUETOOTH_SCAN,
          PermissionsAndroid.PERMISSIONS.BLUETOOTH_CONNECT,
        ];

        const granted = await PermissionsAndroid.requestMultiple(permissions);
        
        const allGranted = Object.values(granted).every(
          permission => permission === PermissionsAndroid.RESULTS.GRANTED
        );

        if (!allGranted) {
          Alert.alert('權限需求', '應用需要藍牙和位置權限才能正常工作');
          return false;
        }
      } else {
        // iOS 權限
        const bluetoothPermission = await request(PERMISSIONS.IOS.BLUETOOTH_PERIPHERAL);
        if (bluetoothPermission !== RESULTS.GRANTED) {
          Alert.alert('權限需求', '應用需要藍牙權限才能正常工作');
          return false;
        }
      }

      console.log('[BLEService] 權限請求成功');
      return true;
    } catch (error) {
      console.error('[BLEService] 權限請求失敗:', error);
      this.onError?.(`權限請求失敗: ${error}`);
      return false;
    }
  }

  /**
   * 開始掃描設備
   */
  async startScan(): Promise<void> {
    try {
      if (this.isScanning) {
        console.log('[BLEService] 已在掃描中');
        return;
      }

      // 檢查權限
      const hasPermissions = await this.requestPermissions();
      if (!hasPermissions) {
        return;
      }

      console.log('[BLEService] 開始掃描 BLE 設備...');
      this.isScanning = true;
      this.scanningDevices.clear();

      // 掃描指定服務的設備
      this.bleManager.startDeviceScan(
        [this.TEMPERATURE_SERVICE_UUID, this.BLOOD_PRESSURE_SERVICE_UUID],
        { allowDuplicates: false },
        (error, device) => {
          if (error) {
            console.error('[BLEService] 掃描錯誤:', error);
            this.onError?.(`掃描錯誤: ${error.message}`);
            return;
          }

          if (device && device.name) {
            const bleDevice = this.createBLEDevice(device);
            this.scanningDevices.set(device.id, bleDevice);
            
            console.log(`[BLEService] 發現設備: ${device.name} (${device.id})`);
            this.onDeviceFound?.(bleDevice);
          }
        }
      );

      // 30秒後自動停止掃描
      setTimeout(() => {
        if (this.isScanning) {
          this.stopScan();
        }
      }, 30000);

    } catch (error) {
      console.error('[BLEService] 開始掃描失敗:', error);
      this.onError?.(`開始掃描失敗: ${error}`);
      this.isScanning = false;
    }
  }

  /**
   * 停止掃描設備
   */
  stopScan(): void {
    if (this.isScanning) {
      console.log('[BLEService] 停止掃描');
      this.bleManager.stopDeviceScan();
      this.isScanning = false;
    }
  }

  /**
   * 連接到設備
   */
  async connectToDevice(deviceId: string): Promise<boolean> {
    try {
      console.log(`[BLEService] 嘗試連接設備: ${deviceId}`);

      // 停止掃描
      this.stopScan();

      // 連接設備
      const device = await this.bleManager.connectToDevice(deviceId);
      console.log(`[BLEService] 設備連接成功: ${device.name}`);

      // 發現服務
      await device.discoverAllServicesAndCharacteristics();
      console.log(`[BLEService] 服務發現完成: ${device.id}`);

      // 存儲連接的設備
      this.connectedDevices.set(deviceId, device);

      // 設置斷線監聽
      device.onDisconnected((error, disconnectedDevice) => {
        console.log(`[BLEService] 設備斷線: ${disconnectedDevice?.id}`);
        this.connectedDevices.delete(deviceId);
        this.onDeviceDisconnected?.(deviceId);
        
        if (error) {
          console.error('[BLEService] 斷線錯誤:', error);
        }
      });

      // 訂閱特徵值
      await this.subscribeToCharacteristics(device);

      this.onDeviceConnected?.(deviceId);
      return true;

    } catch (error) {
      console.error(`[BLEService] 連接設備失敗: ${deviceId}`, error);
      this.onError?.(`連接設備失敗: ${error}`);
      return false;
    }
  }

  /**
   * 斷開設備連接
   */
  async disconnectDevice(deviceId: string): Promise<void> {
    try {
      const device = this.connectedDevices.get(deviceId);
      if (device) {
        console.log(`[BLEService] 斷開設備連接: ${deviceId}`);
        await device.cancelConnection();
        this.connectedDevices.delete(deviceId);
      }
    } catch (error) {
      console.error(`[BLEService] 斷開連接失敗: ${deviceId}`, error);
      this.onError?.(`斷開連接失敗: ${error}`);
    }
  }

  /**
   * 斷開所有設備連接
   */
  async disconnectAllDevices(): Promise<void> {
    console.log('[BLEService] 斷開所有設備連接');
    const disconnectPromises = Array.from(this.connectedDevices.keys()).map(
      deviceId => this.disconnectDevice(deviceId)
    );
    await Promise.all(disconnectPromises);
  }

  /**
   * 訂閱設備特徵值
   */
  private async subscribeToCharacteristics(device: Device): Promise<void> {
    try {
      console.log(`[BLEService] 開始訂閱特徵值: ${device.id}`);

      // 獲取所有服務
      const services = await device.services();
      
      for (const service of services) {
        console.log(`[BLEService] 檢查服務: ${service.uuid}`);
        
        // 獲取服務的特徵值
        const characteristics = await service.characteristics();
        
        for (const characteristic of characteristics) {
          console.log(`[BLEService] 檢查特徵值: ${characteristic.uuid}`);
          
          // 檢查是否為目標特徵值
          if (this.isTargetCharacteristic(characteristic.uuid)) {
            console.log(`[BLEService] 訂閱特徵值: ${characteristic.uuid}`);
            
            // 訂閱通知
            characteristic.monitor((error, updatedCharacteristic) => {
              if (error) {
                console.error('[BLEService] 特徵值監聽錯誤:', error);
                return;
              }
              
              if (updatedCharacteristic?.value) {
                this.handleCharacteristicData(
                  updatedCharacteristic.value,
                  updatedCharacteristic.uuid,
                  device.id
                );
              }
            });
          }
        }
      }

      console.log(`[BLEService] 特徵值訂閱完成: ${device.id}`);
    } catch (error) {
      console.error(`[BLEService] 訂閱特徵值失敗: ${device.id}`, error);
      throw error;
    }
  }

  /**
   * 檢查是否為目標特徵值
   */
  private isTargetCharacteristic(uuid: string): boolean {
    const normalizedUuid = uuid.toLowerCase();
    return normalizedUuid.includes('2a1c') || // Temperature Measurement
           normalizedUuid.includes('2a35') || // Blood Pressure Measurement
           normalizedUuid.includes('1524');   // FORA 自定義特徵值
  }

  /**
   * 處理特徵值數據
   */
  private handleCharacteristicData(base64Data: string, characteristicUuid: string, deviceId: string): void {
    try {
      console.log(`[BLEService] 收到數據: ${deviceId}, 特徵值: ${characteristicUuid}`);
      
      // 解碼 Base64 數據
      const binaryString = atob(base64Data);
      const data = new Uint8Array(binaryString.length);
      for (let i = 0; i < binaryString.length; i++) {
        data[i] = binaryString.charCodeAt(i);
      }

      console.log(`[BLEService] 原始數據: ${Array.from(data).map(b => b.toString(16).padStart(2, '0')).join(' ')}`);

      // 根據特徵值類型解析數據
      let medicalData: MedicalData;
      
      if (characteristicUuid.toLowerCase().includes('2a1c') || 
          characteristicUuid.toLowerCase().includes('1524')) {
        // 體溫數據
        medicalData = this.parser.parseTemperature(data, deviceId);
      } else if (characteristicUuid.toLowerCase().includes('2a35')) {
        // 血壓數據
        medicalData = this.parser.parseBloodPressure(data, deviceId);
      } else {
        console.warn(`[BLEService] 未知的特徵值類型: ${characteristicUuid}`);
        return;
      }

      console.log(`[BLEService] 解析完成:`, medicalData);
      this.onDataReceived?.(medicalData);

    } catch (error) {
      console.error('[BLEService] 數據處理失敗:', error);
      this.onError?.(`數據處理失敗: ${error}`);
    }
  }

  /**
   * 創建 BLE 設備對象
   */
  private createBLEDevice(device: Device): BLEDevice {
    // 根據設備名稱或服務判斷設備類型
    let deviceType = DeviceType.UNKNOWN;
    
    if (device.name?.toLowerCase().includes('ir40') || 
        device.name?.toLowerCase().includes('thermometer')) {
      deviceType = DeviceType.THERMOMETER;
    } else if (device.name?.toLowerCase().includes('blood') ||
               device.name?.toLowerCase().includes('pressure')) {
      deviceType = DeviceType.BLOOD_PRESSURE_MONITOR;
    }

    return {
      id: device.id,
      name: device.name,
      rssi: device.rssi || -100,
      deviceType,
      isConnected: false,
      lastSeen: new Date()
    };
  }

  /**
   * 獲取連接狀態
   */
  getConnectionStatus(): ConnectionStatus {
    return this.connectedDevices.size > 0 ? 
           ConnectionStatus.CONNECTED : 
           ConnectionStatus.DISCONNECTED;
  }

  /**
   * 獲取已連接的設備列表
   */
  getConnectedDevices(): BLEDevice[] {
    return Array.from(this.connectedDevices.values()).map(device => 
      this.createBLEDevice(device)
    );
  }

  /**
   * 清理資源
   */
  destroy(): void {
    console.log('[BLEService] 清理 BLE 服務');
    this.stopScan();
    this.disconnectAllDevices();
    this.bleManager.destroy();
  }
}