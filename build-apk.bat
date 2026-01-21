@echo off
echo 🚀 開始構建 BLE 醫療數據接收器 APK...

REM 檢查環境
echo 📋 檢查構建環境...

REM 檢查 Node.js
node -v
if %errorlevel% neq 0 (
    echo ❌ 錯誤: 未找到 Node.js
    pause
    exit /b 1
)

REM 檢查 Java
java -version
if %errorlevel% neq 0 (
    echo ❌ 錯誤: 未找到 Java JDK
    echo 請安裝 Java JDK 11 或更高版本
    pause
    exit /b 1
)

REM 檢查 Android SDK
if "%ANDROID_HOME%"=="" (
    echo ❌ 錯誤: 未設置 ANDROID_HOME 環境變量
    echo 請安裝 Android Studio 並設置 ANDROID_HOME
    pause
    exit /b 1
)

echo ✅ 環境檢查通過

REM 安裝依賴
echo 📦 安裝依賴...
call npm install --legacy-peer-deps
if %errorlevel% neq 0 (
    echo ❌ 錯誤: 依賴安裝失敗
    pause
    exit /b 1
)

REM 清理構建
echo 🧹 清理之前的構建...
call npx react-native clean
cd android
call gradlew clean
cd ..

REM 構建 APK
echo 🔨 構建 APK...
cd android
call gradlew assembleRelease
if %errorlevel% neq 0 (
    echo ❌ 錯誤: APK 構建失敗
    cd ..
    pause
    exit /b 1
)
cd ..

echo ✅ APK 構建成功！

REM 顯示 APK 位置
echo.
echo 📱 APK 文件位置:
echo android\app\build\outputs\apk\release\app-release.apk
echo.

REM 檢查文件是否存在
if exist "android\app\build\outputs\apk\release\app-release.apk" (
    echo ✅ APK 文件已生成
    echo 📊 文件大小:
    dir "android\app\build\outputs\apk\release\app-release.apk" | find "app-release.apk"
) else (
    echo ❌ APK 文件未找到
)

echo.
echo 📱 安裝說明:
echo 1. 將 APK 文件傳輸到 Android 手機
echo 2. 在手機上啟用"未知來源"安裝
echo 3. 點擊 APK 文件進行安裝
echo 4. 授予藍牙和位置權限
echo 5. 使用 FORA IR40 體溫計進行測試

pause