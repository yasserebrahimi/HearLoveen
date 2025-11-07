import { BleManager, Device, State } from 'react-native-ble-plx';
import AsyncStorage from '@react-native-community/async-storage';

// ASHA (Audio Streaming for Hearing Aids) Protocol
// Service UUID: 0000FDF0-0000-1000-8000-00805f9b34fb
const ASHA_SERVICE_UUID = '0000FDF0-0000-1000-8000-00805f9b34fb';
const ASHA_READ_ONLY_PROPERTIES_UUID = '6333651e-c481-4a3e-9169-7c902aad37bb';
const ASHA_AUDIO_CONTROL_POINT_UUID = 'f0d4de7e-4a88-476c-9d9f-1937b0996cc0';
const ASHA_AUDIO_STATUS_UUID = '38663f1a-e711-4cac-b641-326b56404837';
const ASHA_VOLUME_UUID = '00e4ca9e-ab14-41e4-8823-f9e70c7e91df';

// MFi (Made for iPhone) Hearing Aid Protocol
// Service UUID: Proprietary Apple protocol
const MFI_SERVICE_UUID = '03B80E5A-EDE8-4B33-A751-6CE34EC4C700';

interface HearingAidDevice {
  id: string;
  name: string;
  protocol: 'ASHA' | 'MFi' | 'Generic';
  device: Device;
  connected: boolean;
  battery?: number;
}

class BLEService {
  private manager: BleManager;
  private connectedDevices: Map<string, HearingAidDevice> = new Map();
  private scanning: boolean = false;

  constructor() {
    this.manager = new BleManager();
  }

  async initialize(): Promise<void> {
    const state = await this.manager.state();
    if (state !== State.PoweredOn) {
      console.warn('Bluetooth is not powered on');
    }

    // Load previously connected devices
    const savedDevices = await AsyncStorage.getItem('connected_hearing_aids');
    if (savedDevices) {
      const devices = JSON.parse(savedDevices);
      // Attempt to reconnect
      for (const deviceId of devices) {
        try {
          await this.reconnectDevice(deviceId);
        } catch (error) {
          console.error(`Failed to reconnect to device ${deviceId}:`, error);
        }
      }
    }
  }

  async requestPermissions(): Promise<boolean> {
    try {
      const state = await this.manager.state();
      return state === State.PoweredOn;
    } catch (error) {
      console.error('Error requesting Bluetooth permissions:', error);
      return false;
    }
  }

  async scanForHearingAids(
    onDeviceFound: (device: HearingAidDevice) => void,
    durationMs: number = 10000
  ): Promise<void> {
    if (this.scanning) {
      console.warn('Already scanning');
      return;
    }

    this.scanning = true;
    const foundDevices = new Map<string, HearingAidDevice>();

    this.manager.startDeviceScan(
      [ASHA_SERVICE_UUID, MFI_SERVICE_UUID],
      { allowDuplicates: false },
      (error, device) => {
        if (error) {
          console.error('Scan error:', error);
          return;
        }

        if (device && device.name) {
          const protocol = this.detectProtocol(device);
          const hearingAidDevice: HearingAidDevice = {
            id: device.id,
            name: device.name,
            protocol,
            device,
            connected: false,
          };

          if (!foundDevices.has(device.id)) {
            foundDevices.set(device.id, hearingAidDevice);
            onDeviceFound(hearingAidDevice);
          }
        }
      }
    );

    // Stop scanning after specified duration
    setTimeout(() => {
      this.stopScan();
    }, durationMs);
  }

  stopScan(): void {
    this.manager.stopDeviceScan();
    this.scanning = false;
  }

  private detectProtocol(device: Device): 'ASHA' | 'MFi' | 'Generic' {
    // Check if device advertises ASHA service
    if (device.serviceUUIDs?.includes(ASHA_SERVICE_UUID)) {
      return 'ASHA';
    }
    // Check if device advertises MFi service
    if (device.serviceUUIDs?.includes(MFI_SERVICE_UUID)) {
      return 'MFi';
    }
    // Generic BLE hearing aid
    return 'Generic';
  }

  async connectToDevice(deviceId: string): Promise<HearingAidDevice> {
    try {
      const device = await this.manager.connectToDevice(deviceId);
      await device.discoverAllServicesAndCharacteristics();

      const protocol = this.detectProtocol(device);
      const hearingAidDevice: HearingAidDevice = {
        id: device.id,
        name: device.name || 'Unknown Device',
        protocol,
        device,
        connected: true,
      };

      // Initialize protocol-specific features
      if (protocol === 'ASHA') {
        await this.initializeASHA(device);
      } else if (protocol === 'MFi') {
        await this.initializeMFi(device);
      }

      this.connectedDevices.set(deviceId, hearingAidDevice);

      // Save connected device
      await this.saveConnectedDevices();

      // Monitor connection
      device.onDisconnected(() => {
        this.handleDeviceDisconnected(deviceId);
      });

      return hearingAidDevice;
    } catch (error) {
      console.error(`Failed to connect to device ${deviceId}:`, error);
      throw error;
    }
  }

  private async initializeASHA(device: Device): Promise<void> {
    try {
      // Read ASHA properties
      const properties = await device.readCharacteristicForService(
        ASHA_SERVICE_UUID,
        ASHA_READ_ONLY_PROPERTIES_UUID
      );

      console.log('ASHA Properties:', properties.value);

      // Subscribe to audio status notifications
      device.monitorCharacteristicForService(
        ASHA_SERVICE_UUID,
        ASHA_AUDIO_STATUS_UUID,
        (error, characteristic) => {
          if (error) {
            console.error('ASHA audio status error:', error);
            return;
          }
          console.log('ASHA Audio Status:', characteristic?.value);
        }
      );
    } catch (error) {
      console.error('Failed to initialize ASHA:', error);
    }
  }

  private async initializeMFi(device: Device): Promise<void> {
    try {
      // MFi protocol initialization
      // Note: Full MFi protocol requires Apple MFi certification and proprietary SDKs
      console.log('MFi device connected:', device.name);
    } catch (error) {
      console.error('Failed to initialize MFi:', error);
    }
  }

  async setVolume(deviceId: string, volume: number): Promise<void> {
    const hearingAid = this.connectedDevices.get(deviceId);
    if (!hearingAid || !hearingAid.connected) {
      throw new Error('Device not connected');
    }

    if (hearingAid.protocol === 'ASHA') {
      // ASHA volume control (0-127)
      const volumeValue = Math.max(0, Math.min(127, Math.round(volume * 127)));
      await hearingAid.device.writeCharacteristicWithResponseForService(
        ASHA_SERVICE_UUID,
        ASHA_VOLUME_UUID,
        Buffer.from([volumeValue]).toString('base64')
      );
    }
  }

  async streamAudio(deviceId: string, audioData: ArrayBuffer): Promise<void> {
    const hearingAid = this.connectedDevices.get(deviceId);
    if (!hearingAid || !hearingAid.connected) {
      throw new Error('Device not connected');
    }

    if (hearingAid.protocol === 'ASHA') {
      // Start ASHA audio streaming
      await hearingAid.device.writeCharacteristicWithResponseForService(
        ASHA_SERVICE_UUID,
        ASHA_AUDIO_CONTROL_POINT_UUID,
        Buffer.from([0x01, 0x01, 0x00]).toString('base64') // Start command
      );

      // Stream audio data (in production, this would be done in chunks with proper timing)
      // This is a simplified example
      console.log('Streaming audio to ASHA device');
    }
  }

  async disconnectDevice(deviceId: string): Promise<void> {
    const hearingAid = this.connectedDevices.get(deviceId);
    if (hearingAid) {
      await this.manager.cancelDeviceConnection(deviceId);
      this.connectedDevices.delete(deviceId);
      await this.saveConnectedDevices();
    }
  }

  private async reconnectDevice(deviceId: string): Promise<void> {
    try {
      await this.connectToDevice(deviceId);
    } catch (error) {
      console.error(`Failed to reconnect to device ${deviceId}:`, error);
    }
  }

  private handleDeviceDisconnected(deviceId: string): void {
    const hearingAid = this.connectedDevices.get(deviceId);
    if (hearingAid) {
      hearingAid.connected = false;
      console.log(`Device ${deviceId} disconnected`);

      // Attempt to reconnect after 5 seconds
      setTimeout(() => {
        this.reconnectDevice(deviceId);
      }, 5000);
    }
  }

  private async saveConnectedDevices(): Promise<void> {
    const deviceIds = Array.from(this.connectedDevices.keys());
    await AsyncStorage.setItem('connected_hearing_aids', JSON.stringify(deviceIds));
  }

  getConnectedDevices(): HearingAidDevice[] {
    return Array.from(this.connectedDevices.values());
  }

  async destroy(): Promise<void> {
    this.stopScan();
    for (const deviceId of this.connectedDevices.keys()) {
      await this.disconnectDevice(deviceId);
    }
    this.manager.destroy();
  }
}

export default new BLEService();
export type { HearingAidDevice };
