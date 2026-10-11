import { AnalysisFailure, parseAnalysisResponse, safeFailureCode } from "../api/analysisContract.ts";
import { validateBackendOrigin } from "./backendOrigin.ts";
import { MESSAGE_PREFIX, type MessageSender, type RuntimeBackground } from "./runtimeTransport.ts";

const uuid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/iu;
export function createBackgroundHandler(extensionId: string, backendOrigin: string, fetcher: typeof fetch = fetch) {
  const origin = validateBackendOrigin(backendOrigin);
  const requests = new Map<string, AbortController>();
  return (message: unknown, sender: MessageSender, reply: (response: unknown) => void): boolean | void => {
    if (!message || typeof message !== "object") return;
    const input = message as Record<string, unknown>;
    if (input.type !== `${MESSAGE_PREFIX}:analyze` && input.type !== `${MESSAGE_PREFIX}:cancel`) return;
    // SEC-003: No externally connectable bridge; requests belong to their originating tab/frame.
    if (sender.id !== extensionId || !Number.isInteger(sender.tab?.id) || sender.tab!.id! < 0 || !Number.isInteger(sender.frameId) || sender.frameId! < 0 ||
      typeof input.interaction_id !== "string" || !uuid.test(input.interaction_id)) {
      reply({ ok: false, code: "VALIDATION_FAILED", retryable: false }); return;
    }
    const key = `${sender.tab!.id}:${sender.frameId}:${input.interaction_id}`;
    if (input.type === `${MESSAGE_PREFIX}:cancel`) {
      requests.get(key)?.abort(); reply({ ok: true }); return;
    }
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
    const timer = setTimeout(() => {
      // Complete timeout before abort so an abort callback cannot mask the timeout outcome.
      finish({ ok: false, code: "TIMEOUT", retryable: true }); controller.abort();
    }, 5000);
    void (async () => {
      try {
        const response = await fetcher(`${origin}/api/analysis`, {
          method: "POST", headers: { "Content-Type": "application/json" }, credentials: "omit", redirect: "error",
          body: JSON.stringify({ text: input.text, interaction_id: input.interaction_id }), signal: controller.signal,
        });
        if (replied) return;
        if (!response.ok) {
          let code = "HTTP_ERROR", retryable = response.status >= 500 || response.status === 429;
          try {
            const error = await response.json() as Record<string, unknown>;
            if (typeof error.error_code === "string" && typeof error.request_id === "string" && typeof error.message === "string") {
              code = safeFailureCode(error.error_code); if (typeof error.retryable === "boolean") retryable = error.retryable;
            }
          } catch { /* Fall back to HTTP metadata without retaining backend prose/content. */ }
          finish({ ok: false, code, retryable }); return;
        }
        const value = parseAnalysisResponse(await response.json(), input.interaction_id as string);
        finish({ ok: true, value });
      } catch (error) {
        finish({ ok: false, code: error instanceof AnalysisFailure ? error.code : "NETWORK_ERROR", retryable: true });
      } finally { controller.signal.removeEventListener("abort", abort); }
    })();
    return true;
  };
}
export function registerBackground(runtime: RuntimeBackground, origin: string): void {
  if (runtime.id) runtime.onMessage.addListener(createBackgroundHandler(runtime.id, origin));
}
