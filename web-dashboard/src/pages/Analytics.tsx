import React, { useEffect } from 'react';
import {
  Box,
  Typography,
  Grid,
  Paper,
  CircularProgress,
  Alert,
} from '@mui/material';
import {
  LineChart,
  Line,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Legend,
} from 'recharts';
import { useDispatch, useSelector } from 'react-redux';
import { fetchProgressData } from '../store/slices/analyticsSlice';
import { RootState, AppDispatch } from '../store/store';

const Analytics: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { progressData, isLoading, error } = useSelector((state: RootState) => state.analytics);

  useEffect(() => {
    dispatch(fetchProgressData());
  }, [dispatch]);

  if (isLoading) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Analytics
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Grid container spacing={3}>
        {/* Progress Over Time */}
        <Grid item xs={12} md={8}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Progress Over Time
            </Typography>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={progressData}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip />
                <Legend />
                <Line
                  type="monotone"
                  dataKey="accuracy"
                  stroke="#2196F3"
                  strokeWidth={2}
                  name="Accuracy (%)"
                />
              </LineChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>

        {/* Phoneme Performance */}
        <Grid item xs={12} md={4}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Top Performing Phonemes
            </Typography>
            <Box sx={{ height: 300 }}>
              {progressData.length > 0 ? (
                <Typography variant="body2" color="text.secondary">
                  Performance data available
                </Typography>
              ) : (
                <Typography variant="body2" color="text.secondary">
                  No data available yet
                </Typography>
              )}
            </Box>
          </Paper>
        </Grid>

        {/* Weekly Sessions */}
        <Grid item xs={12}>
          <Paper sx={{ p: 3 }}>
            <Typography variant="h6" gutterBottom>
              Weekly Practice Sessions
            </Typography>
            <ResponsiveContainer width="100%" height={250}>
              <BarChart data={progressData.slice(0, 7)}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="accuracy" fill="#FF5722" name="Sessions" />
              </BarChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Analytics;
