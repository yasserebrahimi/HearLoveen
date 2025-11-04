import React from 'react';
import { Typography, Paper, Box } from '@mui/material';

const Analytics: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Analytics
      </Typography>
      <Paper sx={{ p: 3, mt: 2 }}>
        <Typography variant="body1">
          Advanced analytics and reports will appear here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default Analytics;
