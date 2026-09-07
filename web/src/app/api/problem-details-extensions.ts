import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './problem-details';

export function problemMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'Cannot reach the server. Check that the API is running.';
  }

  const problem = error.error as ProblemDetails | null;

  return problem?.detail ?? problem?.title ?? fallback;
}

export function serverFieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof HttpErrorResponse)) {
    return {};
  }

  const problem = error.error as ProblemDetails | null;

  return (problem?.errors ?? []).reduce<Record<string, string>>((accumulated, item) => {
    const field = item.code.charAt(0).toLowerCase() + item.code.slice(1);
    accumulated[field] = item.description;

    return accumulated;
  }, {});
}
