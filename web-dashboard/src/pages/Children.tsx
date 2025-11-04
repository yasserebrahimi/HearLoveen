import React from 'react';
import { Typography, Paper, Box } from '@mui/material';

const Children: React.FC = () => {
  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Children Management
      </Typography>
      <Paper sx={{ p: 3, mt: 2 }}>
        <Typography variant="body1">
          Children list and management will appear here.
        </Typography>
      </Paper>
    </Box>
  );
};

export default Children;
