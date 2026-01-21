# 🌐 在線 APK 構建服務

## 🎯 使用 GitHub Actions 自動構建

我可以設置一個 GitHub Actions 工作流程來自動構建 APK。

### 優勢
- ✅ 無需本地 Android SDK
- ✅ 自動化構建流程
- ✅ 免費使用
- ✅ 構建結果可下載

### 設置步驟

#### 1. 創建 GitHub 倉庫
```bash
# 初始化 Git 倉庫
git init
git add .
git commit -m "Initial commit: BLE Medical Receiver"

# 推送到 GitHub (需要先創建倉庫)
git remote add origin https://github.com/yourusername/ble-medical-receiver.git
git push -u origin main
```

#### 2. GitHub Actions 工作流程
文件: `.github/workflows/build-apk.yml`

```yaml
name: Build Android APK

on:
  push:
    branches: [ main ]
  pull_request:
    branches: [ main ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup Node.js
      uses: actions/setup-node@v3
      with:
        node-version: '18'
        cache: 'npm'
    
    - name: Setup Java JDK
      uses: actions/setup-java@v3
      with:
        java-version: '11'
        distribution: 'temurin'
    
    - name: Setup Android SDK
      uses: android-actions/setup-android@v2
    
    - name: Install dependencies
      run: npm install --legacy-peer-deps
    
    - name: Build APK
      run: |
        cd android
        ./gradlew assembleRelease
    
    - name: Upload APK
      uses: actions/upload-artifact@v3
      with:
        name: app-release
        path: android/app/build/outputs/apk/release/app-release.apk
```

#### 3. 下載 APK
構建完成後，在 GitHub Actions 頁面下載 APK 文件。

---

## 🔧 使用 Expo EAS Build

### 優勢
- ✅ 專業的移動應用構建服務
- ✅ 支持 React Native 和 Expo
- ✅ 雲端構建，無需本地環境

### 設置步驟

#### 1. 安裝 EAS CLI
```bash
npm install -g @expo/eas-cli
```

#### 2. 登錄 Expo
```bash
eas login
```

#### 3. 初始化 EAS
```bash
eas build:configure
```

#### 4. 構建 APK
```bash
eas build --platform android --profile preview
```

#### 5. 下載 APK
構建完成後會提供下載鏈接。

---

## 📱 使用 Appetize.io (在線模擬器)

### 優勢
- ✅ 無需下載 APK
- ✅ 直接在瀏覽器中測試
- ✅ 支持 BLE 模擬

### 步驟
1. 上傳 APK 到 Appetize.io
2. 在瀏覽器中運行應用
3. 測試 UI 和基本功能

---

## 🎯 推薦方案

### 最簡單: GitHub Actions
1. 我幫你設置 GitHub 倉庫
2. 推送代碼觸發自動構建
3. 下載生成的 APK

### 最專業: Expo EAS
1. 使用專業構建服務
2. 支持發布到 Google Play
3. 完整的 CI/CD 流程

你想使用哪種方案？