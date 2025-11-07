import React, { useEffect } from 'react';
import {
  Box,
  Typography,
  Button,
  Grid,
  Card,
  CardContent,
  CardActions,
  CircularProgress,
  Alert,
} from '@mui/material';
import { Add as AddIcon, Person as PersonIcon } from '@mui/icons-material';
import { useDispatch, useSelector } from 'react-redux';
import { fetchChildren } from '../store/slices/childrenSlice';
import { RootState, AppDispatch } from '../store/store';

const Children: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { children, isLoading, error } = useSelector((state: RootState) => state.children);

  useEffect(() => {
    dispatch(fetchChildren());
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
      <Box display="flex" justifyContent="space-between" alignItems="center" mb={3}>
        <Typography variant="h4">Children</Typography>
        <Button variant="contained" startIcon={<AddIcon />}>
          Add Child
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <Grid container spacing={3}>
        {children.length === 0 && !isLoading ? (
          <Grid item xs={12}>
            <Box textAlign="center" py={4}>
              <PersonIcon sx={{ fontSize: 64, color: 'text.secondary', mb: 2 }} />
              <Typography variant="h6" color="text.secondary">
                No children registered yet
              </Typography>
              <Typography variant="body2" color="text.secondary" mt={1}>
                Add your first child to start tracking their progress
              </Typography>
            </Box>
          </Grid>
        ) : (
          children.map((child) => (
            <Grid item xs={12} sm={6} md={4} key={child.id}>
              <Card>
                <CardContent>
                  <Typography variant="h6" gutterBottom>
                    {child.name}
                  </Typography>
                  <Typography color="text.secondary" gutterBottom>
                    Age: {child.age} years
                  </Typography>
                  <Typography variant="body2">
                    Progress: {child.progress}%
                  </Typography>
                </CardContent>
                <CardActions>
                  <Button size="small">View Details</Button>
                  <Button size="small">Recordings</Button>
                </CardActions>
              </Card>
            </Grid>
          ))
        )}
      </Grid>
    </Box>
  );
};

export default Children;
