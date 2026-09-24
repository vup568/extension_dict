export const RUBY_SAFE_SELECTION_ERROR_CODES = [
  "no-selection",
  "unsupported-multi-range",
  "no-base-text",
  "stale-selection",
  "inaccessible-context",
  "unexpected-failure",
] as const;

export type RubySafeSelectionErrorCode =
  (typeof RUBY_SAFE_SELECTION_ERROR_CODES)[number];

export interface RubySafeSelectionSuccess {
  readonly status: "ok";
  readonly text: string;
}

export interface RubySafeSelectionError {
  readonly status: "error";
  readonly code: RubySafeSelectionErrorCode;
}

export type RubySafeSelectionResult =
  | RubySafeSelectionSuccess
  | RubySafeSelectionError;
