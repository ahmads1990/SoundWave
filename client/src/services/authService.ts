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
    const response = await api.post<ApiResponse<AuthResponse>>('/identity/login', credentials);
    const data = response.data.data;
    if (!data) throw new Error(response.data.message || 'Login failed');
    return data;
  },

  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/identity/register', data);
    const result = response.data.data;
    if (!result) throw new Error(response.data.message || 'Registration failed');
    return result;
  },

  forgotPassword: async (data: ForgotPasswordRequest): Promise<void> => {
    await api.post('/identity/forgot-password', data);
  },

  resetPassword: async (data: ResetPasswordRequest): Promise<void> => {
    await api.post('/identity/reset-password', data);
  },

  logout: async (): Promise<void> => {
    try {
      await api.post('/identity/logout');
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
};
