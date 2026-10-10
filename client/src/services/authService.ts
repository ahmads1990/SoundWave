import { api } from '../api/api';
import {
  AuthResponse,
  ForgotPasswordRequest,
  LoginRequest,
  RegisterRequest,
  ResetPasswordRequest,
  User,
  UserRole,
} from '../types/auth.types';
import { ApiResponse } from '../types/common.types';
import { parseJwt } from '../utils/jwt';
import { tokenStorage } from '../utils/tokenStorage';

export const authService = {
  login: async (credentials: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/v1/auth/login', credentials);
    const data = response.data.data;
    if (!data) throw new Error(response.data.message || 'Login failed');
    return data;
  },

  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/v1/auth/register', data);
    const result = response.data.data;
    if (!result) throw new Error(response.data.message || 'Registration failed');
    return result;
  },

  forgotPassword: async (data: ForgotPasswordRequest): Promise<void> => {
    await api.post('/v1/auth/password/forgot', data);
  },

  resetPassword: async (data: ResetPasswordRequest): Promise<void> => {
    await api.post('/v1/auth/password/reset', data);
  },

  logout: async (): Promise<void> => {
    try {
      await api.post('/v1/auth/logout');
    } catch {
      // Ignore network errors during logout
    } finally {
      tokenStorage.clearTokens();
    }
  },

  getCurrentUser: (): User | null => {
    const token = tokenStorage.getAccessToken();
    if (!token) return null;
    const decoded = parseJwt(token);
    if (!decoded) return null;

    const rawRole = Array.isArray(decoded.role) ? decoded.role[0] : decoded.role;
    const role: UserRole = rawRole === 'Admin' || rawRole === 'Artist' ? rawRole : 'Listener';

    return {
      id: (decoded.nameid as string) || (decoded.sub as string) || '',
      email: (decoded.email as string) || '',
      userName: (decoded.unique_name as string) || (decoded.email as string) || 'User',
      role,
      isEmailVerified: true,
    };
  },

  getProfile: async (): Promise<UserProfile> => {
    const response = await api.get<ApiResponse<UserProfile>>('/v1/auth/profile/me');
    const data = response.data.data;
    if (!data) throw new Error(response.data.message || 'Failed to fetch user profile');
    return data;
  },

  uploadAvatar: async (file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<ApiResponse<{ fileId: string; url: string; sizeBytes: number }>>(
      '/v1/identity/users/me/avatar',
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
      }
    );
    const url = response.data.data?.url;
    if (!url) throw new Error(response.data.message || 'Failed to upload avatar');
    return url;
  },

  uploadBanner: async (file: File): Promise<string> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await api.post<ApiResponse<{ fileId: string; url: string; sizeBytes: number }>>(
      '/v1/identity/users/me/banner',
      formData,
      {
        headers: { 'Content-Type': 'multipart/form-data' },
      }
    );
    const url = response.data.data?.url;
    if (!url) throw new Error(response.data.message || 'Failed to upload banner');
    return url;
  },
};

export interface UserProfile {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string;
  profilePicUrl?: string | null;
  coverImageUrl?: string | null;
  phoneNumber?: string | null;
  dateOfBirth?: string | null;
  language?: string;
  gender?: string;
  countryId?: number | null;
}
