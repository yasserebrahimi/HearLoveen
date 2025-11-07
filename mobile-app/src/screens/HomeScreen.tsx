import React from 'react';
import { View, Text, StyleSheet, FlatList, TouchableOpacity } from 'react-native';
import { useNavigation } from '@react-navigation/native';

interface Child {
  id: string;
  name: string;
  age: number;
  progress: number;
}

const HomeScreen: React.FC = () => {
  const navigation = useNavigation();

  // Mock data - in production, fetch from API
  const children: Child[] = [
    { id: '1', name: 'Emma Johnson', age: 5, progress: 75 },
    { id: '2', name: 'Liam Smith', age: 6, progress: 82 },
  ];

  const renderChild = ({ item }: { item: Child }) => (
    <TouchableOpacity
      style={styles.card}
      onPress={() => navigation.navigate('Progress' as never, { childId: item.id } as never)}
    >
      <View style={styles.cardHeader}>
        <Text style={styles.childName}>{item.name}</Text>
        <Text style={styles.childAge}>{item.age} years</Text>
      </View>
      <View style={styles.progressContainer}>
        <Text style={styles.progressLabel}>Progress</Text>
        <View style={styles.progressBar}>
          <View style={[styles.progressFill, { width: `${item.progress}%` }]} />
        </View>
        <Text style={styles.progressText}>{item.progress}%</Text>
      </View>
    </TouchableOpacity>
  );

  return (
    <View style={styles.container}>
      <Text style={styles.title}>🦻 HearLoveen</Text>
      <Text style={styles.subtitle}>Speech Therapy for Children</Text>

      <FlatList
        data={children}
        renderItem={renderChild}
        keyExtractor={(item) => item.id}
        contentContainerStyle={styles.listContainer}
      />

      <TouchableOpacity
        style={styles.recordButton}
        onPress={() => navigation.navigate('Record' as never)}
      >
        <Text style={styles.recordButtonText}>🎤 Start Recording</Text>
      </TouchableOpacity>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#f5f5f5',
    padding: 16,
  },
  title: {
    fontSize: 28,
    fontWeight: 'bold',
    color: '#2196F3',
    textAlign: 'center',
    marginTop: 20,
  },
  subtitle: {
    fontSize: 16,
    color: '#666',
    textAlign: 'center',
    marginBottom: 24,
  },
  listContainer: {
    paddingBottom: 100,
  },
  card: {
    backgroundColor: 'white',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.1,
    shadowRadius: 4,
    elevation: 3,
  },
  cardHeader: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    marginBottom: 12,
  },
  childName: {
    fontSize: 18,
    fontWeight: '600',
    color: '#333',
  },
  childAge: {
    fontSize: 14,
    color: '#666',
  },
  progressContainer: {
    marginTop: 8,
  },
  progressLabel: {
    fontSize: 12,
    color: '#666',
    marginBottom: 4,
  },
  progressBar: {
    height: 8,
    backgroundColor: '#e0e0e0',
    borderRadius: 4,
    overflow: 'hidden',
  },
  progressFill: {
    height: '100%',
    backgroundColor: '#4CAF50',
  },
  progressText: {
    fontSize: 12,
    color: '#666',
    marginTop: 4,
    textAlign: 'right',
  },
  recordButton: {
    position: 'absolute',
    bottom: 24,
    left: 16,
    right: 16,
    backgroundColor: '#2196F3',
    borderRadius: 12,
    padding: 16,
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.3,
    shadowRadius: 8,
    elevation: 8,
  },
  recordButtonText: {
    color: 'white',
    fontSize: 18,
    fontWeight: 'bold',
  },
});

export default HomeScreen;
