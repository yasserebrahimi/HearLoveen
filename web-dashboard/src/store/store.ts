import { configureStore } from '@reduxjs/toolkit';
import authReducer from './slices/authSlice';
import childrenReducer from './slices/childrenSlice';
import recordingsReducer from './slices/recordingsSlice';
import analyticsReducer from './slices/analyticsSlice';

export const store = configureStore({
  reducer: {
    auth: authReducer,
    children: childrenReducer,
    recordings: recordingsReducer,
    analytics: analyticsReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        // Ignore these action types
        ignoredActions: ['auth/login/fulfilled'],
      },
    }),
});

export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
