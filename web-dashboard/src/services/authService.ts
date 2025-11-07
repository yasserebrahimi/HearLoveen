import { apiClient } from './api';
import { User } from '../store/slices/authSlice';

export const authService = {
  async login(email: string, password: string): Promise<{ user: User; token: string }> {
    const response = await apiClient.post('/api/v1/auth/login', { email, password });
    return response.data;
  },

  async register(userData: { email: string; password: string; name: string; role: string }) {
    const response = await apiClient.post('/api/v1/auth/register', userData);
    return response.data;
  },

  async getCurrentUser(): Promise<User> {
    const response = await apiClient.get('/api/v1/users/me');
    return response.data;
  },

  async logout() {
    // Call logout endpoint if needed
    localStorage.removeItem('token');
  },
};
