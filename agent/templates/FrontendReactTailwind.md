# 🎨 Frontend React + TypeScript + Tailwind CSS Template

---

## 📂 Full Directory & File Skeleton

```text
client/
├── public/
│   ├── favicon.ico
│   └── vite.svg
│
├── src/
│   ├── api/
│   │   ├── api.ts                    # Axios instance with request/response interceptors & token refresh
│   │   └── endpoints.ts              # Centralized API endpoint constants (e.g. AUTH, RECIPES, PANTRY)
│   │
│   ├── assets/                       # Static images, SVGs, brand assets
│   │
│   ├── components/
│   │   ├── common/
│   │   │   ├── Button.tsx            # Variant-based button (primary, secondary, outline, danger, ghost)
│   │   │   ├── Input.tsx             # Accessible form input with label, helper text, and error states
│   │   │   ├── Card.tsx              # Clean container card with header, body, footer slots
│   │   │   ├── Badge.tsx             # Color-coded pill badge (success, warning, danger, info, neutral)
│   │   │   ├── Modal.tsx             # Accessible backdrop modal dialog with smooth transitions
│   │   │   ├── Skeleton.tsx          # Pulse loading skeleton for cards, tables, and text
│   │   │   ├── EmptyState.tsx        # Zero-data placeholder with icon, message, and action CTA
│   │   │   ├── Navbar.tsx            # Top application header with brand, search, and user profile menu
│   │   │   ├── Sidebar.tsx           # Collapsible navigation drawer with active route highlights
│   │   │   └── ProtectedRoute.tsx    # Route guard redirecting unauthorized users to /login
│   │   └── [FeatureComponents...]/   # Modular components grouped by domain (e.g. RecipeCard, PantryGrid)
│   │
│   ├── contexts/
│   │   ├── AuthContext.tsx           # Global auth provider (user, login, register, logout, role check)
│   │   ├── ThemeContext.tsx          # Dark / Light theme toggle with localStorage persistence
│   │   └── ToastContext.tsx          # Toast notification dispatch provider (success, error, info)
│   │
│   ├── hooks/
│   │   ├── useAuth.ts                # Consumer hook for AuthContext
│   │   ├── useToast.ts               # Consumer hook for ToastContext
│   │   ├── useDebounce.ts            # Input debounce hook for search inputs
│   │   └── [useFeatureHooks...].ts   # Custom TanStack Query query/mutation hooks
│   │
│   ├── layouts/
│   │   ├── AppLayout.tsx             # Main authenticated layout (Sidebar + Navbar + Content + Footer)
│   │   └── AuthLayout.tsx            # Clean centered card layout for Login, Register, Forgot Password
│   │
│   ├── pages/
│   │   ├── auth/
│   │   │   ├── LoginPage.tsx
│   │   │   ├── RegisterPage.tsx
│   │   │   └── ForgotPasswordPage.tsx
│   │   ├── dashboard/
│   │   │   └── DashboardPage.tsx
│   │   ├── NotFoundPage.tsx          # 404 error page
│   │   └── [FeaturePages...].tsx     # Domain-specific pages (e.g. RecipesPage, PlannerPage)
│   │
│   ├── services/
│   │   ├── authService.ts            # API call methods for authentication
│   │   └── [featureService].ts       # API call methods for features
│   │
│   ├── types/
│   │   ├── auth.types.ts             # User, LoginRequest, AuthResponse interfaces
│   │   ├── api.types.ts              # ApiResponse<T>, PagedResult<T>, ApiError
│   │   └── [feature.types].ts        # Domain-specific TypeScript models
│   │
│   ├── utils/
│   │   ├── cn.ts                     # clsx + tailwind-merge helper utility
│   │   ├── formatters.ts             # Date, currency, and unit formatting helpers
│   │   └── tokenStorage.ts           # Secure localStorage/sessionStorage token wrapper
│   │
│   ├── App.tsx                       # React Router route definitions & global Provider wrappers
│   ├── index.css                     # Tailwind directives, CSS variables & base typography
│   ├── main.tsx                      # Vite React root mount
│   └── vite-env.d.ts
│
├── .env.example
├── .gitignore
├── index.html
├── package.json
├── postcss.config.js
├── tailwind.config.js
├── tsconfig.json
└── vite.config.ts
```

---

## 📦 Recommended Dependencies (`package.json`)

```json
{
  "name": "client",
  "private": true,
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "dev": "vite",
    "build": "tsc && vite build",
    "preview": "vite preview"
  },
  "dependencies": {
    "@tanstack/react-query": "^5.50.0",
    "axios": "^1.7.0",
    "clsx": "^2.1.1",
    "lucide-react": "^0.400.0",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "react-hook-form": "^7.52.0",
    "react-router-dom": "^6.24.0",
    "tailwind-merge": "^2.3.0",
    "zod": "^3.23.8"
  },
  "devDependencies": {
    "@types/react": "^18.3.3",
    "@types/react-dom": "^18.3.0",
    "@vitejs/plugin-react": "^4.3.1",
    "autoprefixer": "^10.4.19",
    "postcss": "^8.4.38",
    "tailwindcss": "^3.4.4",
    "typescript": "^5.5.3",
    "vite": "^5.3.3"
  }
}
```

---

## 🎨 Core Configuration & Styling

### 1. Tailwind Config (`tailwind.config.js`)

```javascript
/** @type {import('tailwindcss').Config} */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        brand: {
          50: '#f0fdf4',
          100: '#dcfce7',
          500: '#22c55e',
          600: '#16a34a',
          700: '#15803d',
        },
        slate: {
          850: '#151f32',
          900: '#0f172a',
          950: '#020617',
        }
      },
      fontFamily: {
        sans: ['Inter', 'system-ui', 'sans-serif'],
      },
    },
  },
  plugins: [],
};
```

---

### 2. Global Styles & Typography (`src/index.css`)

```css
@tailwind base;
@tailwind components;
@tailwind utilities;

@layer base {
  body {
    @apply bg-slate-50 text-slate-900 dark:bg-slate-950 dark:text-slate-100 font-sans antialiased min-h-screen;
  }
}

/* Custom smooth scroll and scrollbars */
::-webkit-scrollbar {
  width: 6px;
  height: 6px;
}
::-webkit-scrollbar-track {
  background: transparent;
}
::-webkit-scrollbar-thumb {
  @apply bg-slate-300 dark:bg-slate-700 rounded-full;
}
```

---

### 3. Tailwind Merge Utility (`src/utils/cn.ts`)

```typescript
import { type ClassValue, clsx } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}
```

---

## ⚙️ Core Architecture Boilerplates

### 1. Configured Axios Client with Token Injection (`src/api/api.ts`)

```typescript
import axios from 'axios';
import { tokenStorage } from '../utils/tokenStorage';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request Interceptor: Attach Bearer JWT
api.interceptors.request.use(
  (config) => {
    const token = tokenStorage.getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response Interceptor: Catch 401 & Auto Logout / Refresh
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    if (error.response?.status === 401) {
      tokenStorage.clearTokens();
      if (!window.location.pathname.startsWith('/login')) {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);
```

---

### 2. Global Auth Context (`src/contexts/AuthContext.tsx`)

```typescript
import React, { createContext, useContext, useState, useEffect } from 'react';
import { User, LoginRequest, RegisterRequest } from '../types/auth.types';
import { authService } from '../services/authService';
import { tokenStorage } from '../utils/tokenStorage';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  register: (data: RegisterRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const initAuth = async () => {
      const token = tokenStorage.getAccessToken();
      if (token) {
        try {
          const currentUser = await authService.getCurrentUser();
          setUser(currentUser);
        } catch {
          tokenStorage.clearTokens();
        }
      }
      setIsLoading(false);
    };
    initAuth();
  }, []);

  const login = async (credentials: LoginRequest) => {
    const response = await authService.login(credentials);
    tokenStorage.setTokens(response.accessToken, response.refreshToken);
    setUser(response.user);
  };

  const register = async (data: RegisterRequest) => {
    const response = await authService.register(data);
    tokenStorage.setTokens(response.accessToken, response.refreshToken);
    setUser(response.user);
  };

  const logout = () => {
    tokenStorage.clearTokens();
    setUser(null);
    window.location.href = '/login';
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

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used within an AuthProvider');
  return context;
};
```

---

### 3. Protected Route Component (`src/components/common/ProtectedRoute.tsx`)

```typescript
import React from 'react';
import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Skeleton } from './Skeleton';

interface ProtectedRouteProps {
  allowedRoles?: string[];
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ allowedRoles }) => {
  const { user, isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex h-screen w-full items-center justify-center p-6">
        <Skeleton className="h-48 w-full max-w-md rounded-xl" />
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/dashboard" replace />;
  }

  return <Outlet />;
};
```

---

### 4. Reusable UI Button Primitive (`src/components/common/Button.tsx`)

```typescript
import React from 'react';
import { cn } from '../../utils/cn';
import { Loader2 } from 'lucide-react';

interface ButtonProps extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary' | 'outline' | 'danger' | 'ghost';
  size?: 'sm' | 'md' | 'lg';
  isLoading?: boolean;
}

export const Button: React.FC<ButtonProps> = ({
  children,
  className,
  variant = 'primary',
  size = 'md',
  isLoading = false,
  disabled,
  ...props
}) => {
  const baseStyles = 'inline-flex items-center justify-center font-medium rounded-lg transition-all focus:outline-none focus:ring-2 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed';
  
  const variants = {
    primary: 'bg-brand-600 hover:bg-brand-700 text-white focus:ring-brand-500 shadow-sm',
    secondary: 'bg-slate-200 dark:bg-slate-800 text-slate-900 dark:text-slate-100 hover:bg-slate-300 dark:hover:bg-slate-700',
    outline: 'border border-slate-300 dark:border-slate-700 bg-transparent hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-800 dark:text-slate-200',
    danger: 'bg-red-600 hover:bg-red-700 text-white focus:ring-red-500',
    ghost: 'bg-transparent hover:bg-slate-100 dark:hover:bg-slate-800 text-slate-700 dark:text-slate-300',
  };

  const sizes = {
    sm: 'px-2.5 py-1.5 text-xs',
    md: 'px-4 py-2 text-sm',
    lg: 'px-6 py-3 text-base',
  };

  return (
    <button
      className={cn(baseStyles, variants[variant], sizes[size], className)}
      disabled={disabled || isLoading}
      {...props}
    >
      {isLoading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
      {children}
    </button>
  );
};
```
