@echo off
REM BLE 醫療數據接收器 - Windows 構建腳本

echo 🚀 開始構建 BLE 醫療數據接收器...

REM 檢查 Node.js
echo 📋 檢查環境...
node -v
if %errorlevel% neq 0 (
    echo ❌ 錯誤: 未找到 Node.js
    exit /b 1
)

REM 安裝依賴
echo 📦 安裝依賴...
call npm install
if %errorlevel% neq 0 (
    echo ❌ 錯誤: 依賴安裝失敗
    exit /b 1
)

REM 檢查 TypeScript
echo 🔍 檢查 TypeScript...
call npx tsc --noEmit
if %errorlevel% neq 0 (
    echo ❌ 錯誤: TypeScript 檢查失敗
    exit /b 1
)

REM 構建 Android
if "%1"=="android" (
    echo 🤖 構建 Android 版本...
    
    if "%ANDROID_HOME%"=="" (
        echo ❌ 錯誤: 未設置 ANDROID_HOME 環境變量
        exit /b 1
    )
    
    cd android
    call gradlew clean
    call gradlew assembleRelease
    cd ..
    
    echo ✅ Android APK 構建完成
    echo 📍 位置: android\app\build\outputs\apk\release\
)

echo 🎉 構建完成！
echo.
echo 📊 構建摘要:
echo - 平台: %1
echo - 時間: %date% %time%
echo.
echo 📱 安裝說明:
echo Android: adb install android\app\build\outputs\apk\release\app-release.apk