import React, { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { ThemeProvider, createTheme, CssBaseline } from '@mui/material';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Provider, useDispatch, useSelector } from 'react-redux';
import { store, RootState, AppDispatch } from './store/store';
import { getCurrentUser } from './store/slices/authSlice';
import Dashboard from './pages/Dashboard';
import Children from './pages/Children';
import Recordings from './pages/Recordings';
import Analytics from './pages/Analytics';
import Login from './pages/Login';
import Layout from './components/Layout';

const theme = createTheme({
  palette: {
    primary: {
      main: '#2196F3',
    },
    secondary: {
      main: '#FF5722',
    },
  },
});

const queryClient = new QueryClient();

// Protected Route Component
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useSelector((state: RootState) => state.auth);
  const dispatch = useDispatch<AppDispatch>();

  useEffect(() => {
    const token = localStorage.getItem('token');
    if (token && !isAuthenticated) {
      dispatch(getCurrentUser());
    }
  }, [dispatch, isAuthenticated]);

  if (isLoading) {
    return <div>Loading...</div>;
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
};

function AppContent() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route
          path="/*"
          element={
            <ProtectedRoute>
              <Layout>
                <Routes>
                  <Route path="/" element={<Dashboard />} />
                  <Route path="/children" element={<Children />} />
                  <Route path="/recordings" element={<Recordings />} />
                  <Route path="/analytics" element={<Analytics />} />
                </Routes>
              </Layout>
            </ProtectedRoute>
          }
        />
      </Routes>
    </BrowserRouter>
  );
}

function App() {
  return (
    <Provider store={store}>
      <QueryClientProvider client={queryClient}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          <AppContent />
        </ThemeProvider>
      </QueryClientProvider>
    </Provider>
  );
}

export default App;
