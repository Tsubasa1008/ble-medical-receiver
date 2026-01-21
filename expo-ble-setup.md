# Expo BLE 開發構建設置

## ⚠️ 重要說明
Expo Go 應用無法測試真實 BLE 功能，需要創建開發構建。

## 🛠️ 創建 Expo 開發構建

### 1. 安裝 EAS CLI
```bash
npm install -g @expo/eas-cli
```

### 2. 配置 app.json
```json
{
  "expo": {
    "name": "BLE Medical Receiver",
    "slug": "ble-medical-receiver",
    "plugins": [
      [
        "expo-bluetooth",
        {
          "isBackgroundEnabled": true,
          "modes": ["peripheral", "central"]
        }
      ]
    ]
  }
}
```

### 3. 安裝 BLE 依賴
```bash
npx expo install expo-bluetooth
npx expo install expo-device
```

### 4. 創建開發構建
```bash
# Android
eas build --profile development --platform android

# iOS (需要 Apple 開發者帳號)
eas build --profile development --platform ios
```

### 5. 安裝到設備
構建完成後會提供下載鏈接，安裝到手機即可測試真實 BLE 功能。

## ⏱️ 預估時間
- 設置: 30-60 分鐘
- 構建: 10-20 分鐘
- 總計: 1-2 小時

## 💰 費用
- Expo 開發構建: 免費 (有限制)
- iOS 需要 Apple 開發者帳號: $99/年