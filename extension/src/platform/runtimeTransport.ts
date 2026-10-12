import { AnalysisFailure, parseAnalysisResponse, type AnalysisResponse } from "../api/analysisContract.ts";

export interface RuntimeMessenger {
  readonly id?: string;
  readonly lastError?: { message?: string };
  sendMessage(message: unknown, callback: (response: unknown) => void): unknown;
}
export interface MessageSender { id?: string; tab?: { id?: number }; frameId?: number }
export interface RuntimeBackground extends RuntimeMessenger {
  onMessage: { addListener(listener: (message: unknown, sender: MessageSender, reply: (response: unknown) => void) => boolean | void): void };
}
export function getExtensionRuntime(): RuntimeMessenger | undefined {
  return (globalThis as unknown as { chrome?: { runtime?: RuntimeMessenger } }).chrome?.runtime;
}
export const MESSAGE_PREFIX = "jp-reading-analysis-v1";
export function createRuntimeAnalysisTransport(runtime = getExtensionRuntime()) {
  return (text: string, interactionId: string, signal: AbortSignal): Promise<AnalysisResponse> => new Promise((resolve, reject) => {
    if (!runtime) { reject(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true)); return; }
    if (signal.aborted) { reject(new AnalysisFailure("ABORTED", false)); return; }
    let settled = false;
    const cancelBackground = () => {
      try { runtime.sendMessage({ type: `${MESSAGE_PREFIX}:cancel`, interaction_id: interactionId }, () => { void runtime.lastError; }); }
      catch { /* Cancellation is best effort; interaction identity guards correctness. */ }
    };
    const finish = (error: AnalysisFailure | null, value?: AnalysisResponse) => {
      if (settled) return;
      settled = true; clearTimeout(timer); signal.removeEventListener("abort", abort);
      if (error) reject(error); else resolve(value!);
    };
    const abort = () => { cancelBackground(); finish(new AnalysisFailure("ABORTED", false)); };
    const timer = setTimeout(() => { cancelBackground(); finish(new AnalysisFailure("TIMEOUT", true)); }, 5000);
    signal.addEventListener("abort", abort, { once: true });
    try {
      runtime.sendMessage({ type: `${MESSAGE_PREFIX}:analyze`, text, interaction_id: interactionId }, response => {
        if (settled) return;
        if (runtime.lastError) { finish(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true)); return; }
        const message = response as { ok?: unknown; value?: unknown; code?: unknown; retryable?: unknown } | null;
        if (message?.ok === false && typeof message.code === "string" && typeof message.retryable === "boolean") {
          finish(new AnalysisFailure(message.code, message.retryable)); return;
        }
        try {
          if (message?.ok !== true) throw new AnalysisFailure("INVALID_RESPONSE", true);
          finish(null, parseAnalysisResponse(message.value, interactionId));
        } catch { finish(new AnalysisFailure("INVALID_RESPONSE", true)); }
      });
    } catch { finish(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true)); }
  });
}
