# ⚡ 快速 APK 構建指南

## 🎯 目標
在 15 分鐘內獲得可安裝的 APK 文件

## 🚀 方法 1: GitHub Actions 自動構建 (推薦)

### 步驟 1: 設置 GitHub 倉庫
```bash
# 運行設置腳本
./setup-github-build.bat
```

### 步驟 2: 創建 GitHub 倉庫
1. 前往 https://github.com/new
2. 倉庫名稱: `ble-medical-receiver`
3. 設為 Public (免費 Actions)
4. 點擊 "Create repository"

### 步驟 3: 推送代碼
```bash
git remote add origin https://github.com/你的用戶名/ble-medical-receiver.git
git branch -M main
git push -u origin main
```

### 步驟 4: 等待構建完成
1. 前往倉庫的 "Actions" 頁面
2. 查看 "Build Android APK" 工作流程
3. 等待構建完成 (約 10-15 分鐘)

### 步驟 5: 下載 APK
1. 點擊完成的構建任務
2. 在 "Artifacts" 部分下載 APK
3. 或在 "Releases" 頁面下載

---

## 🛠️ 方法 2: 本地構建 (需要 Android Studio)

### 前提條件
- Android Studio 已安裝
- ANDROID_HOME 環境變量已設置

### 快速構建
```bash
# 安裝依賴
npm install --legacy-peer-deps

# 構建 APK
cd android
./gradlew assembleRelease
```

### APK 位置
`android/app/build/outputs/apk/release/app-release.apk`

---

## 📱 APK 信息

### 文件詳情
- **文件名**: `app-release.apk`
- **大小**: 約 35-50 MB
- **版本**: 1.0.x
- **簽名**: Debug 簽名 (測試用)

### 支持規格
- **Android**: 5.0+ (API 21)
- **架構**: ARM64, ARM, x86, x86_64
- **權限**: 藍牙、位置、存儲

### 功能驗證
- ✅ BLE 設備掃描
- ✅ FORA IR40 連接
- ✅ 數據接收和解析
- ✅ 界面顯示
- ✅ 數據存儲

---

## 🔧 故障排除

### GitHub Actions 構建失敗
1. 檢查 Actions 日誌
2. 確認所有文件已推送
3. 重新觸發構建

### 本地構建失敗
1. 檢查 ANDROID_HOME 設置
2. 更新 Android SDK
3. 清理並重新構建

### APK 安裝失敗
1. 啟用未知來源安裝
2. 檢查 Android 版本兼容性
3. 清理舊版本應用

---

## ⏱️ 時間預估

| 步驟 | 時間 |
|------|------|
| 設置 GitHub | 2-3 分鐘 |
| 推送代碼 | 1-2 分鐘 |
| 自動構建 | 10-15 分鐘 |
| 下載安裝 | 1-2 分鐘 |
| **總計** | **15-20 分鐘** |

---

## 🎯 成功標準

### 構建成功
- ✅ GitHub Actions 顯示綠色勾號
- ✅ APK 文件可下載
- ✅ 文件大小合理 (30-60 MB)

### 安裝成功
- ✅ APK 可正常安裝
- ✅ 應用可啟動
- ✅ 權限請求正常

### 功能測試
- ✅ 掃描功能正常
- ✅ 可發現 FORA IR40
- ✅ 連接和數據接收正常

---

## 📞 需要幫助？

如果遇到問題：
1. 檢查 GitHub Actions 日誌
2. 查看本文檔的故障排除部分
3. 提交 GitHub Issue
4. 聯繫技術支持

**預計 15 分鐘後你就可以在手機上測試完整的 BLE 功能了！** 🎉