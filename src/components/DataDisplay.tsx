import React from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  Animated,
} from 'react-native';
import { Card, Chip, Divider } from 'react-native-paper';
import { MedicalData, TemperatureData, BloodPressureData, DeviceType } from '../types';

interface DataDisplayProps {
  data: MedicalData[];
  onDataPress?: (data: MedicalData) => void;
}

export const DataDisplay: React.FC<DataDisplayProps> = ({
  data,
  onDataPress,
}) => {
  const formatTimestamp = (timestamp: Date): string => {
    return new Intl.DateTimeFormat('zh-TW', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false,
    }).format(timestamp);
  };

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

  const getValidityColor = (isValid: boolean): string => {
    return isValid ? '#4CAF50' : '#F44336';
  };

  const getValidityIcon = (isValid: boolean): string => {
    return isValid ? '✅' : '❌';
  };

  const renderTemperatureData = (data: TemperatureData) => (
    <View style={styles.dataDetails}>
      <View style={styles.detailRow}>
        <Text style={styles.detailIcon}>🌡️</Text>
        <Text style={styles.detailText}>
          體溫: {data.temperature.toFixed(1)}°{data.unit}
        </Text>
      </View>
      
      <View style={styles.detailRow}>
        <Text style={styles.detailIcon}>📊</Text>
        <Text style={styles.detailText}>
          範圍: {data.temperature >= 36.1 && data.temperature <= 37.2 ? '正常' : '異常'}
        </Text>
      </View>
      
      {data.isFever && (
        <Chip 
          mode="flat" 
          textStyle={styles.warningChipText}
          style={styles.warningChip}
        >
          ⚠️ 發燒警告
        </Chip>
      )}
    </View>
  );

  const renderBloodPressureData = (data: BloodPressureData) => (
    <View style={styles.dataDetails}>
      <View style={styles.detailRow}>
        <Text style={styles.detailIcon}>💓</Text>
        <Text style={styles.detailText}>
          收縮壓: {data.systolicPressure.toFixed(0)} mmHg
        </Text>
      </View>
      
      <View style={styles.detailRow}>
        <Text style={styles.detailIcon}>💗</Text>
        <Text style={styles.detailText}>
          舒張壓: {data.diastolicPressure.toFixed(0)} mmHg
        </Text>
      </View>
      
      <View style={styles.detailRow}>
        <Text style={styles.detailIcon}>❤️</Text>
        <Text style={styles.detailText}>
          心率: {data.heartRate} bpm
        </Text>
      </View>
      
      {data.isHypertensive && (
        <Chip 
          mode="flat" 
          textStyle={styles.warningChipText}
          style={styles.warningChip}
        >
          ⚠️ 高血壓警告
        </Chip>
      )}
    </View>
  );

  const renderDataItem = ({ item, index }: { item: MedicalData; index: number }) => {
    const animatedValue = new Animated.Value(0);
    
    React.useEffect(() => {
      Animated.timing(animatedValue, {
        toValue: 1,
        duration: 300,
        delay: index * 100,
        useNativeDriver: true,
      }).start();
    }, []);

    return (
      <Animated.View
        style={[
          styles.cardContainer,
          {
            opacity: animatedValue,
            transform: [
              {
                translateY: animatedValue.interpolate({
                  inputRange: [0, 1],
                  outputRange: [50, 0],
                }),
              },
            ],
          },
        ]}
      >
        <Card style={styles.dataCard} onPress={() => onDataPress?.(item)}>
          <Card.Content>
            <View style={styles.cardHeader}>
              <View style={styles.deviceInfo}>
                <Text style={styles.deviceIcon}>
                  {getDeviceIcon(item.deviceType)}
                </Text>
                <View>
                  <Text style={styles.deviceName}>
                    {item.deviceType === DeviceType.THERMOMETER ? '體溫計' : '血壓計'}
                  </Text>
                  <Text style={styles.timestamp}>
                    {formatTimestamp(item.timestamp)}
                  </Text>
                </View>
              </View>
              
              <View style={styles.validityIndicator}>
                <Text style={[styles.validityIcon, { color: getValidityColor(item.isValid) }]}>
                  {getValidityIcon(item.isValid)}
                </Text>
              </View>
            </View>
            
            <Divider style={styles.divider} />
            
            {item.deviceType === DeviceType.THERMOMETER 
              ? renderTemperatureData(item as TemperatureData)
              : renderBloodPressureData(item as BloodPressureData)
            }
          </Card.Content>
        </Card>
      </Animated.View>
    );
  };

  const renderEmpty = () => (
    <View style={styles.emptyContainer}>
      <Text style={styles.emptyIcon}>📊</Text>
      <Text style={styles.emptyTitle}>暫無數據</Text>
      <Text style={styles.emptySubtitle}>
        連接醫療設備並進行測量以查看數據
      </Text>
    </View>
  );

  return (
    <View style={styles.container}>
      <FlatList
        data={data}
        renderItem={renderDataItem}
        keyExtractor={(item, index) => `${item.deviceId}-${item.timestamp.getTime()}-${index}`}
        contentContainerStyle={styles.listContainer}
        showsVerticalScrollIndicator={false}
        ListEmptyComponent={renderEmpty}
      />
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#F5F5F5',
  },
  listContainer: {
    padding: 16,
    flexGrow: 1,
  },
  cardContainer: {
    marginBottom: 12,
  },
  dataCard: {
    backgroundColor: '#FFFFFF',
    elevation: 2,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.22,
    shadowRadius: 2.22,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 8,
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
  deviceName: {
    fontSize: 16,
    fontWeight: '600',
    color: '#212121',
  },
  timestamp: {
    fontSize: 12,
    color: '#757575',
    marginTop: 2,
  },
  validityIndicator: {
    alignItems: 'center',
  },
  validityIcon: {
    fontSize: 20,
  },
  divider: {
    marginVertical: 12,
    backgroundColor: '#E0E0E0',
  },
  dataDetails: {
    gap: 8,
  },
  detailRow: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  detailIcon: {
    fontSize: 16,
    marginRight: 8,
    width: 20,
  },
  detailText: {
    fontSize: 14,
    color: '#424242',
    flex: 1,
  },
  warningChip: {
    backgroundColor: '#FFF3E0',
    marginTop: 8,
    alignSelf: 'flex-start',
  },
  warningChipText: {
    color: '#E65100',
    fontSize: 12,
  },
  emptyContainer: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    paddingVertical: 64,
  },
  emptyIcon: {
    fontSize: 64,
    color: '#BDBDBD',
    marginBottom: 16,
  },
  emptyTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#757575',
    marginBottom: 8,
  },
  emptySubtitle: {
    fontSize: 14,
    color: '#9E9E9E',
    textAlign: 'center',
    paddingHorizontal: 32,
  },
});