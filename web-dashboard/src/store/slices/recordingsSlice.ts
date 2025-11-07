import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { recordingsService } from '../../services/recordingsService';

export interface Recording {
  id: string;
  childId: string;
  audioUrl: string;
  transcription: string;
  confidence: number;
  createdAt: string;
  status: 'processing' | 'completed' | 'failed';
}

interface RecordingsState {
  recordings: Recording[];
  selectedRecording: Recording | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: RecordingsState = {
  recordings: [],
  selectedRecording: null,
  isLoading: false,
  error: null,
};

export const fetchRecordings = createAsyncThunk(
  'recordings/fetchAll',
  async (childId?: string, { rejectWithValue }) => {
    try {
      const recordings = await recordingsService.getRecordings(childId);
      return recordings;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch recordings');
    }
  }
);

export const uploadRecording = createAsyncThunk(
  'recordings/upload',
  async ({ childId, file }: { childId: string; file: File }, { rejectWithValue }) => {
    try {
      const recording = await recordingsService.uploadRecording(childId, file);
      return recording;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to upload recording');
    }
  }
);

const recordingsSlice = createSlice({
  name: 'recordings',
  initialState,
  reducers: {
    selectRecording: (state, action) => {
      state.selectedRecording = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Fetch recordings
    builder.addCase(fetchRecordings.pending, (state) => {
      state.isLoading = true;
      state.error = null;
    });
    builder.addCase(fetchRecordings.fulfilled, (state, action) => {
      state.isLoading = false;
      state.recordings = action.payload;
    });
    builder.addCase(fetchRecordings.rejected, (state, action) => {
      state.isLoading = false;
      state.error = action.payload as string;
    });

    // Upload recording
    builder.addCase(uploadRecording.pending, (state) => {
      state.isLoading = true;
    });
    builder.addCase(uploadRecording.fulfilled, (state, action) => {
      state.isLoading = false;
      state.recordings.unshift(action.payload);
    });
    builder.addCase(uploadRecording.rejected, (state, action) => {
      state.isLoading = false;
      state.error = action.payload as string;
    });
  },
});

export const { selectRecording, clearError } = recordingsSlice.actions;
export default recordingsSlice.reducer;
