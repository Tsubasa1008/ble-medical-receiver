# 🚀 快速測試指南

## 方法 1: Expo Go (推薦 - 最簡單)

### 步驟 1: 手機安裝 Expo Go
- **Android**: Google Play 搜索 "Expo Go"
- **iOS**: App Store 搜索 "Expo Go"

### 步驟 2: 創建 Expo 版本
```bash
# 安裝 Expo CLI
npm install -g @expo/cli

# 創建項目
npx create-expo-app BLEMedicalApp --template blank-typescript
cd BLEMedicalApp

# 複製我們的代碼到新項目
# (手動複製 src 文件夾和 App.tsx)
```

### 步驟 3: 運行
```bash
npx expo start
```

### 步驟 4: 掃描 QR 碼
用 Expo Go 掃描終端顯示的 QR 碼，應用會直接在手機上運行！

---

## 方法 2: 直接 APK 安裝

### 步驟 1: 準備環境
```bash
# 檢查 Node.js 版本 (需要 >= 18)
node -v

# 安裝依賴
npm install
```

### 步驟 2: Android 設備設置
1. 手機設置 > 關於手機 > 連續點擊「版本號」7次
2. 返回設置 > 開發者選項 > 啟用「USB 調試」
3. 用 USB 線連接手機到電腦

### 步驟 3: 檢查連接
```bash
# 檢查設備是否連接
adb devices
# 應該顯示你的設備 ID
```

### 步驟 4: 安裝應用
```bash
# 直接安裝到手機
npm run android
```

---

## 方法 3: APK 文件分享

### 生成 APK
```bash
# 生成發布版 APK
npm run build:android
```

### 安裝 APK
1. 將 APK 文件傳輸到手機
2. 手機上點擊 APK 文件安裝
3. 可能需要允許「未知來源」安裝

---

## 🎯 測試重點

### BLE 功能測試
1. **權限檢查**: 應用會自動請求藍牙和位置權限
2. **設備掃描**: 點擊掃描按鈕，應該能發現 FORA IR40
3. **設備連接**: 點擊設備卡片進行連接
4. **數據接收**: 在體溫計上測量，數據應該自動傳輸到應用

### 性能對比
- **連接速度**: 應該比 Windows 版本快 5-10 倍
- **斷線檢測**: Android ~3秒，iOS <1秒
- **成功率**: 應該 >90% (vs Windows ~30%)

---

## 🔧 故障排除

### 常見問題
1. **權限被拒絕**: 手動到設置中授予藍牙和位置權限
2. **掃描不到設備**: 確保 FORA IR40 處於配對模式
3. **連接失敗**: 重啟藍牙或重新啟動應用

### 調試方法
```bash
# 查看應用日誌
adb logcat | grep BLE
```