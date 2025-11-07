import { apiClient } from './api';
import { DashboardStats, ProgressData } from '../store/slices/analyticsSlice';

export const analyticsService = {
  async getDashboardStats(): Promise<DashboardStats> {
    const response = await apiClient.get('/api/v1/analytics/dashboard');
    return response.data;
  },

  async getProgressData(childId?: string): Promise<ProgressData[]> {
    const url = childId
      ? `/api/v1/analytics/progress/${childId}`
      : '/api/v1/analytics/progress';
    const response = await apiClient.get(url);
    return response.data;
  },

  async getChildReport(childId: string, fromDate?: string, toDate?: string) {
    const params = new URLSearchParams();
    if (fromDate) params.append('fromDate', fromDate);
    if (toDate) params.append('toDate', toDate);

    const response = await apiClient.get(`/api/v1/analytics/report/${childId}?${params}`);
    return response.data;
  },
};
