import React from 'react';
import { View, StyleSheet, ScrollView } from 'react-native';
import { Card, Title, Paragraph, ProgressBar } from 'react-native-paper';

const ProgressScreen: React.FC = () => {
  return (
    <ScrollView style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <Title>Overall Progress</Title>
          <Paragraph>Your speech therapy progress</Paragraph>
          <ProgressBar progress={0.78} style={styles.progressBar} />
          <Paragraph>78% Retention Rate</Paragraph>
        </Card.Content>
      </Card>

      <Card style={styles.card}>
        <Card.Content>
          <Title>Recent Recordings</Title>
          <Paragraph>500+ recordings processed</Paragraph>
        </Card.Content>
      </Card>

      <Card style={styles.card}>
        <Card.Content>
          <Title>AI Accuracy</Title>
          <Paragraph>94% Speech Recognition Accuracy</Paragraph>
          <ProgressBar progress={0.94} style={styles.progressBar} />
        </Card.Content>
      </Card>
    </ScrollView>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
  },
  card: {
    marginBottom: 20,
  },
  progressBar: {
    marginVertical: 10,
  },
});

export default ProgressScreen;
