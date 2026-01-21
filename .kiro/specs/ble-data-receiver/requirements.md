# Requirements Document

## Introduction

本系統是一個基於.NET 8的Console應用程式，用於接收和處理來自BLE (Bluetooth Low Energy) 設備的數值數據。系統將提供穩定的BLE連接管理、數據接收處理和用戶友好的控制台界面。

## Glossary

- **BLE_Receiver**: 負責BLE設備連接和數據接收的核心系統
- **Console_Interface**: 提供用戶交互的控制台界面組件
- **Data_Processor**: 處理和驗證接收到的BLE數據的組件
- **Connection_Manager**: 管理BLE設備連接狀態和重連邏輯的組件
- **Pairing_Manager**: 管理BLE設備自動配對功能的組件
- **BLE_Device**: 支持Bluetooth Low Energy協議的外部硬件設備
- **Blood_Pressure_Monitor**: 支持BLE的血壓計設備
- **Thermometer**: 支持BLE的額溫計設備
- **Characteristic**: BLE設備上可讀寫的數據端點
- **Service**: BLE設備上包含多個Characteristic的邏輯分組
- **Advertisement**: BLE設備發送的廣播訊息

## Requirements

### Requirement 1: BLE設備廣播監聽和自動配對

**User Story:** 作為用戶，我希望應用程式能夠監聽BLE設備廣播並自動配對未配對的設備，以便無縫接收醫療設備數據。

#### Acceptance Criteria

1. WHEN 應用程式啟動時，THE BLE_Receiver SHALL 持續監聽BLE設備廣播訊息
2. WHEN 接收到廣播訊息時，THE Pairing_Manager SHALL 檢查設備是否已配對
3. WHEN 發現未配對的血壓計或額溫計時，THE Pairing_Manager SHALL 自動嘗試配對該設備
4. WHEN 配對成功時，THE Connection_Manager SHALL 建立連接並開始接收數據
5. IF 配對失敗，THEN THE Pairing_Manager SHALL 記錄錯誤並繼續監聽其他設備

### Requirement 2: 醫療設備數據接收和處理

**User Story:** 作為醫護人員，我希望能夠接收來自血壓計和額溫計的測量數據，以便進行健康監測。

#### Acceptance Criteria

1. WHEN Blood_Pressure_Monitor 發送測量數據時，THE BLE_Receiver SHALL 接收收縮壓、舒張壓和心率數值
2. WHEN Thermometer 發送測量數據時，THE BLE_Receiver SHALL 接收體溫數值
3. WHEN 接收到數據時，THE Data_Processor SHALL 驗證數據格式和數值範圍的有效性
4. WHEN 數據有效時，THE Console_Interface SHALL 即時顯示設備類型、測量數值和時間戳
5. WHEN 數據無效時，THE Data_Processor SHALL 記錄錯誤並繼續處理後續數據

### Requirement 3: 連接狀態管理

**User Story:** 作為用戶，我希望系統能夠管理BLE連接狀態，以便在連接中斷時自動重連。

#### Acceptance Criteria

1. WHEN BLE連接中斷時，THE Connection_Manager SHALL 檢測到連接狀態變化
2. WHEN 檢測到連接中斷時，THE Connection_Manager SHALL 自動嘗試重新連接
3. WHILE 重連進行中時，THE Console_Interface SHALL 顯示重連狀態
4. IF 重連失敗超過3次，THEN THE Connection_Manager SHALL 停止自動重連並通知用戶
5. WHEN 重連成功時，THE BLE_Receiver SHALL 恢復數據接收功能

### Requirement 4: 用戶界面和控制

**User Story:** 作為用戶，我希望有清晰的控制台界面來監控系統狀態和控制程式執行。

#### Acceptance Criteria

1. WHEN 程式啟動時，THE Console_Interface SHALL 顯示歡迎信息和操作說明
2. WHILE 程式運行時，THE Console_Interface SHALL 即時顯示連接狀態和接收到的數據
3. WHEN 用戶按下Ctrl+C時，THE Console_Interface SHALL 優雅地終止程式
4. WHEN 程式終止時，THE Connection_Manager SHALL 正確關閉所有BLE連接
5. THE Console_Interface SHALL 使用清晰的格式顯示時間戳、數據值和狀態信息

### Requirement 5: 錯誤處理和日誌記錄

**User Story:** 作為開發者，我希望系統能夠妥善處理錯誤並提供詳細的日誌信息，以便調試和維護。

#### Acceptance Criteria

1. WHEN 發生BLE相關錯誤時，THE BLE_Receiver SHALL 記錄詳細的錯誤信息
2. WHEN 數據解析失敗時，THE Data_Processor SHALL 記錄原始數據和錯誤原因
3. WHEN 連接異常時，THE Connection_Manager SHALL 記錄連接狀態變化和錯誤詳情
4. THE Console_Interface SHALL 向用戶顯示適當級別的錯誤信息
5. THE BLE_Receiver SHALL 在遇到致命錯誤時優雅地終止程式

### Requirement 6: 醫療設備特定數據格式支持

**User Story:** 作為用戶，我希望系統能夠正確解析血壓計和額溫計的特定數據格式，以便獲得準確的測量結果。

#### Acceptance Criteria

1. WHEN 接收到血壓計數據時，THE Data_Processor SHALL 解析收縮壓、舒張壓和心率三個數值
2. WHEN 接收到額溫計數據時，THE Data_Processor SHALL 解析體溫數值並支持攝氏度格式
3. WHEN 血壓數值超出正常範圍時，THE Console_Interface SHALL 顯示警告標記
4. WHEN 體溫數值超出正常範圍時，THE Console_Interface SHALL 顯示警告標記
5. THE Data_Processor SHALL 支持IEEE 11073標準的醫療設備數據格式