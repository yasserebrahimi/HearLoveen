import React, { useState } from 'react';
import { View, StyleSheet, Alert } from 'react-native';
import { Button, Text, ProgressBar } from 'react-native-paper';
import { Audio } from 'expo-av';
import * as FileSystem from 'expo-file-system';
import axios from 'axios';

const RecordScreen: React.FC = () => {
  const [recording, setRecording] = useState<Audio.Recording | null>(null);
  const [isRecording, setIsRecording] = useState(false);
  const [uploading, setUploading] = useState(false);

  const startRecording = async () => {
    try {
      await Audio.requestPermissionsAsync();
      await Audio.setAudioModeAsync({
        allowsRecordingIOS: true,
        playsInSilentModeIOS: true,
      });

      const { recording } = await Audio.Recording.createAsync(
        Audio.RecordingOptionsPresets.HIGH_QUALITY
      );
      
      setRecording(recording);
      setIsRecording(true);
    } catch (err) {
      Alert.alert('Error', 'Failed to start recording');
    }
  };

  const stopRecording = async () => {
    if (!recording) return;

    setIsRecording(false);
    await recording.stopAndUnloadAsync();
    const uri = recording.getURI();
    
    if (uri) {
      await uploadRecording(uri);
    }
    
    setRecording(null);
  };

  const uploadRecording = async (uri: string) => {
    try {
      setUploading(true);
      
      const formData = new FormData();
      formData.append('file', {
        uri,
        type: 'audio/wav',
        name: 'recording.wav',
      } as any);

      await axios.post('http://api.hearloveen.com/audio/upload', formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });

      Alert.alert('Success', 'Recording uploaded successfully!');
    } catch (error) {
      Alert.alert('Error', 'Failed to upload recording');
    } finally {
      setUploading(false);
    }
  };

  return (
    <View style={styles.container}>
      <Text variant="headlineMedium" style={styles.title}>
        Record Your Exercise
      </Text>

      {uploading && <ProgressBar indeterminate />}

      <View style={styles.buttonContainer}>
        {!isRecording ? (
          <Button 
            mode="contained" 
            onPress={startRecording}
            icon="microphone"
            style={styles.button}
          >
            Start Recording
          </Button>
        ) : (
          <Button 
            mode="contained" 
            onPress={stopRecording}
            icon="stop"
            buttonColor="#f44336"
            style={styles.button}
          >
            Stop Recording
          </Button>
        )}
      </View>

      {isRecording && (
        <Text style={styles.recordingText}>
          🔴 Recording...
        </Text>
      )}
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    justifyContent: 'center',
  },
  title: {
    textAlign: 'center',
    marginBottom: 40,
  },
  buttonContainer: {
    marginTop: 20,
  },
  button: {
    marginVertical: 10,
  },
  recordingText: {
    textAlign: 'center',
    marginTop: 20,
    fontSize: 18,
    color: '#f44336',
  },
});

export default RecordScreen;
