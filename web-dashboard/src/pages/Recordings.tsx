import React from 'react';
import { Typography, Paper, Box } from '@mui/material';

const Recordings: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Audio Recordings
      </Typography>
      <Paper sx={{ p: 3, mt: 2 }}>
        <Typography variant="body1">
          Audio recordings list and playback will appear here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default Recordings;
