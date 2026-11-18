import { afterEach, describe, expect, it, vi } from "vitest";
import { createRuntimeTranslationTransport, createTranslationBackgroundHandler, TRANSLATION_MESSAGE_PREFIX } from "../../src/platform/translationTransport.ts";
import { type MessageSender, type RuntimeMessenger } from "../../src/platform/runtimeTransport.ts";
import { interactionId } from "./analysisFixtures.ts";

const sender: MessageSender = { id: "extension", tab: { id: 1 }, frameId: 0 };
const message = { type: `${TRANSLATION_MESSAGE_PREFIX}:translate`, text: "猫です。", interaction_id: interactionId };
const result = { data: { source_language: "ja", target_language: "en", translated_text: "It is a cat." }, meta: { request_id: "req", interaction_id: interactionId } };
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(r => { resolve = r; }); return { promise, resolve }; }
afterEach(() => vi.useRealTimers());

describe("translation boundaries", () => {
  it("sends only selected text, language and identity to the backend", async () => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify(result))), reply = vi.fn();
    const handler = createTranslationBackgroundHandler("extension", "https://api.example.test", fetcher);
    expect(handler(message, sender, reply)).toBe(true);
    await vi.waitFor(() => expect(reply).toHaveBeenCalledWith({ ok: true, value: result }));
    const [url, init] = fetcher.mock.calls[0]!;
    expect(url).toBe("https://api.example.test/api/translations");
    expect(init).toMatchObject({ method: "POST", credentials: "omit", redirect: "error" });
    expect(JSON.parse(init!.body as string)).toEqual({ text: "猫です。", target_language: "en", interaction_id: interactionId });
  });
  it.each([{}, { ...sender, id: "untrusted" }, { ...sender, frameId: -1 }])("rejects an untrusted sender", source => {
    const fetcher = vi.fn<typeof fetch>(), reply = vi.fn();
    createTranslationBackgroundHandler("extension", "http://localhost:5000", fetcher)(message, source as MessageSender, reply);
    expect(reply).toHaveBeenCalledWith({ ok: false, code: "VALIDATION_FAILED", retryable: false }); expect(fetcher).not.toHaveBeenCalled();
  });
  it.each([
    { ...result, data: { ...result.data, target_language: "vi" } },
    { ...result, meta: { ...result.meta, interaction_id: "other" } },
    { ...result, data: { ...result.data, translated_text: " " } },
    {},
  ])("rejects uncorrelated, wrong-language and malformed results", async value => {
    const runtime: RuntimeMessenger = { sendMessage: (_message, callback) => callback({ ok: true, value }) };
    await expect(createRuntimeTranslationTransport(runtime)("猫", interactionId, new AbortController().signal)).rejects.toMatchObject({ code: "INVALID_RESPONSE" });
  });
  it("cancels only the originating tab/frame, ignores late work and never logs provider prose", async () => {
    const response = deferred<Response>(), fetcher = vi.fn<typeof fetch>().mockReturnValue(response.promise), reply = vi.fn();
    const handler = createTranslationBackgroundHandler("extension", "http://localhost:5000", fetcher);
    handler(message, sender, reply);
    const cancel = { type: `${TRANSLATION_MESSAGE_PREFIX}:cancel`, interaction_id: interactionId };
    handler(cancel, { ...sender, tab: { id: 2 } }, vi.fn());
    expect(fetcher.mock.calls[0]?.[1]?.signal?.aborted).toBe(false);
    handler(cancel, sender, vi.fn());
    expect(fetcher.mock.calls[0]?.[1]?.signal?.aborted).toBe(true);
    response.resolve(new Response(JSON.stringify(result))); await Promise.resolve(); await Promise.resolve();
    expect(reply).toHaveBeenCalledExactlyOnceWith({ ok: false, code: "ABORTED", retryable: false });
  });
  it("times out even if the provider transport ignores cancellation", async () => {
    vi.useFakeTimers(); const response = deferred<Response>(), fetcher = vi.fn<typeof fetch>().mockReturnValue(response.promise), reply = vi.fn();
    const handler = createTranslationBackgroundHandler("extension", "http://localhost:5000", fetcher);
    handler(message, sender, reply); await vi.advanceTimersByTimeAsync(15000);
    expect(reply).toHaveBeenCalledExactlyOnceWith({ ok: false, code: "TIMEOUT", retryable: true });
    response.resolve(new Response(JSON.stringify(result))); await vi.advanceTimersByTimeAsync(0); expect(reply).toHaveBeenCalledOnce();
  });
  it("cleans backend errors and never displays raw provider text", async () => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify({ error_code: "SERVICE_UNAVAILABLE", message: "PRIVATE", request_id: "req", retryable: true }), { status: 503 })), reply = vi.fn();
    createTranslationBackgroundHandler("extension", "http://localhost:5000", fetcher)(message, sender, reply);
    await vi.waitFor(() => expect(reply).toHaveBeenCalledWith({ ok: false, code: "SERVICE_UNAVAILABLE", retryable: true }));
    expect(JSON.stringify(reply.mock.calls)).not.toContain("PRIVATE");
  });
  it("runtime cancellation rejects and sends a translation-scoped cancel", async () => {
    const sendMessage = vi.fn(); const controller = new AbortController();
    const resultPromise = createRuntimeTranslationTransport({ sendMessage })("猫", interactionId, controller.signal);
    const observed = expect(resultPromise).rejects.toMatchObject({ code: "ABORTED" });
    controller.abort(); await observed;
    expect(sendMessage.mock.calls[1]?.[0]).toEqual({ type: `${TRANSLATION_MESSAGE_PREFIX}:cancel`, interaction_id: interactionId });
  });
});
