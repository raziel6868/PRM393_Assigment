import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import {
  authApi,
  type ChangeTemporaryPasswordRequest,
  type SignInRequest,
} from '../api/auth';
import {
  configureAccessToken,
  getAccessToken,
  setAccessToken,
  type AuthSessionResponse,
} from '../api/client';
import { isApiError } from '../api/errors';
import type { SchoolRole } from '../shared/config';

type SessionUser = {
  userId: string;
  displayName: string;
  roles: SchoolRole[];
  passwordChangeRequired: boolean;
};

export type AuthState =
  | { status: 'unknown' }
  | { status: 'unauthenticated' }
  | { status: 'authenticated'; user: SessionUser; restricted: boolean }
  | { status: 'restricted'; user: SessionUser };

type AuthContextValue = {
  state: AuthState;
  signIn: (input: SignInRequest) => Promise<AuthSessionResponse>;
  logout: () => Promise<void>;
  refresh: () => Promise<void>;
  changeTemporaryPassword: (input: ChangeTemporaryPasswordRequest) => Promise<void>;
};

const ACCESS_TOKEN_KEY = 'myfschool.web.access';

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }): JSX.Element {
  const [state, setState] = useState<AuthState>({ status: 'unknown' });
  const restoreAttempted = useRef(false);

  const applySession = useCallback((session: { userId: string; displayName: string; roles: SchoolRole[]; passwordChangeRequired: boolean }) => {
    const user: SessionUser = {
      userId: session.userId,
      displayName: session.displayName,
      roles: session.roles,
      passwordChangeRequired: session.passwordChangeRequired,
    };
    setState(
      session.passwordChangeRequired
        ? { status: 'restricted', user }
        : { status: 'authenticated', user, restricted: false },
    );
  }, []);

  const clearSession = useCallback(() => {
    setAccessToken(null);
    setState({ status: 'unauthenticated' });
  }, []);

  const restore = useCallback(async () => {
    if (restoreAttempted.current) return;
    restoreAttempted.current = true;
    try {
      const session = await authApi.refreshSession();
      setAccessToken(session.accessToken);
      applySession(session);
    } catch (error) {
      if (isApiError(error) && error.status === 401) {
        clearSession();
        return;
      }
      clearSession();
    }
  }, [applySession, clearSession]);

  useEffect(() => {
    configureAccessToken(
      () => sessionStorage.getItem(ACCESS_TOKEN_KEY),
      (token) => {
        if (token === null) sessionStorage.removeItem(ACCESS_TOKEN_KEY);
        else sessionStorage.setItem(ACCESS_TOKEN_KEY, token);
      },
    );
    void restore();
  }, [restore]);

  const signIn = useCallback<AuthContextValue['signIn']>(
    async (input) => {
      const response = await authApi.signIn(input);
      setAccessToken(response.accessToken);
      applySession(response);
      return response;
    },
    [applySession],
  );

  const logout = useCallback<AuthContextValue['logout']>(async () => {
    try {
      await authApi.logout();
    } catch (error) {
      if (!isApiError(error)) throw error;
    } finally {
      clearSession();
    }
  }, [clearSession]);

  const refresh = useCallback<AuthContextValue['refresh']>(async () => {
    try {
      const session = await authApi.refreshSession();
      setAccessToken(session.accessToken);
      applySession(session);
    } catch (error) {
      clearSession();
    }
  }, [applySession, clearSession]);

  const changeTemporaryPassword = useCallback<AuthContextValue['changeTemporaryPassword']>(
    async (input) => {
      const token = getAccessToken();
      if (!token) throw new Error('Phiên đăng nhập không khả dụng.');
      await authApi.changeTemporaryPassword(input);
      clearSession();
    },
    [clearSession],
  );

  const value = useMemo<AuthContextValue>(
    () => ({ state, signIn, logout, refresh, changeTemporaryPassword }),
    [state, signIn, logout, refresh, changeTemporaryPassword],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside <AuthProvider>.');
  return context;
}

export function isRestricted(state: AuthState): boolean {
  return state.status === 'restricted';
}

export function isAuthenticated(state: AuthState): boolean {
  return state.status === 'authenticated';
}

export function isLoading(state: AuthState): boolean {
  return state.status === 'unknown';
}

export function userRoles(state: AuthState): SchoolRole[] {
  return state.status === 'authenticated' || state.status === 'restricted' ? state.user.roles : [];
}
