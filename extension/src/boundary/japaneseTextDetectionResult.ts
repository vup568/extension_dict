export type JapaneseTextDetectionErrorCode =
  | "invalid-input"
  | "unexpected-failure";

export interface JapaneseTextDetectionSuccess {
  readonly status: "ok";
  readonly containsJapaneseText: boolean;
}

export interface JapaneseTextDetectionError {
  readonly status: "error";
  readonly code: JapaneseTextDetectionErrorCode;
}

export type JapaneseTextDetectionResult =
  | JapaneseTextDetectionSuccess
  | JapaneseTextDetectionError;
