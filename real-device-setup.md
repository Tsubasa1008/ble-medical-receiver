# 真實設備測試設置

## Android 設備測試

### 1. 啟用開發者模式
- 設置 > 關於手機 > 連續點擊「版本號」7次
- 返回設置 > 開發者選項 > 啟用「USB 調試」

### 2. 連接設備
```bash
# 檢查設備連接
adb devices

# 運行應用
npx react-native run-android
```

### 3. 測試 BLE 功能
- 確保手機藍牙已開啟
- 準備 FORA IR40 體溫計
- 在應用中點擊掃描
- 嘗試連接和接收數據

## iOS 設備測試

### 1. 安裝 Xcode
- 從 Mac App Store 安裝 Xcode

### 2. 設置開發者帳號
- Xcode > Preferences > Accounts
- 添加 Apple ID

### 3. 運行應用
```bash
cd ios && pod install && cd ..
npx react-native run-ios --device
```

## 推薦測試流程
1. 先用 Web 預覽版本熟悉界面
2. 使用 Expo Go 快速測試基本功能
3. 最後在真實設備上測試 BLE 功能