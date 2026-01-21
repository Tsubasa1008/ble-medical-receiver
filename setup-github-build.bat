@echo off
echo 🚀 設置 GitHub 自動構建...

echo 📋 初始化 Git 倉庫...
git init
if %errorlevel% neq 0 (
    echo ❌ Git 初始化失敗
    pause
    exit /b 1
)

echo 📦 添加所有文件...
git add .
git commit -m "Initial commit: BLE Medical Receiver React Native App

✨ 功能特性:
- 🔍 BLE 設備掃描和連接
- 🩺 FORA IR40 體溫計支持  
- 📊 IEEE 11073 數據解析
- 💾 本地數據存儲
- 🎨 Material Design 界面
- 🌐 中文本地化

🛠️ 技術架構:
- React Native 0.72.6
- react-native-ble-plx
- TypeScript
- React Native Paper

📱 構建目標:
- Android APK (自動構建)
- 支持 Android 5.0+
- 完整 BLE 功能"

echo ✅ Git 倉庫初始化完成

echo.
echo 📱 下一步操作:
echo.
echo 1. 在 GitHub 上創建新倉庫 'ble-medical-receiver'
echo 2. 運行以下命令推送代碼:
echo.
echo    git remote add origin https://github.com/你的用戶名/ble-medical-receiver.git
echo    git branch -M main
echo    git push -u origin main
echo.
echo 3. 推送完成後，GitHub Actions 會自動開始構建 APK
echo 4. 構建完成後可在 Actions 頁面下載 APK 文件
echo.
echo 🎯 預計構建時間: 10-15 分鐘
echo 📱 APK 大小: 約 35-50 MB
echo.

pause