# 🛠️ 簡化本地構建設置

## 🎯 快速 Android 環境設置

### 1. 安裝 Android Studio
下載並安裝: https://developer.android.com/studio

### 2. 自動設置環境變量
運行 Android Studio 後，它會自動設置大部分環境。

### 3. 檢查安裝
```bash
# 檢查 Android SDK
%LOCALAPPDATA%\Android\Sdk\platform-tools\adb version

# 設置環境變量 (如果需要)
set ANDROID_HOME=%LOCALAPPDATA%\Android\Sdk
```

### 4. 一鍵構建腳本
```bash
# 運行我們的構建腳本
./build-apk.bat
```

---

## 🚀 替代方案: 使用 Android Studio

### 步驟
1. 打開 Android Studio
2. 選擇 "Open an existing project"
3. 選擇我們的 `android` 文件夾
4. 等待 Gradle 同步完成
5. 點擊 Build > Build Bundle(s) / APK(s) > Build APK(s)

### 優勢
- ✅ 圖形界面操作
- ✅ 自動處理依賴
- ✅ 內建錯誤診斷
- ✅ 一鍵構建

---

## ⚡ 最快方案: 預配置環境

我可以為你創建一個包含所有必要工具的便攜式環境：

### 包含內容
- ✅ Node.js 便攜版
- ✅ Java JDK 便攜版  
- ✅ Android SDK 精簡版
- ✅ 預配置的構建腳本

### 使用方式
1. 下載並解壓環境包
2. 運行 `setup.bat`
3. 運行 `build.bat`
4. 獲得 APK 文件

你想嘗試哪種方案？