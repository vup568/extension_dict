import { AnalysisFailure, safeFailureCode } from "../api/analysisContract.ts";
import { isScanInput, isSpeechSource, parseScanResponse, parseSpeechResponse, readingUuid, speechWireLimit,
  type ScanInput, type ScanResponse, type SpeechResponse, type SpeechSource } from "../api/readingContracts.ts";
import { validateBackendOrigin } from "./backendOrigin.ts";
import { getExtensionRuntime, type MessageSender, type RuntimeBackground, type RuntimeMessenger } from "./runtimeTransport.ts";

export type Scan = (input: ScanInput, id: string, signal: AbortSignal) => Promise<ScanResponse>;
export type Speak = (source: SpeechSource, id: string, signal: AbortSignal) => Promise<SpeechResponse>;
const scanPrefix = "jp-reading-scan-v1";
const speechPrefix = "jp-reading-speech-v1";

function request<T>(runtime: RuntimeMessenger | undefined, prefix: string, input: object, id: string, signal: AbortSignal,
  timeout: number, parse: (value: unknown) => T): Promise<T> {
  return new Promise((resolve, reject) => {
    if (!runtime) { reject(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true)); return; }
    if (signal.aborted) { reject(new AnalysisFailure("ABORTED", false)); return; }
    let done = false;
    function finish(callback: () => void) {
      if (done) return;
      done = true;
      clearTimeout(timer);
      signal.removeEventListener("abort", abort);
      callback();
    }
    function cancel(code: "ABORTED" | "TIMEOUT") {
      finish(() => {
        reject(new AnalysisFailure(code, code === "TIMEOUT"));
        try { runtime?.sendMessage({ type: `${prefix}:cancel`, interaction_id: id }, () => { void runtime.lastError; }); }
        catch { /* The current UI has already settled. */ }
      });
    }
    const abort = () => cancel("ABORTED");
    const timer = setTimeout(() => cancel("TIMEOUT"), timeout);
    signal.addEventListener("abort", abort, { once: true });
    try {
      runtime.sendMessage({ type: prefix, interaction_id: id, ...input }, raw => {
        const lastError = runtime.lastError;
        finish(() => {
          if (lastError) { reject(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true)); return; }
          const reply = raw as { ok?: unknown; value?: unknown; code?: unknown; retryable?: unknown } | null;
          if (reply?.ok === false && typeof reply.code === "string" && typeof reply.retryable === "boolean") {
            reject(new AnalysisFailure(reply.code, reply.retryable)); return;
          }
          try {
            if (reply?.ok !== true) throw new AnalysisFailure("INVALID_RESPONSE", true);
            resolve(parse(reply.value));
          } catch { reject(new AnalysisFailure("INVALID_RESPONSE", true)); }
        });
      });
    } catch { finish(() => reject(new AnalysisFailure("TRANSPORT_UNAVAILABLE", true))); }
  });
}

export function createRuntimeScan(runtime = getExtensionRuntime()): Scan {
  return (input, id, signal) => request(runtime, scanPrefix, input, id, signal, 5000, value => parseScanResponse(value, id, input));
}

export function createRuntimeSpeech(runtime = getExtensionRuntime()): Speak {
  return (source, id, signal) => request(runtime, speechPrefix, { source }, id, signal, 32000, value => parseSpeechResponse(value, id));
}

async function readJson(response: Response, limit: number): Promise<unknown> {
  if (Number(response.headers.get("Content-Length")) > limit || response.body === null) throw new AnalysisFailure("INVALID_RESPONSE", true);
  const reader = response.body.getReader();
  const decoder = new TextDecoder("utf-8", { fatal: true });
  let bytes = 0;
  let json = "";
  try {
    for (;;) {
      const chunk = await reader.read();
      if (chunk.done) break;
      bytes += chunk.value.byteLength;
      if (bytes > limit) {
        void reader.cancel().catch(() => {});
        throw new AnalysisFailure("INVALID_RESPONSE", true);
      }
      try { json += decoder.decode(chunk.value, { stream: true }); }
      catch { throw new AnalysisFailure("INVALID_RESPONSE", true); }
    }
    try { return JSON.parse(json + decoder.decode()) as unknown; }
    catch { throw new AnalysisFailure("INVALID_RESPONSE", true); }
  } finally { reader.releaseLock(); }
}

export function createReadingBackgroundHandler(extensionId: string, backendOrigin: string, fetcher: typeof fetch = fetch) {
  const origin = validateBackendOrigin(backendOrigin);
  const requests = new Map<string, AbortController>();
  return (message: unknown, sender: MessageSender, reply: (response: unknown) => void): boolean | void => {
    if (!message || typeof message !== "object") return;
    const input = message as Record<string, unknown>;
    const speech = input.type === speechPrefix || input.type === `${speechPrefix}:cancel`;
    const prefix = speech ? speechPrefix : scanPrefix;
    if (input.type !== prefix && input.type !== `${prefix}:cancel`) return;
    const id = input.interaction_id;
    const tab = sender.tab?.id;
    const frame = sender.frameId;
    const rejectInvalid = () => reply({ ok: false, code: "VALIDATION_FAILED", retryable: false });
    if (sender.id !== extensionId || typeof tab !== "number" || tab < 0 || !Number.isInteger(tab) ||
        typeof frame !== "number" || frame < 0 || !Number.isInteger(frame) || typeof id !== "string" || !readingUuid.test(id)) {
      rejectInvalid(); return;
    }
    const key = `${prefix}:${tab}:${frame}:${id}`;
    if (input.type !== prefix) { requests.get(key)?.abort(); reply({ ok: true }); return; }
    const scan = !speech && isScanInput(input) ? { text: input.text, offset: input.offset } : null;
    const source = speech && isSpeechSource(input.source) ? input.source : null;
    if ((scan === null && source === null) || requests.has(key)) { rejectInvalid(); return; }
    const controller = new AbortController();
    requests.set(key, controller);
    let replied = false;
    const finish = (value: unknown) => {
      if (replied) return;
      replied = true;
      clearTimeout(timer);
      requests.delete(key);
      reply(value);
    };
    controller.signal.addEventListener("abort", () => finish({ ok: false, code: "ABORTED", retryable: false }), { once: true });
    const timer = setTimeout(() => {
      finish({ ok: false, code: "TIMEOUT", retryable: true });
      controller.abort();
    }, speech ? 30000 : 5000);
    void (async () => {
      try {
        const response = await fetcher(`${origin}/api/${speech ? "speech" : "scan"}`, {
          method: "POST", headers: { "Content-Type": "application/json", "X-Contract-Version": "1" },
          credentials: "omit", redirect: "error", cache: "no-store",
          body: JSON.stringify({ interaction_id: id, ...(source ? { source } : scan) }), signal: controller.signal,
        });
        if (replied) return;
        if (!response.ok) {
          const error = await readJson(response, 4096).catch(() => null) as Record<string, unknown> | null;
          finish({ ok: false, code: typeof error?.error_code === "string" ? safeFailureCode(error.error_code) : "HTTP_ERROR",
            retryable: typeof error?.retryable === "boolean" ? error.retryable : response.status >= 500 || response.status === 429 });
          return;
        }
        const value = await readJson(response, speech ? speechWireLimit : 4 * 1024 * 1024);
        if (replied) return;
        if (scan !== null) parseScanResponse(value, id, scan);
        else parseSpeechResponse(value, id);
        finish({ ok: true, value });
      } catch (error) {
        finish({ ok: false, code: error instanceof AnalysisFailure ? error.code : "NETWORK_ERROR", retryable: true });
      }
    })();
    return true;
  };
}

export function registerReadingBackground(runtime: RuntimeBackground, origin: string): void {
  if (runtime.id) runtime.onMessage.addListener(createReadingBackgroundHandler(runtime.id, origin));
}
