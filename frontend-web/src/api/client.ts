import { ApiError, ProblemDetails } from './errors';
import type { SchoolRole } from '../shared/config';

type LogoutHandler = () => Promise<void> | void;

let accessToken: string | null = null;
let logoutHandler: LogoutHandler = () => {
  accessToken = null;
};
let refreshPromise: Promise<boolean> | null = null;

export function registerLogoutHandler(handler: LogoutHandler): void {
  logoutHandler = handler;
}

export function setAccessToken(token: string | null): void {
  accessToken = token;
}

export function getAccessToken(): string | null {
  return accessToken;
}

export type ApiRequestOptions = {
  method?: 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';
  body?: unknown;
  formData?: FormData;
  headers?: Record<string, string>;
  signal?: AbortSignal;
  skipAuth?: boolean;
};

const problemContentType = /^application\/problem\+json/i;

async function readProblem(response: Response): Promise<ProblemDetails> {
  const contentType = response.headers.get('content-type') ?? '';
  if (problemContentType.test(contentType)) {
    try {
      return (await response.json()) as ProblemDetails;
    } catch {
      return {};
    }
  }
  try {
    const text = await response.text();
    return { detail: text || undefined };
  } catch {
    return {};
  }
}

async function performRefresh(): Promise<boolean> {
  try {
    const response = await fetch(buildUrl('/api/v1/auth/refresh'), {
      method: 'POST',
      credentials: 'include',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ clientType: 'web' }),
    });
    if (!response.ok) return false;
    const payload = (await response.json()) as AuthSessionResponse;
    setAccessToken(payload.accessToken);
    return true;
  } catch {
    return false;
  }
}

function attemptRefresh(): Promise<boolean> {
  if (refreshPromise === null) {
    refreshPromise = performRefresh().finally(() => {
      refreshPromise = null;
    });
  }
  return refreshPromise;
}

async function clearRefreshSession(): Promise<void> {
  try {
    await fetch(buildUrl('/api/v1/auth/logout'), {
      method: 'POST',
      credentials: 'include',
      headers: { 'content-type': 'application/json' },
      body: JSON.stringify({ clientType: 'web' }),
    });
  } catch {
    // Local state is still cleared when the server is unreachable.
  }
}

function buildUrl(path: string): string {
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  const normalized = path.startsWith('/') ? path : `/${path}`;
  return `${__API_BASE_URL__}${normalized}`;
}

export type AuthSessionResponse = {
  userId: string;
  displayName: string;
  roles: SchoolRole[];
  passwordChangeRequired: boolean;
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string | null;
  refreshTokenExpiresAtUtc: string | null;
};

export type SessionContextResponse = {
  userId: string;
  displayName: string;
  roles: SchoolRole[];
  passwordChangeRequired: boolean;
};

export async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const { method = 'GET', body, formData, headers = {}, signal, skipAuth } = options;

  const execute = async (): Promise<Response> => {
    const requestHeaders: Record<string, string> = { ...headers };
    let payload: BodyInit | undefined;
    if (body !== undefined) {
      requestHeaders['content-type'] = 'application/json';
      payload = JSON.stringify(body);
    } else if (formData) {
      payload = formData;
    }
    if (!skipAuth) {
      const token = getAccessToken();
      if (token) requestHeaders['authorization'] = `Bearer ${token}`;
    }
    requestHeaders['accept'] = 'application/json';
    return fetch(buildUrl(path), {
      method,
      headers: requestHeaders,
      body: payload,
      credentials: 'include',
      signal,
    });
  };

  let response = await execute();
  if (response.status === 401 && !skipAuth) {
    const refreshed = await attemptRefresh();
    if (refreshed) response = await execute();
  }

  if (response.status === 204) {
    return undefined as T;
  }

  if (!response.ok) {
    if (response.status === 401 && !skipAuth) {
      await clearRefreshSession();
      await logoutHandler();
    }
    const problem = await readProblem(response);
    throw new ApiError(response.status, problem);
  }

  const contentType = response.headers.get('content-type') ?? '';
  if (contentType.includes('application/json')) {
    return (await response.json()) as T;
  }
  return (await response.text()) as unknown as T;
}

export async function apiDownload(path: string, suggestedFileName: string): Promise<void> {
  const headers: Record<string, string> = {};
  const token = getAccessToken();
  if (token) headers['authorization'] = `Bearer ${token}`;
  const response = await fetch(buildUrl(path), {
    method: 'GET',
    headers,
    credentials: 'include',
  });
  if (!response.ok) {
    const problem = await readProblem(response);
    throw new ApiError(response.status, problem);
  }
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = suggestedFileName;
  document.body.appendChild(anchor);
  anchor.click();
  document.body.removeChild(anchor);
  URL.revokeObjectURL(url);
}
