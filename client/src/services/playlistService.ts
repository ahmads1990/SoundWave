import { api } from '../api/api';
import { ApiResponse } from '../types/common.types';

export const playlistService = {
  uploadPlaylistCover: async (playlistId: string, file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<ApiResponse<{ fileId: string; url: string; sizeBytes: number }>>(
      `/v1/playlists/${playlistId}/cover`,
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
      }
    );
    const url = response.data.data?.url;
    if (!url) throw new Error(response.data.message || 'Failed to upload playlist cover');
    return url;
  },
};
