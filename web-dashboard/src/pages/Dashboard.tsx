import React, { useEffect } from 'react';
import { Grid, Paper, Typography, Box, CircularProgress, Alert } from '@mui/material';
import { LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip, ResponsiveContainer } from 'recharts';
import { useDispatch, useSelector } from 'react-redux';
import { fetchDashboardStats, fetchProgressData } from '../store/slices/analyticsSlice';
import { RootState, AppDispatch } from '../store/store';

const Dashboard: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { dashboardStats, progressData, isLoading, error } = useSelector(
    (state: RootState) => state.analytics
  );

  useEffect(() => {
    dispatch(fetchDashboardStats());
    dispatch(fetchProgressData());
  }, [dispatch]);

  if (isLoading && !dashboardStats) {
    return (
      <Box display="flex" justifyContent="center" alignItems="center" minHeight="400px">
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Dashboard
      </Typography>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Grid container spacing={3}>
        <Grid item xs={12} md={3}>
          <Paper sx={{ p: 2 }}>
            <Typography color="textSecondary" gutterBottom>
              Total Children
            </Typography>
            <Typography variant="h4">
              {dashboardStats?.totalChildren || 0}
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} md={3}>
          <Paper sx={{ p: 2 }}>
            <Typography color="textSecondary" gutterBottom>
              Recordings
            </Typography>
            <Typography variant="h4">
              {dashboardStats?.totalRecordings || 0}
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} md={3}>
          <Paper sx={{ p: 2 }}>
            <Typography color="textSecondary" gutterBottom>
              Avg Accuracy
            </Typography>
            <Typography variant="h4">
              {dashboardStats?.avgAccuracy || 0}%
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12} md={3}>
          <Paper sx={{ p: 2 }}>
            <Typography color="textSecondary" gutterBottom>
              Active Today
            </Typography>
            <Typography variant="h4">
              {dashboardStats?.activeToday || 0}
            </Typography>
          </Paper>
        </Grid>

        <Grid item xs={12}>
          <Paper sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Progress Over Time
            </Typography>
            <ResponsiveContainer width="100%" height={300}>
              <LineChart data={progressData.length > 0 ? progressData : [
                { date: 'No data', accuracy: 0 }
              ]}>
                <CartesianGrid strokeDasharray="3 3" />
                <XAxis dataKey="date" />
                <YAxis />
                <Tooltip />
                <Line type="monotone" dataKey="accuracy" stroke="#2196F3" />
              </LineChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
};

export default Dashboard;
