import { api } from '../api/api';
import { ArtistProfileDto, GenreDto } from '../types/catalog.types';
import { ApiResponse, PaginatedRequest, PaginatedResponse } from '../types/common.types';

export const catalogService = {
  getGenres: async (params?: PaginatedRequest): Promise<PaginatedResponse<GenreDto>> => {
    const response = await api.get<ApiResponse<PaginatedResponse<GenreDto>>>('/catalog/genres', {
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
    const response = await api.get<ApiResponse<ArtistProfileDto>>(`/catalog/artists/${id}`);
    if (!response.data.data) throw new Error('Artist not found');
    return response.data.data;
  },

  applyForArtist: async (stageName: string, bio?: string): Promise<void> => {
    await api.post('/catalog/artists/apply', { stageName, bio });
  },

  getMyArtistStatus: async (): Promise<{ status: string; rejectionReason?: string }> => {
    const response = await api.get<ApiResponse<{ status: string; rejectionReason?: string }>>(
      '/catalog/artists/application-status'
    );
    return response.data.data || { status: 'None' };
  },
};
