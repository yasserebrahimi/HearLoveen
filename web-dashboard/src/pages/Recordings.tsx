import React, { useEffect } from 'react';
import {
  Box,
  Typography,
  Button,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  CircularProgress,
  Alert,
} from '@mui/material';
import { CloudUpload as UploadIcon, PlayArrow as PlayIcon } from '@mui/icons-material';
import { useDispatch, useSelector } from 'react-redux';
import { fetchRecordings } from '../store/slices/recordingsSlice';
import { RootState, AppDispatch } from '../store/store';

const Recordings: React.FC = () => {
  const dispatch = useDispatch<AppDispatch>();
  const { recordings, isLoading, error } = useSelector((state: RootState) => state.recordings);

  useEffect(() => {
    dispatch(fetchRecordings());
  }, [dispatch]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'completed':
        return 'success';
      case 'processing':
        return 'warning';
      case 'failed':
        return 'error';
      default:
        return 'default';
    }
  };

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
        <Typography variant="h4">Recordings</Typography>
        <Button variant="contained" startIcon={<UploadIcon />}>
          Upload Recording
        </Button>
      </Box>

      {error && (
        <Alert severity="error" sx={{ mb: 2 }}>
          {error}
        </Alert>
      )}

      <TableContainer component={Paper}>
        <Table>
          <TableHead>
            <TableRow>
              <TableCell>Date</TableCell>
              <TableCell>Child</TableCell>
              <TableCell>Transcription</TableCell>
              <TableCell>Confidence</TableCell>
              <TableCell>Status</TableCell>
              <TableCell>Actions</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {recordings.length === 0 ? (
              <TableRow>
                <TableCell colSpan={6} align="center">
                  <Box py={4}>
                    <Typography variant="body1" color="text.secondary">
                      No recordings found
                    </Typography>
                  </Box>
                </TableCell>
              </TableRow>
            ) : (
              recordings.map((recording) => (
                <TableRow key={recording.id}>
                  <TableCell>
                    {new Date(recording.createdAt).toLocaleDateString()}
                  </TableCell>
                  <TableCell>{recording.childId}</TableCell>
                  <TableCell>
                    {recording.transcription?.substring(0, 50) || 'Processing...'}
                  </TableCell>
                  <TableCell>
                    {recording.confidence
                      ? `${(recording.confidence * 100).toFixed(1)}%`
                      : '-'}
                  </TableCell>
                  <TableCell>
                    <Chip
                      label={recording.status}
                      color={getStatusColor(recording.status) as any}
                      size="small"
                    />
                  </TableCell>
                  <TableCell>
                    <Button size="small" startIcon={<PlayIcon />}>
                      Play
                    </Button>
                  </TableCell>
                </TableRow>
              ))
            )}
          </TableBody>
        </Table>
      </TableContainer>
    </Box>
  );
};

export default Recordings;
