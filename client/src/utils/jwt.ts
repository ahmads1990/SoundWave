import { User, UserRole } from '../types/auth.types';

export interface JwtPayload {
  [key: string]: unknown;
}

export function parseJwt(token: string): JwtPayload | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;

    const base64Url = parts[1];
    let base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    while (base64.length % 4 !== 0) {
      base64 += '=';
    }

    const jsonPayload = decodeURIComponent(
      window
        .atob(base64)
        .split('')
        .map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2))
        .join('')
    );
    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

export function extractUserFromToken(token: string): User | null {
  const payload = parseJwt(token);
  if (!payload) return null;

  try {
    const id =
      (payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'] as string) ||
      (payload['nameid'] as string) ||
      (payload['sub'] as string) ||
      '';

    const email =
      (payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'] as string) ||
      (payload['email'] as string) ||
      '';

    const rawRole =
      (payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] as string) ||
      (payload['role'] as string) ||
      'Listener';
    const role: UserRole = rawRole === 'Admin' || rawRole === 'Artist' ? rawRole : 'Listener';

    const userName =
      (payload['username'] as string) ||
      (payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] as string) ||
      (payload['unique_name'] as string) ||
      (payload['name'] as string) ||
      (email ? email.split('@')[0] : 'User');

    return {
      id,
      email,
      userName,
      role,
      isEmailVerified: true,
    };
  } catch {
    return null;
  }
}

export function isTokenExpired(token: string): boolean {
  const decoded = parseJwt(token);
  if (!decoded || typeof decoded.exp !== 'number') return true;
  const currentTime = Math.floor(Date.now() / 1000);
  return decoded.exp < currentTime;
}
