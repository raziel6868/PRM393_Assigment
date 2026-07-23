import { Navigate, useLocation } from 'react-router-dom';
import { type ReactNode } from 'react';
import {
  isAuthenticated,
  isLoading,
  isRestricted,
  useAuth,
  type AuthState,
} from './auth-context';
import type { SchoolRole } from '../shared/config';

type GuardProps = {
  children: ReactNode;
  roles?: readonly SchoolRole[];
};

function rolesOf(state: AuthState): SchoolRole[] {
  if (state.status === 'authenticated' || state.status === 'restricted') {
    return state.user.roles;
  }
  return [];
}

export function SessionGuard({ children }: GuardProps): JSX.Element {
  const { state } = useAuth();
  const location = useLocation();

  if (isLoading(state)) {
    return <div role="status" aria-live="polite" />;
  }

  if (isRestricted(state)) {
    // The forced-change route must be reachable even when the restricted
    // session is still active; everything else must funnel into it.
    if (location.pathname === '/change-temporary-password') {
      return <>{children}</>;
    }
    return <Navigate to="/change-temporary-password" replace state={{ from: location }} />;
  }

  if (!isAuthenticated(state)) {
    return <Navigate to="/login" replace state={{ from: location }} />;
  }

  return <>{children}</>;
}

export function AnonymousGuard({ children }: GuardProps): JSX.Element {
  const { state } = useAuth();
  const location = useLocation();

  if (isLoading(state)) {
    return <div role="status" aria-live="polite" />;
  }

  if (isAuthenticated(state)) {
    const from = (location.state as { from?: Location } | null)?.from?.pathname;
    return <Navigate to={from ?? '/dashboard'} replace />;
  }

  if (isRestricted(state)) {
    return <Navigate to="/change-temporary-password" replace />;
  }

  return <>{children}</>;
}

export function RoleGuard({ children, roles }: GuardProps): JSX.Element {
  const { state } = useAuth();

  if (isLoading(state)) {
    return <div role="status" aria-live="polite" />;
  }
  if (!isAuthenticated(state)) {
    return <Navigate to="/login" replace />;
  }
  if (isRestricted(state)) {
    return <Navigate to="/change-temporary-password" replace />;
  }
  if (roles && roles.length > 0) {
    const userRoles = rolesOf(state);
    const hasRole = userRoles.some((role: SchoolRole) => roles.includes(role));
    if (!hasRole) {
      return <Navigate to="/dashboard" replace />;
    }
  }
  return <>{children}</>;
}
