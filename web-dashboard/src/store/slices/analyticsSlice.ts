import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { analyticsService } from '../../services/analyticsService';

export interface DashboardStats {
  totalChildren: number;
  totalRecordings: number;
  avgAccuracy: number;
  activeToday: number;
}

export interface ProgressData {
  date: string;
  accuracy: number;
}

interface AnalyticsState {
  dashboardStats: DashboardStats | null;
  progressData: ProgressData[];
  isLoading: boolean;
  error: string | null;
}

const initialState: AnalyticsState = {
  dashboardStats: null,
  progressData: [],
  isLoading: false,
  error: null,
};

export const fetchDashboardStats = createAsyncThunk(
  'analytics/fetchDashboardStats',
  async (_, { rejectWithValue }) => {
    try {
      const stats = await analyticsService.getDashboardStats();
      return stats;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch stats');
    }
  }
);

export const fetchProgressData = createAsyncThunk(
  'analytics/fetchProgressData',
  async (childId?: string, { rejectWithValue }) => {
    try {
      const data = await analyticsService.getProgressData(childId);
      return data;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch progress data');
    }
  }
);

const analyticsSlice = createSlice({
  name: 'analytics',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Fetch dashboard stats
    builder.addCase(fetchDashboardStats.pending, (state) => {
      state.isLoading = true;
      state.error = null;
    });
    builder.addCase(fetchDashboardStats.fulfilled, (state, action) => {
      state.isLoading = false;
      state.dashboardStats = action.payload;
    });
    builder.addCase(fetchDashboardStats.rejected, (state, action) => {
      state.isLoading = false;
      state.error = action.payload as string;
    });

    // Fetch progress data
    builder.addCase(fetchProgressData.pending, (state) => {
      state.isLoading = true;
    });
    builder.addCase(fetchProgressData.fulfilled, (state, action) => {
      state.isLoading = false;
      state.progressData = action.payload;
    });
    builder.addCase(fetchProgressData.rejected, (state, action) => {
      state.isLoading = false;
      state.error = action.payload as string;
    });
  },
});

export const { clearError } = analyticsSlice.actions;
export default analyticsSlice.reducer;
