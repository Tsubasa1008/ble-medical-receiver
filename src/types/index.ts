// 設備類型定義
export enum DeviceType {
  THERMOMETER = 'Thermometer',
  BLOOD_PRESSURE_MONITOR = 'BloodPressureMonitor',
  UNKNOWN = 'Unknown'
}

// 溫度單位
export enum TemperatureUnit {
  CELSIUS = 'Celsius',
  FAHRENHEIT = 'Fahrenheit'
}

// 連接狀態
export enum ConnectionStatus {
  DISCONNECTED = 'Disconnected',
  CONNECTING = 'Connecting',
  CONNECTED = 'Connected',
  DISCONNECTING = 'Disconnecting'
}

// 基礎醫療數據接口
export interface MedicalData {
  deviceId: string;
  deviceType: DeviceType;
  timestamp: Date;
  isValid: boolean;
}

// 體溫數據
export interface TemperatureData extends MedicalData {
  temperature: number;
  unit: TemperatureUnit;
}

// 血壓數據
export interface BloodPressureData extends MedicalData {
  systolicPressure: number;
  diastolicPressure: number;
  heartRate: number;
}

// BLE 設備信息
export interface BLEDevice {
  id: string;
  name: string | null;
  rssi: number;
  deviceType: DeviceType;
  isConnected: boolean;
  lastSeen: Date;
}

// 連接事件
export interface ConnectionEvent {
  deviceId: string;
  status: ConnectionStatus;
  error?: string;
}

// 數據接收事件
export interface DataReceivedEvent {
  deviceId: string;
  data: MedicalData;
}

// 應用狀態
export interface AppState {
  isScanning: boolean;
  devices: BLEDevice[];
  connectedDevices: BLEDevice[];
  receivedData: MedicalData[];
  connectionStatus: ConnectionStatus;
  error: string | null;
}