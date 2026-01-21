# Expo 測試設置

## 1. 安裝 Expo CLI
```bash
npm install -g @expo/cli
```

## 2. 初始化 Expo 項目
```bash
npx create-expo-app BLEMedicalReceiver --template blank-typescript
cd BLEMedicalReceiver
```

## 3. 安裝依賴
```bash
npm install expo-bluetooth expo-permissions
npm install react-native-paper react-native-vector-icons
```

## 4. 在手機上安裝 Expo Go
- iOS: App Store 搜索 "Expo Go"
- Android: Google Play 搜索 "Expo Go"

## 5. 運行項目
```bash
npx expo start
```

## 6. 掃描 QR 碼
用 Expo Go 掃描終端顯示的 QR 碼即可在手機上運行