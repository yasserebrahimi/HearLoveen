import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import { childrenService } from '../../services/childrenService';

export interface Child {
  id: string;
  name: string;
  age: number;
  parentId: string;
  therapistId?: string;
  createdAt: string;
  progress: number;
}

interface ChildrenState {
  children: Child[];
  selectedChild: Child | null;
  isLoading: boolean;
  error: string | null;
}

const initialState: ChildrenState = {
  children: [],
  selectedChild: null,
  isLoading: false,
  error: null,
};

export const fetchChildren = createAsyncThunk(
  'children/fetchAll',
  async (_, { rejectWithValue }) => {
    try {
      const children = await childrenService.getAllChildren();
      return children;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch children');
    }
  }
);

export const fetchChildById = createAsyncThunk(
  'children/fetchById',
  async (id: string, { rejectWithValue }) => {
    try {
      const child = await childrenService.getChildById(id);
      return child;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to fetch child');
    }
  }
);

export const createChild = createAsyncThunk(
  'children/create',
  async (childData: Omit<Child, 'id' | 'createdAt' | 'progress'>, { rejectWithValue }) => {
    try {
      const newChild = await childrenService.createChild(childData);
      return newChild;
    } catch (error: any) {
      return rejectWithValue(error.response?.data?.message || 'Failed to create child');
    }
  }
);

const childrenSlice = createSlice({
  name: 'children',
  initialState,
  reducers: {
    selectChild: (state, action) => {
      state.selectedChild = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
  },
  extraReducers: (builder) => {
    // Fetch all children
    builder.addCase(fetchChildren.pending, (state) => {
      state.isLoading = true;
      state.error = null;
    });
    builder.addCase(fetchChildren.fulfilled, (state, action) => {
      state.isLoading = false;
      state.children = action.payload;
    });
    builder.addCase(fetchChildren.rejected, (state, action) => {
      state.isLoading = false;
      state.error = action.payload as string;
    });

    // Fetch child by ID
    builder.addCase(fetchChildById.pending, (state) => {
      state.isLoading = true;
    });
    builder.addCase(fetchChildById.fulfilled, (state, action) => {
      state.isLoading = false;
      state.selectedChild = action.payload;
    });
    builder.addCase(fetchChildById.rejected, (state, action) => {
      state.isLoading = false;
      state.error = action.payload as string;
    });

    // Create child
    builder.addCase(createChild.pending, (state) => {
      state.isLoading = true;
    });
    builder.addCase(createChild.fulfilled, (state, action) => {
      state.isLoading = false;
      state.children.push(action.payload);
    });
    builder.addCase(createChild.rejected, (state, action) => {
      state.isLoading = false;
      state.error = action.payload as string;
    });
  },
});

export const { selectChild, clearError } = childrenSlice.actions;
export default childrenSlice.reducer;
