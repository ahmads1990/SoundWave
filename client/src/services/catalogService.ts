import axios from 'axios';
import { api } from '../api/api';
import {
  ArtistProfileDto,
  GenreDto,
  GetTrackUploadUrlRequest,
  TrackStatusResponse,
  TrackUploadUrlResponse,
} from '../types/catalog.types';
import { ApiResponse, PaginatedRequest, PaginatedResponse } from '../types/common.types';

export const catalogService = {
  getGenres: async (params?: PaginatedRequest): Promise<PaginatedResponse<GenreDto>> => {
    const response = await api.get<ApiResponse<PaginatedResponse<GenreDto>>>('/v1/catalog/genres', {
      params,
    });
    return (
      response.data.data || {
        items: [],
        pageNumber: 1,
        pageSize: 50,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
      }
    );
  },

  getArtistProfile: async (id: string): Promise<ArtistProfileDto> => {
    const response = await api.get<ApiResponse<ArtistProfileDto>>(`/v1/catalog/artists/${id}`);
    if (!response.data.data) throw new Error('Artist not found');
    return response.data.data;
  },

  applyForArtist: async (stageName: string, bio?: string): Promise<void> => {
    await api.post('/v1/catalog/artists/apply', { stageName, bio });
  },

  getMyArtistStatus: async (): Promise<{ status: string; rejectionReason?: string }> => {
    const response = await api.get<ApiResponse<{ status: string; rejectionReason?: string }>>(
      '/v1/catalog/artists/applications/my'
    );
    return response.data.data || { status: 'None' };
  },

  uploadAlbumCover: async (albumId: string, file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<ApiResponse<{ fileId: string; url: string; sizeBytes: number }>>(
      `/v1/catalog/albums/${albumId}/cover`,
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
      }
    );
    const url = response.data.data?.url;
    if (!url) throw new Error(response.data.message || 'Failed to upload album cover');
    return url;
  },

  getTrackUploadUrl: async (
    trackId: string,
    data: GetTrackUploadUrlRequest
  ): Promise<TrackUploadUrlResponse> => {
    const response = await api.post<ApiResponse<TrackUploadUrlResponse>>(
      `/v1/catalog/tracks/${trackId}/audio/upload-url`,
      data
    );
    if (!response.data.data) throw new Error(response.data.message || 'Failed to get track upload URL');
    return response.data.data;
  },

  // Backwards-compatible alias
  initiateTrackUpload: async (
    trackId: string,
    data: GetTrackUploadUrlRequest
  ): Promise<TrackUploadUrlResponse> => {
    return catalogService.getTrackUploadUrl(trackId, data);
  },

  uploadToPresignedUrl: async (
    uploadUrl: string,
    file: File,
    onProgress?: (percent: number) => void
  ): Promise<void> => {
    await axios.put(uploadUrl, file, {
      headers: {
        'Content-Type': file.type || 'application/octet-stream',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && onProgress) {
          const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress(percent);
        }
      },
    });
  },

  getTrackStatus: async (trackId: string): Promise<TrackStatusResponse> => {
    const response = await api.get<ApiResponse<TrackStatusResponse>>(
      `/v1/catalog/tracks/${trackId}/status`
    );
    if (!response.data.data) throw new Error(response.data.message || 'Failed to fetch track status');
    return response.data.data;
  },
};

