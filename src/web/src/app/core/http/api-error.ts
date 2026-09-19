import { HttpErrorResponse } from '@angular/common/http';

/**
 * The shape the UI speaks. Transport concerns (status codes, Problem Details) stop
 * at the data-access edge; pages and stores only ever see an `ApiError`.
 */
export interface ApiError {
  /** What kind of failure this is, for the UI to branch on. */
  readonly kind: ApiErrorKind;
  /** HTTP status, or 0 when the request never reached the server. */
  readonly status: number;
  /** Stable machine-readable code from the API, when it sent one. */
  readonly code: string | null;
  /** Short human-readable summary. */
  readonly title: string;
  /** Longer human-readable explanation, when the API sent one. */
  readonly detail: string | null;
  /** Correlation id for support, from the Problem Details `traceId`. */
  readonly traceId: string | null;
  /** Field-level messages, keyed by form control name. Empty unless `kind` is `validation`. */
  readonly fieldErrors: Readonly<Record<string, readonly string[]>>;
}

export type ApiErrorKind =
  | 'validation'
  | 'unauthorized'
  | 'forbidden'
  | 'notFound'
  | 'conflict'
  | 'offline'
  | 'server';

/** RFC 9457 Problem Details, as the API returns it. */
interface ProblemDetails {
  readonly title?: string;
  readonly detail?: string;
  readonly status?: number;
  readonly code?: string;
  readonly traceId?: string;
  readonly errors?: Record<string, string[]>;
}

const TITLES: Record<ApiErrorKind, string> = {
  validation: 'Please check the highlighted fields.',
  unauthorized: 'Please sign in again.',
  forbidden: 'You do not have access to this.',
  notFound: 'We could not find that.',
  conflict: 'That change conflicts with the current state.',
  offline: 'We could not reach Coffeeg. Check your connection.',
  server: 'Something went wrong on our side.',
};

/** The single place an HTTP failure becomes a domain-shaped error. */
export function toApiError(response: HttpErrorResponse): ApiError {
  const kind = toKind(response.status);
  const problem: ProblemDetails = isProblemDetails(response.error) ? response.error : {};

  return {
    kind,
    status: response.status,
    code: problem.code ?? null,
    title: problem.title ?? TITLES[kind],
    detail: problem.detail ?? null,
    traceId: problem.traceId ?? null,
    fieldErrors: problem.errors ?? {},
  };
}

/** True when the value came from this application's own error mapping. */
export function isApiError(value: unknown): value is ApiError {
  return typeof value === 'object' && value !== null && 'kind' in value && 'status' in value;
}

/**
 * Server-side messages for one form control.
 *
 * The API keys its validation failures by C# property name (`UserName`), while form
 * controls are camelCase (`userName`), so the lookup ignores case rather than making
 * every template know both spellings.
 */
export function fieldErrorsFor(error: ApiError | null, field: string): readonly string[] {
  if (error === null) {
    return [];
  }

  const key = Object.keys(error.fieldErrors).find(
    (candidate) => candidate.toLowerCase() === field.toLowerCase(),
  );

  return key === undefined ? [] : error.fieldErrors[key];
}

function toKind(status: number): ApiErrorKind {
  if (status === 0) return 'offline';
  if (status === 400 || status === 422) return 'validation';
  if (status === 401) return 'unauthorized';
  if (status === 403) return 'forbidden';
  if (status === 404) return 'notFound';
  if (status === 409) return 'conflict';
  return 'server';
}

function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null;
}
