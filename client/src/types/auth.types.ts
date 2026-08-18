export type UserRole = 'Listener' | 'Artist' | 'Admin';

export interface User {
  id: string;
  email: string;
  userName: string;
  role: UserRole;
  isEmailVerified: boolean;
  profilePicUrl?: string;
  coverImageUrl?: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresIn?: number;
  user: User;
}

export interface LoginRequest {
  email: string;
  passwordHash?: string;
  password?: string;
}

export interface RegisterRequest {
  email: string;
  userName: string;
  password?: string;
  confirmPassword?: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ResetPasswordRequest {
  email: string;
  token: string;
  newPassword: string;
}

export interface RefreshTokenRequest {
  accessToken: string;
  refreshToken: string;
}
