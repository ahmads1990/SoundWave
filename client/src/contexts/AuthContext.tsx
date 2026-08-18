import React, { createContext, useContext, useEffect, useState } from 'react';
import { authService } from '../services/authService';
import { LoginRequest, RegisterRequest, User } from '../types/auth.types';
import { isTokenExpired } from '../utils/jwt';
import { tokenStorage } from '../utils/tokenStorage';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const initAuth = () => {
      const token = tokenStorage.getAccessToken();
      if (token && !isTokenExpired(token)) {
        const currentUser = authService.getCurrentUser();
        setUser(currentUser);
      } else if (token && isTokenExpired(token)) {
        // Token is expired; let axios interceptor or refresh flow handle it, or clear
        const refreshToken = tokenStorage.getRefreshToken();
        if (!refreshToken) {
          tokenStorage.clearTokens();
          setUser(null);
        } else {
          // Token exists with refresh token; keep optimistic user
          const currentUser = authService.getCurrentUser();
          setUser(currentUser);
        }
      } else {
        tokenStorage.clearTokens();
        setUser(null);
      }
      setIsLoading(false);
    };

    initAuth();
  }, []);

  const login = async (credentials: LoginRequest) => {
    const response = await authService.login(credentials);
    tokenStorage.setTokens(response.accessToken, response.refreshToken);
    setUser(response.user || authService.getCurrentUser());
  };

  const register = async (data: RegisterRequest) => {
    const response = await authService.register(data);
    tokenStorage.setTokens(response.accessToken, response.refreshToken);
    setUser(response.user || authService.getCurrentUser());
  };

  const logout = async () => {
    await authService.logout();
    setUser(null);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = (): AuthContextType => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
