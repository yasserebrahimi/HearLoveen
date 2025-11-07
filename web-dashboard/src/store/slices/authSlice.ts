import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { authService } from '../../services/authService';

export interface User {
  id: string;
  email: string;
  name: string;
  role: 'Parent' | 'Therapist' | 'Admin';
}

interface AuthState {
  user: User | null;
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
}

const initialState: AuthState = {
  user: null,
  token: localStorage.getItem('token'),
  isAuthenticated: !!localStorage.getItem('token'),
  isLoading: false,
  error: null,
};

export const login = createAsyncThunk(
  'auth/login',
  async (credentials: { email: string; password: string }, { rejectWithValue }) => {
    try {
      const response = await authService.login(credentials.email, credentials.password);
      localStorage.setItem('token', response.token);
      return response;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Login failed');
    }
  }
);

export const logout = createAsyncThunk('auth/logout', async () => {
  localStorage.removeItem('token');
  return null;
});

export const getCurrentUser = createAsyncThunk(
  'auth/getCurrentUser',
  async (_, { rejectWithValue }) => {
    try {
      const user = await authService.getCurrentUser();
      return user;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to get user');
    }
  }
);

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Login
    builder.addCase(login.pending, (state) => {
      state.isLoading = true;
      state.error = null;
    });
    builder.addCase(login.fulfilled, (state, action) => {
      state.isLoading = false;
      state.isAuthenticated = true;
      state.user = action.payload.user;
      state.token = action.payload.token;
    });
    builder.addCase(login.rejected, (state, action) => {
      state.isLoading = false;
      state.error = action.payload as string;
    });

    // Logout
    builder.addCase(logout.fulfilled, (state) => {
      state.user = null;
      state.token = null;
      state.isAuthenticated = false;
    });

    // Get Current User
    builder.addCase(getCurrentUser.pending, (state) => {
      state.isLoading = true;
    });
    builder.addCase(getCurrentUser.fulfilled, (state, action) => {
      state.isLoading = false;
      state.user = action.payload;
      state.isAuthenticated = true;
    });
    builder.addCase(getCurrentUser.rejected, (state) => {
      state.isLoading = false;
      state.isAuthenticated = false;
      state.token = null;
      localStorage.removeItem('token');
    });
  },
});

export const { clearError } = authSlice.actions;
export default authSlice.reducer;
