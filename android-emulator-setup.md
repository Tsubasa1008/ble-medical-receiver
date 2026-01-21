# Android 模擬器測試設置

## 1. 安裝 Android Studio
下載並安裝 Android Studio: https://developer.android.com/studio

## 2. 設置 Android SDK
- 打開 Android Studio
- 進入 Settings > Appearance & Behavior > System Settings > Android SDK
- 安裝 Android 13 (API Level 33) 或更高版本

## 3. 創建虛擬設備
- 打開 AVD Manager
- 創建新的虛擬設備 (推薦 Pixel 6)
- 選擇 Android 13 系統映像

## 4. 設置環境變量
```bash
# Windows
set ANDROID_HOME=C:\Users\%USERNAME%\AppData\Local\Android\Sdk
set PATH=%PATH%;%ANDROID_HOME%\tools;%ANDROID_HOME%\platform-tools
```

## 5. 運行項目
```bash
npm install
npx react-native run-android
```

## 注意事項
- 模擬器無法測試真實的 BLE 功能
- 但可以測試 UI 和應用邏輯
- 需要真實設備才能測試 BLE 連接