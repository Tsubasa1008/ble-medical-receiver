import React from 'react';
import {
  View,
  Text,
  FlatList,
  StyleSheet,
  Animated,
} from 'react-native';
import { Card, Chip, Divider } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialIcons';
import { MedicalData, TemperatureData, BloodPressureData, DeviceType } from '../types';

interface DataDisplayProps {
  data: MedicalData[];
  onDataPress?: (data: MedicalData) => void;
}

export const DataDisplay: React.FC<DataDisplayProps> = ({
  data,
  onDataPress,
}) => {
  const fadeAnim = React.useRef(new Animated.Value(0)).current;

  React.useEffect(() => {
    if (data.length > 0) {
      Animated.timing(fadeAnim, {
        toValue: 1,
        duration: 300,
        useNativeDriver: true,
      }).start();
    }
  }, [data.length]);

  const formatTimestamp = (timestamp: Date): string => {
    return new Intl.DateTimeFormat('zh-TW', {
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
    }).format(timestamp);
  };

  const getDeviceIcon = (deviceType: DeviceType): string => {
    switch (deviceType) {
      case DeviceType.THERMOMETER:
        return 'thermostat';
      case DeviceType.BLOOD_PRESSURE_MONITOR:
        return 'favorite';
      default:
        return 'device-unknown';
    }
  };

  const getValidityColor = (isValid: boolean): string => {
    return isValid ? '#4CAF50' : '#F44336';
  };

  const getValidityIcon = (isValid: boolean): string => {
    return isValid ? 'check-circle' : 'error';
  };

  const renderTemperatureData = (tempData: TemperatureData) => (
    <View style={styles.dataContent}>
      <View style={styles.primaryValue}>
        <Text style={styles.valueNumber}>
          {tempData.temperature.toFixed(1)}
        </Text>
        <Text style={styles.valueUnit}>°{tempData.unit}</Text>
      </View>
      
      <View style={styles.dataDetails}>
        <View style={styles.detailRow}>
          <Icon name="thermostat" size={16} color="#757575" />
          <Text style={styles.detailText}>
            體溫: {tempData.temperature.toFixed(1)}°{tempData.unit}
          </Text>
        </View>
        
        <View style={styles.detailRow}>
          <Icon name="assessment" size={16} color="#757575" />
          <Text style={styles.detailText}>
            範圍: {tempData.temperature >= 36.1 && tempData.temperature <= 37.2 ? '正常' : '異常'}
          </Text>
        </View>
      </View>
    </View>
  );

  const renderBloodPressureData = (bpData: BloodPressureData) => (
    <View style={styles.dataContent}>
      <View style={styles.primaryValue}>
        <Text style={styles.valueNumber}>
          {bpData.systolicPressure.toFixed(0)}/{bpData.diastolicPressure.toFixed(0)}
        </Text>
        <Text style={styles.valueUnit}>mmHg</Text>
      </View>
      
      <View style={styles.dataDetails}>
        <View style={styles.detailRow}>
          <Icon name="favorite" size={16} color="#757575" />
          <Text style={styles.detailText}>
            收縮壓: {bpData.systolicPressure.toFixed(0)} mmHg
          </Text>
        </View>
        
        <View style={styles.detailRow}>
          <Icon name="favorite-border" size={16} color="#757575" />
          <Text style={styles.detailText}>
            舒張壓: {bpData.diastolicPressure.toFixed(0)} mmHg
          </Text>
        </View>
        
        <View style={styles.detailRow}>
          <Icon name="monitor-heart" size={16} color="#757575" />
          <Text style={styles.detailText}>
            心率: {bpData.heartRate} bpm
          </Text>
        </View>
      </View>
    </View>
  );

  const renderDataItem = ({ item, index }: { item: MedicalData; index: number }) => (
    <Animated.View
      style={[
        styles.dataCard,
        {
          opacity: fadeAnim,
          transform: [
            {
              translateY: fadeAnim.interpolate({
                inputRange: [0, 1],
                outputRange: [50, 0],
              }),
            },
          ],
        },
      ]}
    >
      <Card
        mode="outlined"
        style={[
          styles.card,
          { borderLeftColor: getValidityColor(item.isValid), borderLeftWidth: 4 }
        ]}
        onPress={() => onDataPress?.(item)}
      >
        <View style={styles.cardHeader}>
          <View style={styles.deviceInfo}>
            <Icon
              name={getDeviceIcon(item.deviceType)}
              size={24}
              color="#2196F3"
            />
            <View style={styles.deviceDetails}>
              <Text style={styles.deviceType}>
                {item.deviceType === DeviceType.THERMOMETER ? '體溫計' : '血壓計'}
              </Text>
              <Text style={styles.timestamp}>
                {formatTimestamp(item.timestamp)}
              </Text>
            </View>
          </View>
          
          <View style={styles.validityIndicator}>
            <Icon
              name={getValidityIcon(item.isValid)}
              size={20}
              color={getValidityColor(item.isValid)}
            />
            <Chip
              mode="flat"
              compact
              style={[
                styles.validityChip,
                { backgroundColor: item.isValid ? '#E8F5E8' : '#FFEBEE' }
              ]}
              textStyle={[
                styles.validityText,
                { color: getValidityColor(item.isValid) }
              ]}
            >
              {item.isValid ? '有效' : '無效'}
            </Chip>
          </View>
        </View>

        <Divider style={styles.divider} />

        {item.deviceType === DeviceType.THERMOMETER
          ? renderTemperatureData(item as TemperatureData)
          : renderBloodPressureData(item as BloodPressureData)
        }
      </Card>
    </Animated.View>
  );

  const renderEmpty = () => (
    <View style={styles.emptyContainer}>
      <Icon name="insert-chart" size={64} color="#BDBDBD" />
      <Text style={styles.emptyTitle}>暫無數據</Text>
      <Text style={styles.emptySubtitle}>
        連接醫療設備並進行測量以查看數據
      </Text>
    </View>
  );

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.headerTitle}>測量數據</Text>
        <Chip mode="outlined" compact>
          {data.length} 筆記錄
        </Chip>
      </View>

      <FlatList
        data={data}
        renderItem={renderDataItem}
        keyExtractor={(item, index) => `${item.deviceId}-${index}`}
        contentContainerStyle={styles.listContainer}
        ListEmptyComponent={renderEmpty}
        showsVerticalScrollIndicator={false}
        inverted // 最新數據在頂部
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
  dataCard: {
    marginBottom: 12,
  },
  card: {
    backgroundColor: '#FFFFFF',
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 16,
    paddingBottom: 8,
  },
  deviceInfo: {
    flexDirection: 'row',
    alignItems: 'center',
    flex: 1,
  },
  deviceDetails: {
    marginLeft: 12,
  },
  deviceType: {
    fontSize: 16,
    fontWeight: '500',
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
  validityChip: {
    marginTop: 4,
  },
  validityText: {
    fontSize: 10,
    fontWeight: '500',
  },
  divider: {
    marginHorizontal: 16,
  },
  dataContent: {
    padding: 16,
    paddingTop: 12,
  },
  primaryValue: {
    flexDirection: 'row',
    alignItems: 'baseline',
    justifyContent: 'center',
    marginBottom: 16,
  },
  valueNumber: {
    fontSize: 32,
    fontWeight: '700',
    color: '#2196F3',
  },
  valueUnit: {
    fontSize: 16,
    fontWeight: '500',
    color: '#757575',
    marginLeft: 4,
  },
  dataDetails: {
    backgroundColor: '#F8F9FA',
    borderRadius: 8,
    padding: 12,
  },
  detailRow: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 8,
  },
  detailText: {
    fontSize: 14,
    color: '#424242',
    marginLeft: 8,
    flex: 1,
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
});