import React from 'react';
import { View, Text, StyleSheet } from 'react-native';
import { Chip } from 'react-native-paper';
import Icon from 'react-native-vector-icons/MaterialIcons';
import { ConnectionStatus as Status } from '../types';

interface ConnectionStatusProps {
  status: Status;
  deviceCount: number;
}

export const ConnectionStatus: React.FC<ConnectionStatusProps> = ({
  status,
  deviceCount,
}) => {
  const getStatusConfig = (status: Status) => {
    switch (status) {
      case Status.CONNECTED:
        return {
          icon: 'bluetooth-connected',
          color: '#4CAF50',
          backgroundColor: '#E8F5E8',
          text: '已連接',
        };
      case Status.CONNECTING:
        return {
          icon: 'bluetooth-searching',
          color: '#FF9800',
          backgroundColor: '#FFF3E0',
          text: '連接中',
        };
      case Status.DISCONNECTING:
        return {
          icon: 'bluetooth-disabled',
          color: '#FF5722',
          backgroundColor: '#FFEBEE',
          text: '斷開中',
        };
      default:
        return {
          icon: 'bluetooth',
          color: '#757575',
          backgroundColor: '#F5F5F5',
          text: '未連接',
        };
    }
  };

  const config = getStatusConfig(status);

  return (
    <View style={styles.container}>
      <Chip
        mode="flat"
        style={[styles.statusChip, { backgroundColor: config.backgroundColor }]}
        textStyle={[styles.statusText, { color: config.color }]}
        icon={() => (
          <Icon name={config.icon} size={16} color={config.color} />
        )}
      >
        {config.text}
      </Chip>
      
      {deviceCount > 0 && (
        <Text style={styles.deviceCount}>
          {deviceCount} 個設備
        </Text>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flexDirection: 'row',
    alignItems: 'center',
    paddingHorizontal: 16,
    paddingVertical: 8,
  },
  statusChip: {
    marginRight: 8,
  },
  statusText: {
    fontSize: 12,
    fontWeight: '500',
  },
  deviceCount: {
    fontSize: 12,
    color: '#757575',
  },
});