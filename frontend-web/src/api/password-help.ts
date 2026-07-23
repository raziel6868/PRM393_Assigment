import { apiRequest } from './client';

export type PasswordHelpStatus = 'pending' | 'resolved' | 'rejected';

export type PasswordHelpItem = {
  requestId: string;
  userId: string;
  displayName: string;
  userName: string;
  email: string | null;
  status: PasswordHelpStatus;
  requestedAtUtc: string;
  resolvedAtUtc: string | null;
  rowVersion: string;
};

export type PasswordHelpPage = {
  items: PasswordHelpItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

export type IssueTemporaryPasswordResponse = {
  userId: string;
  temporaryPassword: string;
  expiresAtUtc: string;
};

export const passwordHelpApi = {
  listPending(page = 1, pageSize = 20) {
    return apiRequest<PasswordHelpPage>(
      `/api/v1/admin/password-help-requests?status=pending&page=${page}&pageSize=${pageSize}`,
    );
  },
  listByStatus(status: PasswordHelpStatus, page = 1, pageSize = 20) {
    return apiRequest<PasswordHelpPage>(
      `/api/v1/admin/password-help-requests?status=${status}&page=${page}&pageSize=${pageSize}`,
    );
  },
  reject(requestId: string, rowVersion: string) {
    return apiRequest<void>(
      `/api/v1/admin/password-help-requests/${requestId}/reject`,
      { method: 'POST', body: { confirmed: true, rowVersion } },
    );
  },
  issueTemporaryPassword(userId: string, rowVersion: string) {
    return apiRequest<IssueTemporaryPasswordResponse>(
      `/api/v1/admin/users/${userId}/issue-temporary-password`,
      { method: 'POST', body: { confirmed: true, rowVersion } },
    );
  },
};
