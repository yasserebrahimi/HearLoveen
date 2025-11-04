import React from 'react';
import { View, StyleSheet } from 'react-native';
import { Button, Card, Title, Paragraph } from 'react-native-paper';

interface HomeScreenProps {
  navigation: any;
}

const HomeScreen: React.FC<HomeScreenProps> = ({ navigation }) => {
  return (
    <View style={styles.container}>
      <Card style={styles.card}>
        <Card.Content>
          <Title>Welcome to HearLoveen</Title>
          <Paragraph>
            AI-Powered Speech Therapy for Hearing-Impaired Children
          </Paragraph>
        </Card.Content>
      </Card>

      <Button
        mode="contained"
        onPress={() => navigation.navigate('Record')}
        style={styles.button}
      >
        Start Recording
      </Button>

      <Button
        mode="outlined"
        onPress={() => navigation.navigate('Progress')}
        style={styles.button}
      >
        View Progress
      </Button>
    </View>
  );
};

const styles = StyleSheet.create({
  container: {
    flex: 1,
    padding: 20,
    justifyContent: 'center',
  },
  card: {
    marginBottom: 30,
  },
  button: {
    marginVertical: 10,
  },
});

export default HomeScreen;
