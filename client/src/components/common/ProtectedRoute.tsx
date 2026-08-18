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
      <div className="flex h-full w-full items-center justify-center p-8 bg-spotify-base">
        <div className="space-y-4 w-72">
          <Skeleton className="h-12 w-full rounded-xl" />
          <Skeleton className="h-4 w-3/4 rounded-md" />
          <Skeleton className="h-4 w-1/2 rounded-md" />
        </div>
      </div>
    );
  }

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
};
