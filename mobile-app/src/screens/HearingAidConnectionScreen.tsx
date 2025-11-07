import React, { useState, useEffect } from 'react';
import { View, StyleSheet, FlatList, Alert } from 'react-native';
import { Button, Text, Card, ActivityIndicator, List, IconButton } from 'react-native-paper';
import BLEService, { HearingAidDevice } from '../services/BLEService';

const HearingAidConnectionScreen: React.FC = () => {
  const [scanning, setScanning] = useState(false);
  const [devices, setDevices] = useState<HearingAidDevice[]>([]);
  const [connectedDevices, setConnectedDevices] = useState<HearingAidDevice[]>([]);

  useEffect(() => {
    initializeBLE();
    return () => {
      BLEService.stopScan();
    };
  }, []);

  const initializeBLE = async () => {
    try {
      await BLEService.initialize();
      const hasPermission = await BLEService.requestPermissions();
      if (!hasPermission) {
        Alert.alert('Permission Required', 'Bluetooth permissions are required to connect to hearing aids.');
      }

      // Load connected devices
      const connected = BLEService.getConnectedDevices();
      setConnectedDevices(connected);
    } catch (error) {
      Alert.alert('Error', 'Failed to initialize Bluetooth');
    }
  };

  const startScan = async () => {
    setScanning(true);
    setDevices([]);

    try {
      await BLEService.scanForHearingAids(
        (device) => {
          setDevices((prev) => {
            const exists = prev.find((d) => d.id === device.id);
            if (exists) return prev;
            return [...prev, device];
          });
        },
        15000 // Scan for 15 seconds
      );
    } catch (error) {
      Alert.alert('Error', 'Failed to scan for devices');
    } finally {
      setTimeout(() => setScanning(false), 15000);
    }
  };

  const connectToDevice = async (device: HearingAidDevice) => {
    try {
      const connected = await BLEService.connectToDevice(device.id);
      setConnectedDevices((prev) => [...prev, connected]);
      setDevices((prev) => prev.filter((d) => d.id !== device.id));
      Alert.alert('Success', `Connected to ${device.name}`);
    } catch (error) {
      Alert.alert('Error', `Failed to connect to ${device.name}`);
    }
  };

  const disconnectDevice = async (deviceId: string) => {
    try {
      await BLEService.disconnectDevice(deviceId);
      setConnectedDevices((prev) => prev.filter((d) => d.id !== deviceId));
      Alert.alert('Success', 'Device disconnected');
    } catch (error) {
      Alert.alert('Error', 'Failed to disconnect device');
    }
  };

  const getProtocolBadge = (protocol: string) => {
    const colors: { [key: string]: string } = {
      ASHA: '#4CAF50',
      MFi: '#2196F3',
      Generic: '#9E9E9E',
    };
    return (
      <View style={[styles.badge, { backgroundColor: colors[protocol] || '#9E9E9E' }]}>
        <Text style={styles.badgeText}>{protocol}</Text>
      </View>
    );
  };

  return (
    <View style={styles.container}>
      <Text variant="headlineMedium" style={styles.title}>
        Hearing Aid Connection
      </Text>

      <Text variant="bodyMedium" style={styles.subtitle}>
        Connect to your ASHA or MFi compatible hearing aids
      </Text>

      {/* Connected Devices */}
      {connectedDevices.length > 0 && (
        <View style={styles.section}>
          <Text variant="titleLarge" style={styles.sectionTitle}>
            Connected Devices
          </Text>
          {connectedDevices.map((device) => (
            <Card key={device.id} style={styles.card}>
              <Card.Content>
                <View style={styles.deviceRow}>
                  <View style={styles.deviceInfo}>
                    <Text variant="titleMedium">{device.name}</Text>
                    <View style={styles.deviceMeta}>
                      {getProtocolBadge(device.protocol)}
                      <Text style={styles.statusText}>● Connected</Text>
                    </View>
                  </View>
                  <IconButton
                    icon="bluetooth-off"
                    size={24}
                    onPress={() => disconnectDevice(device.id)}
                  />
                </View>
              </Card.Content>
            </Card>
          ))}
        </View>
      )}

      {/* Scan Section */}
      <View style={styles.section}>
        <Button
          mode="contained"
          onPress={startScan}
          disabled={scanning}
          icon="bluetooth-audio"
          style={styles.scanButton}
        >
          {scanning ? 'Scanning...' : 'Scan for Devices'}
        </Button>

        {scanning && (
          <View style={styles.scanningIndicator}>
            <ActivityIndicator size="large" />
            <Text style={styles.scanningText}>Searching for hearing aids...</Text>
          </View>
        )}
      </View>

      {/* Available Devices */}
      {devices.length > 0 && (
        <View style={styles.section}>
          <Text variant="titleLarge" style={styles.sectionTitle}>
            Available Devices
          </Text>
          <FlatList
            data={devices}
            keyExtractor={(item) => item.id}
            renderItem={({ item }) => (
              <Card style={styles.card} onPress={() => connectToDevice(item)}>
                <Card.Content>
                  <View style={styles.deviceRow}>
                    <View style={styles.deviceInfo}>
                      <Text variant="titleMedium">{item.name}</Text>
                      <View style={styles.deviceMeta}>
                        {getProtocolBadge(item.protocol)}
                      </View>
                    </View>
                    <IconButton icon="bluetooth-connect" size={24} />
                  </View>
                </Card.Content>
              </Card>
            )}
          />
        </View>
      )}

      {!scanning && devices.length === 0 && connectedDevices.length === 0 && (
        <View style={styles.emptyState}>
          <Text variant="bodyLarge" style={styles.emptyText}>
            No devices found. Make sure your hearing aids are in pairing mode.
          </Text>
        </View>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    backgroundColor: '#f5f5f5',
  },
  title: {
    textAlign: 'center',
    marginBottom: 10,
  },
  subtitle: {
    textAlign: 'center',
    marginBottom: 20,
    color: '#666',
  },
  section: {
    marginBottom: 20,
  },
  sectionTitle: {
    marginBottom: 10,
  },
  card: {
    marginBottom: 10,
  },
  deviceRow: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
  },
  deviceInfo: {
    flex: 1,
  },
  deviceMeta: {
    flexDirection: 'row',
    alignItems: 'center',
    marginTop: 5,
  },
  badge: {
    paddingHorizontal: 8,
    paddingVertical: 2,
    borderRadius: 4,
    marginRight: 10,
  },
  badgeText: {
    color: '#fff',
    fontSize: 12,
    fontWeight: 'bold',
  },
  statusText: {
    color: '#4CAF50',
    fontSize: 12,
  },
  scanButton: {
    marginBottom: 10,
  },
  scanningIndicator: {
    alignItems: 'center',
    marginTop: 20,
  },
  scanningText: {
    marginTop: 10,
    color: '#666',
  },
  emptyState: {
    alignItems: 'center',
    marginTop: 40,
  },
  emptyText: {
    textAlign: 'center',
    color: '#666',
  },
});

export default HearingAidConnectionScreen;
