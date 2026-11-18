import { AnalysisFailure, safeFailureCode } from "../api/analysisContract.ts";
import { parseTranslationResponse, type TranslationResponse } from "../api/translationContract.ts";
import { validateBackendOrigin } from "./backendOrigin.ts";
import { getExtensionRuntime, type MessageSender, type RuntimeBackground } from "./runtimeTransport.ts";

export type Translate = (text: string, interactionId: string, signal: AbortSignal) => Promise<TranslationResponse>;
export const TRANSLATION_MESSAGE_PREFIX = "jp-reading-translation-v1";
const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;
const timeoutMs = 15000;

export function createRuntimeTranslationTransport(runtime = getExtensionRuntime()): Translate {
  return (text, interactionId, signal) => new Promise((resolve, reject) => {
    if (!runtime) { reject(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true)); return; }
    if (signal.aborted) { reject(new AnalysisFailure("ABORTED", false)); return; }
    let settled = false;
    const cancel = () => {
      try { runtime.sendMessage({ type: `${TRANSLATION_MESSAGE_PREFIX}:cancel`, interaction_id: interactionId }, () => { void runtime.lastError; }); }
      catch { /* Best effort; component identity independently prevents stale display. */ }
    };
    const finish = (failure: AnalysisFailure | null, result?: TranslationResponse) => {
      if (settled) return;
      settled = true; clearTimeout(timer); signal.removeEventListener("abort", abort);
      if (failure) reject(failure); else resolve(result!);
    };
    const abort = () => { cancel(); finish(new AnalysisFailure("ABORTED", false)); };
    const timer = setTimeout(() => { cancel(); finish(new AnalysisFailure("TIMEOUT", true)); }, timeoutMs);
    signal.addEventListener("abort", abort, { once: true });
    try {
      runtime.sendMessage({ type: `${TRANSLATION_MESSAGE_PREFIX}:translate`, text, interaction_id: interactionId }, response => {
        if (settled) return;
        if (runtime.lastError) { finish(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true)); return; }
        const message = response as { ok?: unknown; value?: unknown; code?: unknown; retryable?: unknown } | null;
        if (message?.ok === false && typeof message.code === "string" && typeof message.retryable === "boolean") {
          finish(new AnalysisFailure(message.code, message.retryable)); return;
        }
        try {
          if (message?.ok !== true) throw new AnalysisFailure("INVALID_RESPONSE", true);
          finish(null, parseTranslationResponse(message.value, interactionId));
        } catch { finish(new AnalysisFailure("INVALID_RESPONSE", true)); }
      });
    } catch { finish(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true)); }
  });
}

/** Only extension-owned tab/frame requests can access the fixed backend endpoint. */
export function createTranslationBackgroundHandler(extensionId: string, backendOrigin: string, fetcher: typeof fetch = fetch) {
  const origin = validateBackendOrigin(backendOrigin);
  const requests = new Map<string, AbortController>();
  return (message: unknown, sender: MessageSender, reply: (response: unknown) => void): boolean | void => {
    if (!message || typeof message !== "object") return;
    const input = message as Record<string, unknown>;
    if (input.type !== `${TRANSLATION_MESSAGE_PREFIX}:translate` && input.type !== `${TRANSLATION_MESSAGE_PREFIX}:cancel`) return;
    if (sender.id !== extensionId || !Number.isInteger(sender.tab?.id) || sender.tab!.id! < 0 ||
        !Number.isInteger(sender.frameId) || sender.frameId! < 0 || typeof input.interaction_id !== "string" || !uuid.test(input.interaction_id)) {
      reply({ ok: false, code: "VALIDATION_FAILED", retryable: false }); return;
    }
    const key = `${sender.tab!.id}:${sender.frameId}:${input.interaction_id}`;
    if (input.type === `${TRANSLATION_MESSAGE_PREFIX}:cancel`) { requests.get(key)?.abort(); reply({ ok: true }); return; }
    if (typeof input.text !== "string" || input.text.trim().length === 0 || input.text.trim().length > 1000 || requests.has(key)) {
      reply({ ok: false, code: "VALIDATION_FAILED", retryable: false }); return;
    }
    const controller = new AbortController(); requests.set(key, controller);
    let replied = false;
    const finish = (result: unknown) => {
      if (replied) return;
      replied = true; clearTimeout(timer); requests.delete(key); reply(result);
    };
    const abort = () => finish({ ok: false, code: "ABORTED", retryable: false });
    controller.signal.addEventListener("abort", abort, { once: true });
    const timer = setTimeout(() => { finish({ ok: false, code: "TIMEOUT", retryable: true }); controller.abort(); }, timeoutMs);
    void (async () => {
      try {
        const response = await fetcher(`${origin}/api/translations`, {
          method: "POST", headers: { "Content-Type": "application/json" }, credentials: "omit", redirect: "error",
          body: JSON.stringify({ text: input.text, interaction_id: input.interaction_id, target_language: "en" }), signal: controller.signal,
        });
        if (replied) return;
        if (!response.ok) {
          let code = "HTTP_ERROR", retryable = response.status >= 500 || response.status === 429;
          try {
            const error = await response.json() as Record<string, unknown>;
            if (typeof error.error_code === "string" && typeof error.request_id === "string" && typeof error.message === "string") {
              code = safeFailureCode(error.error_code); if (typeof error.retryable === "boolean") retryable = error.retryable;
            }
          } catch { /* No provider or backend prose is exposed. */ }
          finish({ ok: false, code, retryable }); return;
        }
        const value = parseTranslationResponse(await response.json(), input.interaction_id as string);
        finish({ ok: true, value });
      } catch (error) {
        finish({ ok: false, code: error instanceof AnalysisFailure ? error.code : "NETWORK_ERROR", retryable: true });
      } finally { controller.signal.removeEventListener("abort", abort); }
    })();
    return true;
  };
}

export function registerTranslationBackground(runtime: RuntimeBackground, origin: string): void {
  if (runtime.id) runtime.onMessage.addListener(createTranslationBackgroundHandler(runtime.id, origin));
}
