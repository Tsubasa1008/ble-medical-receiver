import React from 'react';
import {
  View,
  Text,
  FlatList,
  TouchableOpacity,
  StyleSheet,
  RefreshControl,
} from 'react-native';
import { Card, Button, Chip, ActivityIndicator } from 'react-native-paper';
import { BLEDevice, DeviceType } from '../types';

interface DeviceListProps {
  devices: BLEDevice[];
  isScanning: boolean;
  onDevicePress: (device: BLEDevice) => void;
  onRefresh: () => void;
}

export const DeviceList: React.FC<DeviceListProps> = ({
  devices,
  isScanning,
  onDevicePress,
  onRefresh,
}) => {
  const getDeviceIcon = (deviceType: DeviceType): string => {
    switch (deviceType) {
      case DeviceType.THERMOMETER:
        return '🌡️';
      case DeviceType.BLOOD_PRESSURE_MONITOR:
        return '🩺';
      default:
        return '📱';
    }
  };

  const getDeviceTypeText = (deviceType: DeviceType): string => {
    switch (deviceType) {
      case DeviceType.THERMOMETER:
        return '體溫計';
      case DeviceType.BLOOD_PRESSURE_MONITOR:
        return '血壓計';
      default:
        return '未知設備';
    }
  };

  const getSignalStrength = (rssi: number): { text: string; color: string } => {
    if (rssi > -50) return { text: '📶', color: '#4CAF50' };
    if (rssi > -70) return { text: '📶', color: '#FF9800' };
    if (rssi > -90) return { text: '📶', color: '#FF5722' };
    return { text: '📶', color: '#F44336' };
  };

  const renderDevice = ({ item }: { item: BLEDevice }) => {
    const signal = getSignalStrength(item.rssi);
    
    return (
      <Card style={styles.deviceCard} mode="outlined">
        <TouchableOpacity
          onPress={() => onDevicePress(item)}
          style={styles.deviceContent}
        >
          <View style={styles.deviceHeader}>
            <View style={styles.deviceInfo}>
              <Text style={styles.deviceIcon}>
                {getDeviceIcon(item.deviceType)}
              </Text>
              <View style={styles.deviceDetails}>
                <Text style={styles.deviceName}>
                  {item.name || '未知設備'}
                </Text>
                <Text style={styles.deviceId}>
                  ID: {item.id.substring(0, 8)}...
                </Text>
              </View>
            </View>
            
            <View style={styles.deviceStatus}>
              <Text style={[styles.signalIcon, { color: signal.color }]}>
                {signal.text}
              </Text>
              <Text style={[styles.rssiText, { color: signal.color }]}>
                {item.rssi} dBm
              </Text>
            </View>
          </View>

          <View style={styles.deviceFooter}>
            <Chip
              mode="outlined"
              compact
              style={styles.deviceTypeChip}
            >
              {getDeviceTypeText(item.deviceType)}
            </Chip>
            
            {item.isConnected && (
              <Chip
                mode="flat"
                compact
                style={styles.connectedChip}
                textStyle={styles.connectedText}
              >
                已連接
              </Chip>
            )}
          </View>
        </TouchableOpacity>
      </Card>
    );
  };

  const renderEmpty = () => (
    <View style={styles.emptyContainer}>
      <Text style={styles.emptyIcon}>🔍</Text>
      <Text style={styles.emptyTitle}>
        {isScanning ? '正在搜尋設備...' : '未發現設備'}
      </Text>
      <Text style={styles.emptySubtitle}>
        {isScanning 
          ? '請確保您的醫療設備已開啟並處於配對模式'
          : '點擊重新整理開始搜尋'
        }
      </Text>
      {isScanning && (
        <ActivityIndicator
          size="large"
          color="#2196F3"
          style={styles.loadingIndicator}
        />
      )}
    </View>
  );

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>可用設備</Text>
        <Button
          mode="outlined"
          onPress={onRefresh}
          disabled={isScanning}
          icon="refresh"
          compact
        >
          {isScanning ? '搜尋中...' : '重新整理'}
        </Button>
      </View>

      <FlatList
        data={devices}
        renderItem={renderDevice}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContainer}
        refreshControl={
          <RefreshControl
            refreshing={isScanning}
            onRefresh={onRefresh}
            colors={['#2196F3']}
          />
        }
        ListEmptyComponent={renderEmpty}
        showsVerticalScrollIndicator={false}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F5F5F5',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 12,
    backgroundColor: '#FFFFFF',
    borderBottomWidth: 1,
    borderBottomColor: '#E0E0E0',
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#212121',
  },
  listContainer: {
    padding: 16,
    flexGrow: 1,
  },
  deviceCard: {
    marginBottom: 12,
    backgroundColor: '#FFFFFF',
  },
  deviceContent: {
    padding: 16,
  },
  deviceHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  deviceInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  deviceIcon: {
    fontSize: 24,
    marginRight: 12,
  },
  signalIcon: {
    fontSize: 16,
  },
  emptyIcon: {
    fontSize: 64,
    color: '#BDBDBD',
  },
  deviceDetails: {
    flex: 1,
  },
  deviceName: {
    fontSize: 16,
    fontWeight: '500',
    color: '#212121',
    marginBottom: 2,
  },
  deviceId: {
    fontSize: 12,
    color: '#757575',
  },
  deviceStatus: {
    alignItems: 'center',
  },
  rssiText: {
    fontSize: 10,
    marginTop: 2,
    fontWeight: '500',
  },
  deviceFooter: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  deviceTypeChip: {
    backgroundColor: '#E3F2FD',
  },
  connectedChip: {
    backgroundColor: '#E8F5E8',
  },
  connectedText: {
    color: '#2E7D32',
    fontSize: 12,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 64,
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '500',
    color: '#757575',
    marginTop: 16,
    marginBottom: 8,
  },
  emptySubtitle: {
    fontSize: 14,
    color: '#9E9E9E',
    textAlign: 'center',
    lineHeight: 20,
    paddingHorizontal: 32,
  },
  loadingIndicator: {
    marginTop: 24,
  },
});