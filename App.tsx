import React, { useState, useEffect, useRef } from 'react';
import {
  View,
  StyleSheet,
  StatusBar,
  Alert,
  BackHandler,
  AppState as RNAppState,
} from 'react-native';
import { Provider as PaperProvider, Appbar, FAB, Snackbar } from 'react-native-paper';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { BLEService } from './src/services/BLEService';
import { DeviceList } from './src/components/DeviceList';
import { DataDisplay } from './src/components/DataDisplay';
import { BLEDevice, MedicalData, AppState } from './src/types';

const STORAGE_KEY = 'BLE_MEDICAL_DATA';

const App: React.FC = () => {
  // 狀態管理
  const [appState, setAppState] = useState<AppState>({
    isScanning: false,
    devices: [],
    connectedDevices: [],
    receivedData: [],
    connectionStatus: 'Disconnected' as any,
    error: null,
  });

  const [currentView, setCurrentView] = useState<'devices' | 'data'>('devices');
  const [snackbarVisible, setSnackbarVisible] = useState(false);
  const [snackbarMessage, setSnackbarMessage] = useState('');

  // BLE 服務實例
  const bleServiceRef = useRef<BLEService | null>(null);

  // 初始化 BLE 服務
  useEffect(() => {
    initializeBLEService();
    loadStoredData();

    // 監聽應用狀態變化
    const handleAppStateChange = (nextAppState: string) => {
      if (nextAppState === 'background' || nextAppState === 'inactive') {
        // 應用進入後台時停止掃描
        if (bleServiceRef.current && appState.isScanning) {
          bleServiceRef.current.stopScan();
          setAppState(prev => ({ ...prev, isScanning: false }));
        }
      }
    };

    const subscription = RNAppState.addEventListener('change', handleAppStateChange);

    // 監聽返回鍵
    const backHandler = BackHandler.addEventListener('hardwareBackPress', () => {
      if (currentView === 'data') {
        setCurrentView('devices');
        return true;
      }
      return false;
    });

    return () => {
      subscription?.remove();
      backHandler.remove();
      if (bleServiceRef.current) {
        bleServiceRef.current.destroy();
      }
    };
  }, []);

  // 初始化 BLE 服務
  const initializeBLEService = () => {
    try {
      const bleService = new BLEService();
      
      // 設置事件回調
      bleService.onDeviceFound = (device: BLEDevice) => {
        setAppState(prev => {
          const existingIndex = prev.devices.findIndex(d => d.id === device.id);
          if (existingIndex >= 0) {
            // 更新現有設備
            const updatedDevices = [...prev.devices];
            updatedDevices[existingIndex] = device;
            return { ...prev, devices: updatedDevices };
          } else {
            // 添加新設備
            return { ...prev, devices: [...prev.devices, device] };
          }
        });
      };

      bleService.onDeviceConnected = (deviceId: string) => {
        setAppState(prev => ({
          ...prev,
          connectionStatus: 'Connected' as any,
          devices: prev.devices.map(d => 
            d.id === deviceId ? { ...d, isConnected: true } : d
          ),
        }));
        showSnackbar('設備連接成功');
      };

      bleService.onDeviceDisconnected = (deviceId: string) => {
        setAppState(prev => ({
          ...prev,
          connectionStatus: 'Disconnected' as any,
          devices: prev.devices.map(d => 
            d.id === deviceId ? { ...d, isConnected: false } : d
          ),
        }));
        showSnackbar('設備已斷開連接');
      };

      bleService.onDataReceived = (data: MedicalData) => {
        console.log('[App] 收到醫療數據:', data);
        setAppState(prev => ({
          ...prev,
          receivedData: [data, ...prev.receivedData], // 最新數據在前
        }));
        
        // 保存數據到本地存儲
        saveDataToStorage(data);
        
        // 顯示通知
        const deviceType = data.deviceType === 'Thermometer' ? '體溫' : '血壓';
        showSnackbar(`收到新的${deviceType}數據`);
        
        // 自動切換到數據視圖
        setCurrentView('data');
      };

      bleService.onError = (error: string) => {
        console.error('[App] BLE 錯誤:', error);
        setAppState(prev => ({ ...prev, error }));
        showSnackbar(`錯誤: ${error}`);
      };

      bleServiceRef.current = bleService;
      console.log('[App] BLE 服務初始化完成');
    } catch (error) {
      console.error('[App] BLE 服務初始化失敗:', error);
      Alert.alert('初始化失敗', `無法初始化 BLE 服務: ${error}`);
    }
  };

  // 從本地存儲加載數據
  const loadStoredData = async () => {
    try {
      const storedData = await AsyncStorage.getItem(STORAGE_KEY);
      if (storedData) {
        const parsedData = JSON.parse(storedData);
        // 轉換時間戳
        const dataWithDates = parsedData.map((item: any) => ({
          ...item,
          timestamp: new Date(item.timestamp),
        }));
        setAppState(prev => ({ ...prev, receivedData: dataWithDates }));
        console.log(`[App] 加載了 ${dataWithDates.length} 筆歷史數據`);
      }
    } catch (error) {
      console.error('[App] 加載歷史數據失敗:', error);
    }
  };

  // 保存數據到本地存儲
  const saveDataToStorage = async (newData: MedicalData) => {
    try {
      const currentData = appState.receivedData;
      const updatedData = [newData, ...currentData].slice(0, 100); // 只保留最近100筆
      await AsyncStorage.setItem(STORAGE_KEY, JSON.stringify(updatedData));
    } catch (error) {
      console.error('[App] 保存數據失敗:', error);
    }
  };

  // 顯示提示消息
  const showSnackbar = (message: string) => {
    setSnackbarMessage(message);
    setSnackbarVisible(true);
  };

  // 開始掃描設備
  const handleStartScan = async () => {
    if (!bleServiceRef.current) {
      showSnackbar('BLE 服務未初始化');
      return;
    }

    try {
      setAppState(prev => ({ ...prev, isScanning: true, devices: [], error: null }));
      await bleServiceRef.current.startScan();
    } catch (error) {
      console.error('[App] 開始掃描失敗:', error);
      setAppState(prev => ({ ...prev, isScanning: false }));
      showSnackbar(`掃描失敗: ${error}`);
    }
  };

  // 連接設備
  const handleDevicePress = async (device: BLEDevice) => {
    if (!bleServiceRef.current) {
      showSnackbar('BLE 服務未初始化');
      return;
    }

    if (device.isConnected) {
      // 斷開連接
      try {
        await bleServiceRef.current.disconnectDevice(device.id);
      } catch (error) {
        showSnackbar(`斷開連接失敗: ${error}`);
      }
    } else {
      // 連接設備
      try {
        setAppState(prev => ({ ...prev, connectionStatus: 'Connecting' as any }));
        const success = await bleServiceRef.current.connectToDevice(device.id);
        if (!success) {
          setAppState(prev => ({ ...prev, connectionStatus: 'Disconnected' as any }));
          showSnackbar('連接失敗，請重試');
        }
      } catch (error) {
        setAppState(prev => ({ ...prev, connectionStatus: 'Disconnected' as any }));
        showSnackbar(`連接失敗: ${error}`);
      }
    }
  };

  // 清除所有數據
  const handleClearData = () => {
    Alert.alert(
      '清除數據',
      '確定要清除所有測量數據嗎？此操作無法撤銷。',
      [
        { text: '取消', style: 'cancel' },
        {
          text: '確定',
          style: 'destructive',
          onPress: async () => {
            try {
              await AsyncStorage.removeItem(STORAGE_KEY);
              setAppState(prev => ({ ...prev, receivedData: [] }));
              showSnackbar('數據已清除');
            } catch (error) {
              showSnackbar('清除數據失敗');
            }
          },
        },
      ]
    );
  };

  return (
    <PaperProvider>
      <SafeAreaProvider>
        <StatusBar barStyle="dark-content" backgroundColor="#FFFFFF" />
        <SafeAreaView style={styles.container}>
          {/* 頂部導航欄 */}
          <Appbar.Header>
            <Appbar.Content title="BLE 醫療數據接收器" />
            <Appbar.Action
              icon={currentView === 'devices' ? 'chart-line' : 'bluetooth'}
              onPress={() => setCurrentView(currentView === 'devices' ? 'data' : 'devices')}
            />
            {currentView === 'data' && appState.receivedData.length > 0 && (
              <Appbar.Action
                icon="delete"
                onPress={handleClearData}
              />
            )}
          </Appbar.Header>

          {/* 主要內容 */}
          <View style={styles.content}>
            {currentView === 'devices' ? (
              <DeviceList
                devices={appState.devices}
                isScanning={appState.isScanning}
                onDevicePress={handleDevicePress}
                onRefresh={handleStartScan}
              />
            ) : (
              <DataDisplay
                data={appState.receivedData}
                onDataPress={(data) => {
                  // 可以在這裡添加數據詳情查看功能
                  console.log('查看數據詳情:', data);
                }}
              />
            )}
          </View>

          {/* 浮動操作按鈕 */}
          {currentView === 'devices' && (
            <FAB
              style={styles.fab}
              icon={appState.isScanning ? 'stop' : 'bluetooth-connect'}
              label={appState.isScanning ? '停止掃描' : '掃描設備'}
              onPress={appState.isScanning ? 
                () => {
                  bleServiceRef.current?.stopScan();
                  setAppState(prev => ({ ...prev, isScanning: false }));
                } : 
                handleStartScan
              }
            />
          )}

          {/* 提示消息 */}
          <Snackbar
            visible={snackbarVisible}
            onDismiss={() => setSnackbarVisible(false)}
            duration={3000}
            style={styles.snackbar}
          >
            {snackbarMessage}
          </Snackbar>
        </SafeAreaView>
      </SafeAreaProvider>
    </PaperProvider>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#FFFFFF',
  },
  content: {
    flex: 1,
  },
  fab: {
    position: 'absolute',
    margin: 16,
    right: 0,
    bottom: 0,
    backgroundColor: '#2196F3',
  },
  snackbar: {
    backgroundColor: '#323232',
  },
});

export default App;