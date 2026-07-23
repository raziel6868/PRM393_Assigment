import { apiRequest, type AuthSessionResponse, type SessionContextResponse } from './client';

export type SignInRequest = {
  emailOrUserName: string;
  password: string;
  clientType: 'web';
};

export type ChangeTemporaryPasswordRequest = {
  currentPassword: string;
  newPassword: string;
  confirmation: string;
};

export type PasswordHelpSubmissionRequest = {
  emailOrUserName: string;
};

export type PasswordHelpAcceptedResponse = {
  message: string;
};

export const authApi = {
  signIn(body: SignInRequest) {
    return apiRequest<AuthSessionResponse>('/api/v1/auth/sign-in', {
      method: 'POST',
      body,
      skipAuth: true,
    });
  },
  logout() {
    return apiRequest<void>('/api/v1/auth/logout', {
      method: 'POST',
      body: { clientType: 'web' },
    });
  },
  session() {
    return apiRequest<SessionContextResponse>('/api/v1/auth/session');
  },
  changeTemporaryPassword(body: ChangeTemporaryPasswordRequest) {
    return apiRequest<void>('/api/v1/auth/change-temporary-password', {
      method: 'POST',
      body,
    });
  },
  submitPasswordHelp(body: PasswordHelpSubmissionRequest) {
    return apiRequest<PasswordHelpAcceptedResponse>('/api/v1/auth/password-help-requests', {
      method: 'POST',
      body,
      skipAuth: true,
    });
  },
  refreshSession() {
    return apiRequest<AuthSessionResponse>('/api/v1/auth/refresh', {
      method: 'POST',
      body: { clientType: 'web' },
      skipAuth: true,
    });
  },
};
