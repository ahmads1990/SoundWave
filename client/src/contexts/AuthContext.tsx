import React, { createContext, useContext, useEffect, useState } from 'react';
import { authService } from '../services/authService';
import { LoginRequest, RegisterRequest, User } from '../types/auth.types';
import { extractUserFromToken, isTokenExpired } from '../utils/jwt';
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
      const storedUser = tokenStorage.getUser();

      if (token && !isTokenExpired(token)) {
        const currentUser = storedUser || extractUserFromToken(token);
        if (currentUser) {
          setUser(currentUser);
          tokenStorage.setUser(currentUser);
        }

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
    const token = response.jwtToken || (response as unknown as { accessToken?: string }).accessToken;
    if (!token) {
      throw new Error('Authentication token not received from server');
    }

    tokenStorage.setTokens(token, response.refreshToken);

    const loggedUser = extractUserFromToken(token);
    if (loggedUser) {
      tokenStorage.setUser(loggedUser);
      setUser(loggedUser);
    }
    void refreshProfile();
  };

  const register = async (data: RegisterRequest) => {
    const response = await authService.register(data);
    const token = response.jwtToken || response.accessToken;
    if (token) {
      tokenStorage.setTokens(token, response.refreshToken);
      const registeredUser = response.user || extractUserFromToken(token);
      if (registeredUser) {
        tokenStorage.setUser(registeredUser);
        setUser(registeredUser);
      }
      void refreshProfile();
    }
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
