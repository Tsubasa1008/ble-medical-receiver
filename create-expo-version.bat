@echo off
echo 🚀 創建 Expo 測試版本...

REM 檢查 npm
npm -v
if %errorlevel% neq 0 (
    echo ❌ 錯誤: 未找到 npm
    pause
    exit /b 1
)

REM 安裝 Expo CLI
echo 📦 安裝 Expo CLI...
call npm install -g @expo/cli

REM 創建項目
echo 🏗️ 創建 Expo 項目...
call npx create-expo-app BLEMedicalReceiver --template blank-typescript

echo ✅ Expo 項目創建完成！

echo.
echo 📱 下一步操作：
echo 1. 在手機上安裝 Expo Go 應用
echo 2. 運行: cd BLEMedicalReceiver
echo 3. 運行: npx expo start
echo 4. 用 Expo Go 掃描 QR 碼
echo.

pause