#!/bin/bash

# BLE 醫療數據接收器 - 構建腳本

set -e

echo "🚀 開始構建 BLE 醫療數據接收器..."

# 檢查 Node.js 版本
echo "📋 檢查環境..."
node_version=$(node -v)
echo "Node.js 版本: $node_version"

if [[ "$node_version" < "v16" ]]; then
    echo "❌ 錯誤: 需要 Node.js 16 或更高版本"
    exit 1
fi

# 安裝依賴
echo "📦 安裝依賴..."
npm install

# 檢查 TypeScript
echo "🔍 檢查 TypeScript..."
npx tsc --noEmit

# 運行測試
echo "🧪 運行測試..."
npm test -- --watchAll=false

# 構建 Android
if [[ "$1" == "android" || "$1" == "all" ]]; then
    echo "🤖 構建 Android 版本..."
    
    # 檢查 Android SDK
    if [[ -z "$ANDROID_HOME" ]]; then
        echo "❌ 錯誤: 未設置 ANDROID_HOME 環境變量"
        exit 1
    fi
    
    # 清理構建
    cd android
    ./gradlew clean
    cd ..
    
    # 構建 APK
    npx react-native build-android --mode=release
    
    echo "✅ Android APK 構建完成"
    echo "📍 位置: android/app/build/outputs/apk/release/"
fi

# 構建 iOS
if [[ "$1" == "ios" || "$1" == "all" ]]; then
    echo "🍎 構建 iOS 版本..."
    
    # 檢查 Xcode
    if ! command -v xcodebuild &> /dev/null; then
        echo "❌ 錯誤: 未找到 Xcode"
        exit 1
    fi
    
    # 安裝 CocoaPods
    cd ios
    pod install
    cd ..
    
    # 構建 iOS
    npx react-native build-ios --mode=Release
    
    echo "✅ iOS 構建完成"
    echo "📍 位置: ios/build/Build/Products/Release-iphoneos/"
fi

echo "🎉 構建完成！"

# 顯示構建信息
echo ""
echo "📊 構建摘要:"
echo "- 平台: $1"
echo "- 時間: $(date)"
echo "- Node.js: $node_version"
echo ""
echo "📱 安裝說明:"
echo "Android: adb install android/app/build/outputs/apk/release/app-release.apk"
echo "iOS: 使用 Xcode 或 TestFlight 安裝"