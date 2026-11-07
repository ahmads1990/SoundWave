import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import { tokenStorage } from '../utils/tokenStorage';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: unknown) => void;
  reject: (reason?: unknown) => void;
}> = [];

const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve(token);
    }
  });
  failedQueue = [];
};

// Request Interceptor: Attach Bearer JWT
api.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = tokenStorage.getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Catch 401 & Auto Refresh / Logout
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    // Never attempt token refresh on auth entry endpoints (login, register, refresh-tokens)
    const isAuthEndpoint =
      originalRequest.url?.includes('/v1/auth/login') ||
      originalRequest.url?.includes('/v1/auth/register') ||
      originalRequest.url?.includes('/v1/auth/refresh-tokens');

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthEndpoint) {
      const refreshToken = tokenStorage.getRefreshToken();
      const user = tokenStorage.getUser();

      if (!refreshToken || !user) {
        tokenStorage.clearTokens();
        if (!window.location.pathname.startsWith('/login')) {
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        const response = await axios.post(`${api.defaults.baseURL}/v1/auth/refresh-tokens`, {
          userId: user.id,
          refreshToken,
        });

        const newJwt =
          response.data?.data?.jwtToken ||
          response.data?.data?.accessToken ||
          response.data?.jwtToken;
        const newRefreshToken =
          response.data?.data?.refreshToken || response.data?.refreshToken;

        if (newJwt) {
          tokenStorage.setTokens(newJwt, newRefreshToken);
          api.defaults.headers.common.Authorization = `Bearer ${newJwt}`;
          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newJwt}`;
          }
          processQueue(null, newJwt);
          return api(originalRequest);
        }
      } catch (refreshError) {
        processQueue(refreshError, null);
        tokenStorage.clearTokens();
        if (!window.location.pathname.startsWith('/login')) {
          window.location.href = '/login';
        }
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
