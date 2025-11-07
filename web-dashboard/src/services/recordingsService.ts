import { apiClient } from './api';
import { Recording } from '../store/slices/recordingsSlice';

export const recordingsService = {
  async getRecordings(childId?: string): Promise<Recording[]> {
    const url = childId ? `/api/v1/audio/child/${childId}` : '/api/v1/audio';
    const response = await apiClient.get(url);
    return response.data;
  },

  async getRecordingById(id: string): Promise<Recording> {
    const response = await apiClient.get(`/api/v1/audio/${id}`);
    return response.data;
  },

  async uploadRecording(childId: string, file: File): Promise<Recording> {
    const formData = new FormData();
    formData.append('audio', file);
    formData.append('childId', childId);

    const response = await apiClient.post('/api/v1/audio/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  async deleteRecording(id: string): Promise<void> {
    await apiClient.delete(`/api/v1/audio/${id}`);
  },
};
