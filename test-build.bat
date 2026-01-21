@echo off
echo 🔧 測試 Android APK 構建...
echo.

echo 📦 安裝依賴...
call npm install --legacy-peer-deps
if %errorlevel% neq 0 (
    echo ❌ 依賴安裝失敗
    pause
    exit /b 1
)

echo.
echo 🧹 清理項目...
cd android
call gradlew clean
if %errorlevel% neq 0 (
    echo ❌ 清理失敗
    cd ..
    pause
    exit /b 1
)

echo.
echo 🏗️ 構建 APK...
call gradlew assembleRelease --no-daemon --stacktrace
if %errorlevel% neq 0 (
    echo ❌ APK 構建失敗
    cd ..
    pause
    exit /b 1
)

cd ..
echo.
echo ✅ APK 構建成功！
echo 📱 APK 位置: android\app\build\outputs\apk\release\app-release.apk
echo.
pause