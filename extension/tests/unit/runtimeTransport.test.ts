import { afterEach, describe, expect, it, vi } from "vitest";
import { AnalysisFailure } from "../../src/api/analysisContract.ts";
import { createRuntimeAnalysisTransport, MESSAGE_PREFIX, type RuntimeMessenger } from "../../src/platform/runtimeTransport.ts";
import { analysisFixture, interactionId } from "./analysisFixtures.ts";

afterEach(() => vi.useRealTimers());
describe("F-04 runtime transport", () => {
  it("sends only ephemeral selected text and interaction identity", async () => {
    const sendMessage = vi.fn((_message, callback) => callback({ ok: true, value: analysisFixture() }));
    const result = await createRuntimeAnalysisTransport({ sendMessage })("食事", interactionId, new AbortController().signal);
    expect(result.meta.interaction_id).toBe(interactionId);
    expect(sendMessage.mock.calls[0]?.[0]).toEqual({ type: `${MESSAGE_PREFIX}:analyze`, text: "食事", interaction_id: interactionId });
  });
  it("rejects absent runtime and already aborted signal", async () => {
    await expect(createRuntimeAnalysisTransport()("食事", interactionId, new AbortController().signal)).rejects.toMatchObject({ code: "TRANSPORT_UNAVAILABLE" });
    const sendMessage = vi.fn(); const controller = new AbortController(); controller.abort();
    await expect(createRuntimeAnalysisTransport({ sendMessage })("食事", interactionId, controller.signal)).rejects.toMatchObject({ code: "ABORTED" });
    expect(sendMessage).not.toHaveBeenCalled();
  });
  it("attempts cancellation and ignores late callback even if transport cannot cancel", async () => {
    let callback: ((value: unknown) => void) | undefined;
    const sendMessage = vi.fn((message, reply) => { if (message.type.endsWith(":analyze")) callback = reply; });
    const controller = new AbortController();
    const result = createRuntimeAnalysisTransport({ sendMessage })("食事", interactionId, controller.signal);
    const observed = expect(result).rejects.toMatchObject({ code: "ABORTED" });
    controller.abort(); callback?.({ ok: true, value: analysisFixture() }); await observed;
    expect(sendMessage.mock.calls[1]?.[0]).toEqual({ type: `${MESSAGE_PREFIX}:cancel`, interaction_id: interactionId });
  });
  it("enforces 5s timeout when no message callback arrives", async () => {
    vi.useFakeTimers(); const sendMessage = vi.fn();
    const result = createRuntimeAnalysisTransport({ sendMessage })("食事", interactionId, new AbortController().signal);
    const observed = expect(result).rejects.toMatchObject({ code: "TIMEOUT", retryable: true });
    await vi.advanceTimersByTimeAsync(5000); await observed;
    expect(sendMessage.mock.calls[1]?.[0].type).toBe(`${MESSAGE_PREFIX}:cancel`);
  });
  it.each([null, {}, { ok: true, value: {} }, { ok: true, value: analysisFixture("old") }])("rejects malformed/uncorrelated message %j", async response => {
    const runtime: RuntimeMessenger = { sendMessage: (_, callback) => callback(response) };
    await expect(createRuntimeAnalysisTransport(runtime)("食事", interactionId, new AbortController().signal)).rejects.toBeInstanceOf(AnalysisFailure);
  });
  it("maps lastError and synchronous failures without including browser prose", async () => {
    const runtime: RuntimeMessenger = { lastError: { message: "PRIVATE_TEXT" }, sendMessage: (_, callback) => callback(null) };
    await expect(createRuntimeAnalysisTransport(runtime)("食事", interactionId, new AbortController().signal)).rejects.toMatchObject({ code: "TRANSPORT_UNAVAILABLE" });
    runtime.sendMessage = () => { throw new Error("PRIVATE_TEXT"); };
    await expect(createRuntimeAnalysisTransport(runtime)("食事", interactionId, new AbortController().signal)).rejects.not.toThrow("PRIVATE_TEXT");
  });
  it("passes typed retryable backend failure", async () => {
    const runtime: RuntimeMessenger = { sendMessage: (_, callback) => callback({ ok: false, code: "SERVICE_UNAVAILABLE", retryable: true }) };
    await expect(createRuntimeAnalysisTransport(runtime)("食事", interactionId, new AbortController().signal)).rejects.toMatchObject({ code: "SERVICE_UNAVAILABLE", retryable: true });
  });
});
