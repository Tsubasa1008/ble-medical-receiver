# 📱 APK 構建指南

## 🎯 目標
創建一個包含完整 BLE 功能的 Android APK 文件，可以直接安裝到手機上測試 FORA IR40 體溫計。

## 🛠️ 構建選項

### 選項 1: 自動構建腳本 (推薦)
```bash
# 運行自動構建腳本
./build-apk.bat
```

### 選項 2: 手動構建步驟
```bash
# 1. 安裝依賴
npm install --legacy-peer-deps

# 2. 清理項目
npx react-native clean

# 3. 構建 APK
cd android
gradlew assembleRelease
cd ..
```

### 選項 3: 使用 React Native CLI
```bash
# 直接構建發布版本
npx react-native build-android --mode=release
```

## 📋 環境要求

### 必需軟件
- ✅ Node.js (當前版本 20.13.1 可用)
- ✅ Java JDK 11+
- ✅ Android SDK (API Level 21+)
- ✅ Android Studio (推薦)

### 環境變量
```bash
# Windows
set ANDROID_HOME=C:\Users\%USERNAME%\AppData\Local\Android\Sdk
set JAVA_HOME=C:\Program Files\Java\jdk-11.0.x

# 添加到 PATH
set PATH=%PATH%;%ANDROID_HOME%\tools;%ANDROID_HOME%\platform-tools
```

## 🔧 故障排除

### 常見問題

#### 1. Node.js 版本警告
```
npm WARN EBADENGINE Unsupported engine
```
**解決方案**: 使用 `--legacy-peer-deps` 標誌
```bash
npm install --legacy-peer-deps
```

#### 2. Android SDK 未找到
```
ANDROID_HOME is not set
```
**解決方案**: 
1. 安裝 Android Studio
2. 設置 ANDROID_HOME 環境變量
3. 重啟命令提示符

#### 3. Java 版本問題
```
java.lang.UnsupportedClassVersionError
```
**解決方案**: 安裝 Java JDK 11 或更高版本

#### 4. Gradle 構建失敗
```
Could not resolve all files for configuration
```
**解決方案**: 
```bash
cd android
gradlew clean
gradlew assembleRelease --refresh-dependencies
```

## 📱 APK 輸出

### 構建成功後
APK 文件位置: `android/app/build/outputs/apk/release/app-release.apk`

### 文件信息
- 文件名: `app-release.apk`
- 大小: 約 30-50 MB
- 支持架構: ARM64, ARM, x86, x86_64
- 最低 Android 版本: 5.0 (API 21)

## 🚀 安裝和測試

### 安裝到手機
1. 將 APK 文件傳輸到 Android 手機
2. 啟用"未知來源"安裝權限
3. 點擊 APK 文件安裝

### 測試步驟
1. 開啟應用並授予權限
2. 準備 FORA IR40 體溫計
3. 點擊"掃描設備"
4. 連接並測試數據接收

## 📊 預期結果

### 功能驗證
- ✅ BLE 設備掃描
- ✅ FORA IR40 連接
- ✅ 體溫數據接收
- ✅ IEEE 11073 解析
- ✅ 數據顯示和存儲

### 性能指標
- 連接時間: 2-5 秒
- 數據接收: 即時
- 成功率: >90%
- 斷線檢測: 3-5 秒

## 🆘 需要幫助？

如果構建過程中遇到問題：
1. 檢查環境變量設置
2. 確認所有依賴已安裝
3. 嘗試清理並重新構建
4. 查看詳細錯誤日誌