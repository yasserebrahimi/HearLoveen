import { apiClient } from './api';
import { Child } from '../store/slices/childrenSlice';

export const childrenService = {
  async getAllChildren(): Promise<Child[]> {
    const response = await apiClient.get('/api/v1/children');
    return response.data;
  },

  async getChildById(id: string): Promise<Child> {
    const response = await apiClient.get(`/api/v1/children/${id}`);
    return response.data;
  },

  async createChild(childData: Omit<Child, 'id' | 'createdAt' | 'progress'>): Promise<Child> {
    const response = await apiClient.post('/api/v1/children', childData);
    return response.data;
  },

  async updateChild(id: string, childData: Partial<Child>): Promise<Child> {
    const response = await apiClient.put(`/api/v1/children/${id}`, childData);
    return response.data;
  },

  async deleteChild(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/children/${id}`);
  },
};
