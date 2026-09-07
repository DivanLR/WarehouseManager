import { ProblemError } from './problem-error';

export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: ProblemError[];
}
