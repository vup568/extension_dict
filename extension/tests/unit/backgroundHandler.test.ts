import { afterEach, describe, expect, it, vi } from "vitest";
import { createBackgroundHandler, registerBackground } from "../../src/platform/backgroundHandler.ts";
import { validateBackendOrigin } from "../../src/platform/backendOrigin.ts";
import { MESSAGE_PREFIX, type MessageSender, type RuntimeBackground } from "../../src/platform/runtimeTransport.ts";
import { analysisFixture, interactionId } from "./analysisFixtures.ts";
const sender: MessageSender = { id: "extension", tab: { id: 1 }, frameId: 0 };
const analyze = { type: `${MESSAGE_PREFIX}:analyze`, text: "食事", interaction_id: interactionId };
const cancel = { type: `${MESSAGE_PREFIX}:cancel`, interaction_id: interactionId };
afterEach(() => vi.useRealTimers());
function deferred<T>() { let resolve!: (value: T) => void; const promise = new Promise<T>(r => { resolve = r; }); return { promise, resolve }; }
describe("backend origin boundary", () => {
  it.each(["http://127.0.0.1:5000", "http://localhost:5000", "http://[::1]:5000", "https://api.example.test/"])("accepts %s", origin => {
    expect(validateBackendOrigin(origin)).toBe(new URL(origin).origin);
  });
  it.each(["invalid", "http://public.example.test", "https://user:pass@api.example.test", "https://api.example.test/api", "https://api.example.test/?key=x", "https://api.example.test/#x", "ftp://localhost"])("rejects %s", origin => {
    expect(() => validateBackendOrigin(origin)).toThrow();
  });
});
describe("MV3 backend handler", () => {
  it("uses fixed origin, JSON minimal request, no credentials or redirects", async () => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify(analysisFixture())));
    const handler = createBackgroundHandler("extension", "https://api.example.test", fetcher);
    const reply = vi.fn(); expect(handler(analyze, sender, reply)).toBe(true);
    await vi.waitFor(() => expect(reply).toHaveBeenCalledOnce());
    expect(reply.mock.calls[0]?.[0]).toEqual({ ok: true, value: analysisFixture() });
    const [url, options] = fetcher.mock.calls[0]!;
    expect(url).toBe("https://api.example.test/api/analysis");
    expect(options).toMatchObject({ method: "POST", redirect: "error", credentials: "omit" });
    expect(JSON.parse(options!.body as string)).toEqual({ text: "食事", interaction_id: interactionId });
  });
  it.each([
    {}, { ...sender, id: "foreign" }, { ...sender, tab: undefined }, { ...sender, frameId: undefined }, { ...sender, tab: { id: -1 } }, { ...sender, frameId: -1 },
  ])("rejects untrusted sender %j", source => {
    const fetcher = vi.fn<typeof fetch>(); const reply = vi.fn();
    createBackgroundHandler("extension", "https://api.example.test", fetcher)(analyze, source as MessageSender, reply);
    expect(reply).toHaveBeenCalledWith({ ok: false, code: "VALIDATION_FAILED", retryable: false }); expect(fetcher).not.toHaveBeenCalled();
  });
  it.each(["", "x".repeat(1001), null, 1])("rejects malformed/oversized input", text => {
    const fetcher = vi.fn<typeof fetch>(), reply = vi.fn();
    createBackgroundHandler("extension", "https://api.example.test", fetcher)({ ...analyze, text }, sender, reply);
    expect(reply).toHaveBeenCalledWith({ ok: false, code: "VALIDATION_FAILED", retryable: false }); expect(fetcher).not.toHaveBeenCalled();
  });
  it("ignores unrelated messages and rejects nonUUID identities", () => {
    const fetcher = vi.fn<typeof fetch>(), reply = vi.fn(), handler = createBackgroundHandler("extension", "https://api.example.test", fetcher);
    handler(null, sender, reply); handler({}, sender, reply); expect(reply).not.toHaveBeenCalled();
    handler({ ...analyze, interaction_id: "old" }, sender, reply); expect(reply).toHaveBeenCalledOnce();
  });
  it("isolates cancellation by originating tab/frame even with same UUID", async () => {
    const response = deferred<Response>(), fetcher = vi.fn<typeof fetch>().mockReturnValue(response.promise);
    const handler = createBackgroundHandler("extension", "https://api.example.test", fetcher), reply = vi.fn();
    handler(analyze, sender, reply); handler(cancel, { ...sender, tab: { id: 2 } }, vi.fn());
    expect(fetcher.mock.calls[0]?.[1]?.signal?.aborted).toBe(false);
    handler(cancel, sender, vi.fn()); expect(fetcher.mock.calls[0]?.[1]?.signal?.aborted).toBe(true);
    expect(reply).toHaveBeenCalledExactlyOnceWith({ ok: false, code: "ABORTED", retryable: false });
    response.resolve(new Response(JSON.stringify(analysisFixture()))); await Promise.resolve(); await Promise.resolve();
    expect(reply).toHaveBeenCalledOnce();
  });
  it("rejects duplicate live identity and cleans up after cancellation", () => {
    const fetcher = vi.fn<typeof fetch>().mockReturnValue(new Promise(() => {}));
    const handler = createBackgroundHandler("extension", "https://api.example.test", fetcher), duplicate = vi.fn();
    handler(analyze, sender, vi.fn()); handler(analyze, sender, duplicate);
    expect(duplicate).toHaveBeenCalledWith({ ok: false, code: "VALIDATION_FAILED", retryable: false });
    handler(cancel, sender, vi.fn()); handler(analyze, sender, vi.fn()); expect(fetcher).toHaveBeenCalledTimes(2);
    handler(cancel, sender, vi.fn());
  });
  it("enforces timeout even when fetch ignores AbortSignal and ignores late outcome", async () => {
    vi.useFakeTimers(); const response = deferred<Response>(), fetcher = vi.fn<typeof fetch>().mockReturnValue(response.promise);
    const handler = createBackgroundHandler("extension", "https://api.example.test", fetcher), reply = vi.fn();
    handler(analyze, sender, reply); await vi.advanceTimersByTimeAsync(5000);
    expect(reply).toHaveBeenCalledExactlyOnceWith({ ok: false, code: "TIMEOUT", retryable: true });
    expect(fetcher.mock.calls[0]?.[1]?.signal?.aborted).toBe(true);
    response.resolve(new Response(JSON.stringify(analysisFixture()))); await vi.advanceTimersByTimeAsync(0); expect(reply).toHaveBeenCalledOnce();
  });
  it.each([
    [400, { error_code: "VALIDATION_FAILED", message: "PRIVATE_TEXT", request_id: "r", retryable: false }, "VALIDATION_FAILED", false],
    [503, { error_code: "SERVICE_UNAVAILABLE", message: "PRIVATE_TEXT", request_id: "r", retryable: true }, "SERVICE_UNAVAILABLE", true],
    [500, {}, "HTTP_ERROR", true],
    [503, { error_code: "PRIVATE_TEXT", message: "PRIVATE_TEXT", request_id: "r" }, "HTTP_ERROR", true],
  ])("uses structured error code/status, excluding backend prose", async (status, payload, code, retryable) => {
    const fetcher = vi.fn<typeof fetch>().mockResolvedValue(new Response(JSON.stringify(payload), { status })); const reply = vi.fn();
    createBackgroundHandler("extension", "https://api.example.test", fetcher)(analyze, sender, reply);
    await vi.waitFor(() => expect(reply).toHaveBeenCalledWith({ ok: false, code, retryable }));
    expect(JSON.stringify(reply.mock.calls)).not.toContain("PRIVATE_TEXT");
  });
  it("handles malformed success and fetch rejection without raw content", async () => {
    const malformed = vi.fn<typeof fetch>().mockResolvedValue(new Response("{}")), reply = vi.fn();
    createBackgroundHandler("extension", "https://api.example.test", malformed)(analyze, sender, reply);
    await vi.waitFor(() => expect(reply).toHaveBeenCalledWith({ ok: false, code: "INVALID_RESPONSE", retryable: true }));
    const rejected = vi.fn<typeof fetch>().mockRejectedValue(new Error("PRIVATE_TEXT")), failureReply = vi.fn();
    createBackgroundHandler("extension", "https://api.example.test", rejected)(analyze, sender, failureReply);
    await vi.waitFor(() => expect(failureReply).toHaveBeenCalledWith({ ok: false, code: "NETWORK_ERROR", retryable: true }));
  });
  it("registers only when extension ID is present", () => {
    const addListener = vi.fn(); const runtime = { id: "extension", onMessage: { addListener }, sendMessage: vi.fn() } as RuntimeBackground;
    registerBackground(runtime, "http://127.0.0.1:5000"); expect(addListener).toHaveBeenCalledOnce();
    registerBackground({ ...runtime, id: "" }, "http://127.0.0.1:5000"); expect(addListener).toHaveBeenCalledOnce();
  });
});
