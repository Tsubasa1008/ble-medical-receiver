# React Native BLE 真實測試設置

## ✅ 優勢
- 完整的 BLE 功能支持
- 直接使用 react-native-ble-plx
- 無需額外的構建服務

## 🛠️ 設置步驟

### 方案 A: 升級 Node.js (推薦)
```bash
# 1. 升級 Node.js 到 20.19.4+
# 2. 安裝依賴
npm install

# 3. Android 測試
npm run android

# 4. 或生成 APK
npm run build:android
```

### 方案 B: 使用 Android Studio
1. 安裝 Android Studio
2. 設置 Android SDK
3. 連接 Android 設備
4. 運行 `npm run android`

### 方案 C: 預編譯 APK (最簡單)
我可以為你創建一個預編譯的 APK 文件：
- 包含完整的 BLE 功能
- 直接安裝到 Android 手機
- 可以測試真實的 FORA IR40

## 🎯 真實 BLE 功能
- ✅ 掃描 BLE 設備
- ✅ 連接 FORA IR40 體溫計
- ✅ 接收和解析 IEEE 11073 數據
- ✅ 實時數據顯示
- ✅ 數據持久化存儲