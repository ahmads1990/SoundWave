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
  updateProfileImages: (profilePicUrl?: string, coverImageUrl?: string) => void;
  refreshProfile: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const refreshProfile = async () => {
    try {
      const profile = await authService.getProfile();
      setUser((prev) => {
        if (!prev) return null;
        return {
          ...prev,
          profilePicUrl: profile.profilePicUrl || prev.profilePicUrl,
          coverImageUrl: profile.coverImageUrl || prev.coverImageUrl,
        };
      });
    } catch {
      // Ignore background refresh failure if offline or unauthenticated
    }
  };

  const updateProfileImages = (profilePicUrl?: string, coverImageUrl?: string) => {
    setUser((prev) => {
      if (!prev) return null;
      return {
        ...prev,
        ...(profilePicUrl !== undefined ? { profilePicUrl } : {}),
        ...(coverImageUrl !== undefined ? { coverImageUrl } : {}),
      };
    });
  };

  useEffect(() => {
    const initAuth = async () => {
      const token = tokenStorage.getAccessToken();
      if (token && !isTokenExpired(token)) {
        const currentUser = authService.getCurrentUser();
        setUser(currentUser);
        // Hydrate profile pictures from backend
        try {
          const profile = await authService.getProfile();
          setUser((prev) =>
            prev
              ? {
                  ...prev,
                  profilePicUrl: profile.profilePicUrl || prev.profilePicUrl,
                  coverImageUrl: profile.coverImageUrl || prev.coverImageUrl,
                }
              : null
          );
        } catch {
          // Ignore background profile fetch failure
        }
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
    const loggedUser = response.user || authService.getCurrentUser();
    setUser(loggedUser);
    void refreshProfile();
  };

  const register = async (data: RegisterRequest) => {
    const response = await authService.register(data);
    tokenStorage.setTokens(response.accessToken, response.refreshToken);
    const registeredUser = response.user || authService.getCurrentUser();
    setUser(registeredUser);
    void refreshProfile();
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
        updateProfileImages,
        refreshProfile,
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
