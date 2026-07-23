export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  code?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  [extension: string]: unknown;
};

export class ApiError extends Error {
  public readonly status: number;
  public readonly code: string;
  public readonly traceId: string | undefined;
  public readonly fieldErrors: Record<string, string[]>;
  public readonly title: string | undefined;
  public readonly detail: string | undefined;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? 'Yêu cầu không thành công.');
    this.status = status;
    this.code = typeof problem.code === 'string' ? problem.code : 'requestError';
    this.traceId = typeof problem.traceId === 'string' ? problem.traceId : undefined;
    this.fieldErrors = problem.errors ?? {};
    this.title = problem.title;
    this.detail = problem.detail;
  }

  public get userMessage(): string {
    if (this.fieldErrors && Object.keys(this.fieldErrors).length > 0) {
      const firstField = Object.values(this.fieldErrors)[0]?.[0];
      if (firstField) return firstField;
    }
    return this.detail ?? this.title ?? 'Không thể hoàn thành yêu cầu.';
  }
}

export function isApiError(error: unknown): error is ApiError {
  return error instanceof ApiError;
}
