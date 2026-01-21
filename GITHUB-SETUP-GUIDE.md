# 🚀 GitHub Actions APK 構建指南

## 🎯 目標
使用 GitHub Actions 自動構建 BLE 醫療數據接收器 APK

## ⏱️ 預計時間
**總計 15-20 分鐘**
- GitHub 設置: 5 分鐘
- 代碼推送: 2 分鐘  
- 自動構建: 10-15 分鐘

---

## 📋 第一步: 創建 GitHub 倉庫

### 1.1 前往 GitHub
打開瀏覽器，前往: https://github.com/new

### 1.2 創建倉庫
- **Repository name**: `ble-medical-receiver`
- **Description**: `🩺 BLE 醫療數據接收器 - 支持 FORA IR40 體溫計`
- **Visibility**: 選擇 **Public** (免費 Actions 額度)
- **Initialize**: 不要勾選任何選項 (我們已有代碼)
- 點擊 **"Create repository"**

### 1.3 複製倉庫 URL
創建後會顯示倉庫 URL，類似：
```
https://github.com/你的用戶名/ble-medical-receiver.git
```

---

## 📤 第二步: 推送代碼到 GitHub

### 2.1 添加遠程倉庫
在當前目錄運行：
```bash
git remote add origin https://github.com/你的用戶名/ble-medical-receiver.git
```

### 2.2 設置主分支
```bash
git branch -M main
```

### 2.3 推送代碼
```bash
git push -u origin main
```

**注意**: 如果是第一次使用 GitHub，可能需要：
- 輸入 GitHub 用戶名和密碼
- 或設置 Personal Access Token

---

## ⚡ 第三步: 觸發自動構建

### 3.1 查看 Actions
推送完成後：
1. 前往你的 GitHub 倉庫頁面
2. 點擊 **"Actions"** 標籤
3. 應該會看到 **"Build Android APK"** 工作流程正在運行

### 3.2 監控構建進度
- 🟡 **黃色圓點**: 構建進行中
- 🟢 **綠色勾號**: 構建成功
- 🔴 **紅色叉號**: 構建失敗

### 3.3 構建時間
- **預計時間**: 10-15 分鐘
- **包含步驟**:
  - 設置 Node.js 環境
  - 設置 Java JDK
  - 設置 Android SDK
  - 安裝依賴
  - 構建 APK

---

## 📥 第四步: 下載 APK

### 4.1 方式一: 從 Actions 下載
構建完成後：
1. 點擊完成的構建任務
2. 滾動到底部找到 **"Artifacts"** 部分
3. 點擊 **"BLE-Medical-Receiver-APK"** 下載

### 4.2 方式二: 從 Releases 下載
如果推送到 main 分支：
1. 前往倉庫的 **"Releases"** 頁面
2. 找到最新版本
3. 下載 APK 文件

---

## 📱 第五步: 安裝和測試

### 5.1 APK 信息
- **文件名**: `app-release.apk`
- **大小**: 約 35-50 MB
- **版本**: 1.0.x
- **支持**: Android 5.0+

### 5.2 安裝步驟
1. **傳輸到手機**: 通過 USB、雲端或其他方式
2. **啟用未知來源**: 設置 > 安全 > 未知來源
3. **安裝 APK**: 點擊文件並按提示安裝
4. **授予權限**: 藍牙、位置等權限

### 5.3 測試 FORA IR40
1. 開啟應用
2. 點擊「掃描設備」
3. 連接 FORA IR40
4. 進行體溫測量
5. 查看數據接收

---

## 🔧 故障排除

### GitHub 推送問題
**問題**: `Permission denied`
**解決方案**:
```bash
# 使用 Personal Access Token
git remote set-url origin https://你的用戶名:你的token@github.com/你的用戶名/ble-medical-receiver.git
```

### Actions 構建失敗
**常見原因**:
1. **依賴問題**: 檢查 package.json
2. **權限問題**: 確保倉庫是 Public
3. **配置錯誤**: 檢查 .github/workflows/build-apk.yml

**解決方案**:
1. 查看 Actions 日誌
2. 修復錯誤後重新推送
3. 或手動觸發構建

### APK 安裝失敗
**問題**: 無法安裝 APK
**解決方案**:
1. 檢查 Android 版本 (需要 5.0+)
2. 啟用未知來源安裝
3. 清理存儲空間
4. 重新下載 APK

---

## 📊 構建狀態監控

### 實時狀態
在 GitHub 倉庫頁面可以看到：
- 🟢 **Build passing**: 構建成功
- 🔴 **Build failing**: 構建失敗
- 🟡 **Build pending**: 構建中

### 構建歷史
在 Actions 頁面可以查看：
- 所有構建記錄
- 構建時間和日誌
- 成功/失敗統計

### 自動化觸發
每次推送到 main 分支都會：
- 自動觸發構建
- 生成新的 APK
- 創建新的 Release

---

## 🎉 成功標準

### ✅ 構建成功
- GitHub Actions 顯示綠色勾號
- APK 文件可以下載
- 文件大小合理 (30-60 MB)

### ✅ 安裝成功
- APK 可以正常安裝到 Android 設備
- 應用可以啟動並顯示界面
- 權限請求正常

### ✅ 功能驗證
- 可以掃描 BLE 設備
- 可以發現 FORA IR40 體溫計
- 可以連接並接收數據
- 數據解析和顯示正確

---

## 🚀 下一步操作

### 立即開始
1. **創建 GitHub 倉庫** (5 分鐘)
2. **推送代碼** (2 分鐘)
3. **等待構建完成** (15 分鐘)
4. **下載並測試 APK**

### 後續改進
- 添加更多醫療設備支持
- 優化 UI 界面
- 增加數據導出功能
- 集成雲端同步

**準備好開始了嗎？** 🎯