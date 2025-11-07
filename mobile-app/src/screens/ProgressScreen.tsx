import React from 'react';
import {
  View,
  Text,
  StyleSheet,
  ScrollView,
  Dimensions,
} from 'react-native';
import { LineChart } from 'react-native-chart-kit';
import { useRoute } from '@react-navigation/native';

const ProgressScreen: React.FC = () => {
  const route = useRoute();
  const childId = (route.params as any)?.childId;

  // Mock data - in production, fetch from API
  const progressData = {
    labels: ['Week 1', 'Week 2', 'Week 3', 'Week 4'],
    datasets: [
      {
        data: [65, 72, 78, 85],
        color: (opacity = 1) => `rgba(33, 150, 243, ${opacity})`,
        strokeWidth: 2,
      },
    ],
  };

  const recentRecordings = [
    { id: '1', date: '2024-11-07', accuracy: 85, word: 'Hello' },
    { id: '2', date: '2024-11-06', accuracy: 78, word: 'World' },
    { id: '3', date: '2024-11-05', accuracy: 82, word: 'Thank you' },
  ];

  return (
    <ScrollView style={styles.container}>
      <Text style={styles.title}>Progress Report</Text>

      {/* Overall Progress */}
      <View style={styles.card}>
        <Text style={styles.cardTitle}>Overall Accuracy</Text>
        <Text style={styles.accuracyText}>85%</Text>
        <Text style={styles.accuracySubtext}>
          +7% from last week
        </Text>
      </View>

      {/* Progress Chart */}
      <View style={styles.card}>
        <Text style={styles.cardTitle}>Weekly Progress</Text>
        <LineChart
          data={progressData}
          width={Dimensions.get('window').width - 64}
          height={220}
          chartConfig={{
            backgroundColor: '#fff',
            backgroundGradientFrom: '#fff',
            backgroundGradientTo: '#fff',
            decimalPlaces: 0,
            color: (opacity = 1) => `rgba(33, 150, 243, ${opacity})`,
            labelColor: (opacity = 1) => `rgba(0, 0, 0, ${opacity})`,
            style: {
              borderRadius: 16,
            },
            propsForDots: {
              r: '6',
              strokeWidth: '2',
              stroke: '#2196F3',
            },
          }}
          bezier
          style={{
            marginVertical: 8,
            borderRadius: 16,
          }}
        />
      </View>

      {/* Recent Recordings */}
      <View style={styles.card}>
        <Text style={styles.cardTitle}>Recent Recordings</Text>
        {recentRecordings.map((recording) => (
          <View key={recording.id} style={styles.recordingItem}>
            <View>
              <Text style={styles.recordingWord}>{recording.word}</Text>
              <Text style={styles.recordingDate}>{recording.date}</Text>
            </View>
            <Text style={styles.recordingAccuracy}>{recording.accuracy}%</Text>
          </View>
        ))}
      </View>

      {/* Practice Recommendations */}
      <View style={styles.card}>
        <Text style={styles.cardTitle}>Practice Recommendations</Text>
        <Text style={styles.recommendation}>
          🎯 Focus on 'S' and 'TH' sounds
        </Text>
        <Text style={styles.recommendation}>
          ⏰ Practice 10-15 minutes daily
        </Text>
        <Text style={styles.recommendation}>
          🌟 Great progress on 'R' sounds!
        </Text>
      </View>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    padding: 16,
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    color: '#333',
    marginBottom: 16,
    marginTop: 8,
  },
  card: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    marginBottom: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  cardTitle: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
    marginBottom: 12,
  },
  accuracyText: {
    fontSize: 48,
    fontWeight: 'bold',
    color: '#4CAF50',
    textAlign: 'center',
  },
  accuracySubtext: {
    fontSize: 14,
    color: '#4CAF50',
    textAlign: 'center',
    marginTop: 4,
  },
  recordingItem: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    paddingVertical: 12,
    borderBottomWidth: 1,
    borderBottomColor: '#f0f0f0',
  },
  recordingWord: {
    fontSize: 16,
    fontWeight: '500',
    color: '#333',
  },
  recordingDate: {
    fontSize: 12,
    color: '#666',
    marginTop: 2,
  },
  recordingAccuracy: {
    fontSize: 16,
    fontWeight: 'bold',
    color: '#2196F3',
  },
  recommendation: {
    fontSize: 14,
    color: '#666',
    marginBottom: 8,
    lineHeight: 20,
  },
});

export default ProgressScreen;
