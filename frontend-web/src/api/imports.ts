import { apiDownload, apiRequest } from './client';

export type ImportTemplateInfo = {
  templateVersion: string;
  contentType: string;
  fileName: string;
  sheets: string[];
  sheetHeaders: Record<string, string[]>;
  instructionsSheet: string;
};

export type ImportBatchStatus =
  | 'uploaded'
  | 'validated'
  | 'committed'
  | 'failed'
  | 'rejected';

export type ImportBatchSummary = {
  batchId: string;
  fileName: string;
  fileSizeBytes: number;
  status: ImportBatchStatus;
  hasBlockingErrors: boolean;
  rowCount: number;
  createdUserCount: number;
  updatedUserCount: number;
  createdProfileCount: number;
  createdLinkCount: number;
  createdAssignmentCount: number;
  createdEnrollmentCount: number;
  failureReason: string | null;
  createdAtUtc: string;
  validatedAtUtc: string | null;
  committedAtUtc: string | null;
  rowVersion: string;
};

export type ImportValidation = {
  batchId: string;
  hasBlockingErrors: boolean;
  totalRowCount: number;
  validRowCount: number;
  warningRowCount: number;
  errorRowCount: number;
  errors: ImportValidationError[];
};

export type ImportValidationError = {
  sheet: string;
  rowNumber: number;
  column: string;
  code: string;
  message: string;
  severity: 'error' | 'warning';
};

export type ImportCommitResult = ImportBatchSummary;

export const importsApi = {
  templateInfo() {
    return apiRequest<ImportTemplateInfo>('/api/v1/admin/imports/template/info');
  },
  downloadTemplate() {
    return apiDownload('/api/v1/admin/imports/template', 'mau-nhap-lieu.xlsx');
  },
  upload(file: File) {
    const form = new FormData();
    form.append('file', file, file.name);
    return apiRequest<ImportBatchSummary>('/api/v1/admin/imports', {
      method: 'POST',
      formData: form,
    });
  },
  validate(batchId: string) {
    return apiRequest<ImportValidation>(
      `/api/v1/admin/imports/${batchId}/validate`,
      { method: 'POST' },
    );
  },
  commit(batchId: string) {
    return apiRequest<ImportCommitResult>(
      `/api/v1/admin/imports/${batchId}/commit`,
      { method: 'POST' },
    );
  },
  result(batchId: string) {
    return apiRequest<ImportBatchSummary>(`/api/v1/admin/imports/${batchId}/result`);
  },
};
