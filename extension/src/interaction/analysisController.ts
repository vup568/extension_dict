import { AnalysisFailure, type AnalysisResponse } from "../api/analysisContract.ts";

export type AnalysisState =
  | { readonly phase: "hidden" }
  | { readonly phase: "loading"; readonly text: string; readonly interactionId: string }
  | { readonly phase: "result"; readonly text: string; readonly interactionId: string; readonly response: AnalysisResponse }
  | { readonly phase: "error"; readonly text: string; readonly interactionId: string; readonly code: string; readonly retryable: boolean };

export type Analyze = (text: string, interactionId: string, signal: AbortSignal) => Promise<AnalysisResponse>;

export interface AnalysisControllerOptions {
  readonly analyze: Analyze;
  readonly createId: () => string;
  readonly onState: (state: AnalysisState) => void;
}

/** Browser-independent currentness boundary. Text lives only in the active state. */
export class AnalysisController {
  private sequence = 0;
  private timer: ReturnType<typeof setTimeout> | undefined;
  private deadline: ReturnType<typeof setTimeout> | undefined;
  private request: AbortController | undefined;
  private current: AnalysisState = { phase: "hidden" };

  constructor(private readonly options: AnalysisControllerOptions) {}

  get state(): AnalysisState { return this.current; }

  select(text: string | null, debounce = true): void {
    // EARS[Event]: WHEN selection changes, invalidate immediately, before debounce [ASYNC-002].
    this.invalidate();
    if (text === null || text.trim().length === 0) {
      this.publish({ phase: "hidden" });
      return;
    }
    const interactionId = this.options.createId();
    if (text.trim().length > 1000) {
      this.publish({ phase: "error", text, interactionId, code: "INPUT_TOO_LONG", retryable: false });
      return;
    }
    // EARS[Event]: WHEN input is valid, render shell before sending analysis [EXT-003].
    this.publish({ phase: "loading", text, interactionId });
    const sequence = this.sequence;
    this.timer = setTimeout(() => {
      this.timer = undefined;
      this.dispatch(text, interactionId, sequence);
    }, debounce ? 300 : 0);
  }

  retry(): void {
    // EARS[Event]: WHEN retry is requested, use a fresh interaction identity [NET-005].
    if (this.current.phase === "error" && this.current.retryable) {
      this.select(this.current.text, false);
    }
  }

  close(): void { this.select(null); }

  private publish(state: AnalysisState): void {
    this.current = state;
    this.options.onState(state);
  }

  private invalidate(): void {
    this.sequence++;
    clearTimeout(this.timer);
    clearTimeout(this.deadline);
    this.timer = undefined;
    this.deadline = undefined;
    const request = this.request;
    this.request = undefined;
    request?.abort();
  }

  private dispatch(text: string, interactionId: string, sequence: number): void {
    if (sequence !== this.sequence) return;
    const request = new AbortController();
    this.request = request;
    // EARS[Unwanted]: IF transport hangs, fail within 5s even if abort is ignored [PERF-004].
    this.deadline = setTimeout(() => {
      if (sequence !== this.sequence) return;
      this.invalidate();
      this.publish({ phase: "error", text, interactionId, code: "TIMEOUT", retryable: true });
    }, 5000);
    let result: Promise<AnalysisResponse>;
    try { result = this.options.analyze(text, interactionId, request.signal); }
    catch (error: unknown) { result = Promise.reject(error); }
    result.then(
      (response) => {
        if (sequence !== this.sequence) return;
        clearTimeout(this.deadline);
        this.deadline = undefined;
        this.request = undefined;
        if (response.meta.interaction_id !== interactionId) {
          this.publish({ phase: "error", text, interactionId, code: "INVALID_RESPONSE", retryable: true });
          return;
        }
        this.publish({ phase: "result", text, interactionId, response });
      },
      (error: unknown) => {
        // EARS[Unwanted]: Old errors, including ignored cancellation, never update UI [ASYNC-002].
        if (sequence !== this.sequence) return;
        clearTimeout(this.deadline);
        this.deadline = undefined;
        this.request = undefined;
        const failure = error instanceof AnalysisFailure ? error : new AnalysisFailure("NETWORK_ERROR", true);
        this.publish({ phase: "error", text, interactionId, code: failure.code, retryable: failure.retryable });
      },
    );
  }
}
