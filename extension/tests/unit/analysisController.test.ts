import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { AnalysisController, type AnalysisState, type Analyze } from "../../src/interaction/analysisController.ts";
import { AnalysisFailure, type AnalysisResponse } from "../../src/api/analysisContract.ts";

function response(id: string, text = "日本語"): AnalysisResponse {
  return {
    data: { normalized_text: text, tokens: [], dictionary_matches: [], kanji: [], conjugations: [], grammar_occurrences: [],
      capabilities: { vocabulary: "completed", kanji: "completed", grammar: "completed", morphology: "unavailable", conjugation: "unavailable" } },
    meta: { request_id: "request", interaction_id: id, contract_version: "1" },
  };
}
function pending() {
  let resolve!: (value: AnalysisResponse) => void;
  let reject!: (reason: unknown) => void;
  const promise = new Promise<AnalysisResponse>((yes, no) => { resolve = yes; reject = no; });
  return { promise, resolve, reject };
}
function setup(analyze: Analyze = async (_text, id) => response(id)) {
  const states: AnalysisState[] = [];
  const calls = vi.fn(analyze);
  let serial = 0;
  const controller = new AnalysisController({ analyze: calls, createId: () => `id-${++serial}`, onState: (state) => states.push(state) });
  return { controller, states, calls };
}

describe("F-03/F-04 current interaction", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => { vi.clearAllTimers(); vi.useRealTimers(); });

  it("renders loading synchronously before transport and debounces exactly 300ms", async () => {
    const { controller, states, calls } = setup();
    controller.select("日本語");
    expect(states).toEqual([{ phase: "loading", text: "日本語", interactionId: "id-1" }]);
    expect(calls).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(299);
    expect(calls).not.toHaveBeenCalled();
    await vi.advanceTimersByTimeAsync(1);
    expect(calls).toHaveBeenCalledTimes(1);
    expect(controller.state.phase).toBe("result");
  });

  it("rapid changes send only the final exact selection", async () => {
    const { controller, calls } = setup();
    controller.select("日本"); await vi.advanceTimersByTimeAsync(200);
    controller.select("食事"); await vi.advanceTimersByTimeAsync(300);
    expect(calls).toHaveBeenCalledTimes(1);
    expect(calls.mock.calls[0]?.slice(0, 2)).toEqual(["食事", "id-2"]);
  });

  it.each([null, "", "  \n"]) ("closes without a request for %s", async (text) => {
    const { controller, calls } = setup(); controller.select(text);
    await vi.advanceTimersByTimeAsync(6000);
    expect(controller.state).toEqual({ phase: "hidden" }); expect(calls).not.toHaveBeenCalled();
  });

  it("does not truncate oversized input or send it", async () => {
    const { controller, calls } = setup(); controller.select("日".repeat(1001));
    expect(controller.state).toMatchObject({ phase: "error", code: "INPUT_TOO_LONG", retryable: false });
    controller.retry(); await vi.advanceTimersByTimeAsync(6000); expect(calls).not.toHaveBeenCalled();
  });

  it("accepts the actual trimmed 1000 UTF-16 API limit without changing the payload", async () => {
    const { controller, calls } = setup(); const text = ` ${"日".repeat(1000)} `;
    controller.select(text); await vi.advanceTimersByTimeAsync(300);
    expect(calls.mock.calls[0]?.[0]).toBe(text);
  });

  it.each(["success", "error", "partial"])("rejects old %s during the new selection's debounce even when abort is ignored", async (outcome) => {
    const old = pending(); const { controller, calls } = setup((_text, id) => id === "id-1" ? old.promise : Promise.resolve(response(id)));
    controller.select("日本"); await vi.advanceTimersByTimeAsync(300);
    const signal = calls.mock.calls[0]?.[2]; controller.select("食事");
    expect(signal?.aborted).toBe(true);
    if (outcome === "error") old.reject(new AnalysisFailure("SERVICE_UNAVAILABLE", true));
    else {
      const result = response("id-1", "日本"); if (outcome === "partial") result.data.capabilities.grammar = "failed";
      old.resolve(result);
    }
    await vi.advanceTimersByTimeAsync(1);
    expect(controller.state).toEqual({ phase: "loading", text: "食事", interactionId: "id-2" });
    await vi.advanceTimersByTimeAsync(299); expect(controller.state).toMatchObject({ phase: "result", interactionId: "id-2" });
  });

  it("does not let A arriving after B success overwrite B", async () => {
    const old = pending(); const { controller } = setup((_text, id) => id === "id-1" ? old.promise : Promise.resolve(response(id, "食事")));
    controller.select("日本"); await vi.advanceTimersByTimeAsync(300);
    controller.select("食事"); await vi.advanceTimersByTimeAsync(300);
    old.resolve(response("id-1")); await vi.advanceTimersByTimeAsync(0);
    expect(controller.state).toMatchObject({ phase: "result", text: "食事", interactionId: "id-2" });
  });

  it("same text still creates a new identity and abort attempt", async () => {
    const request = pending(); const { controller, calls } = setup(() => request.promise);
    controller.select("日本"); await vi.advanceTimersByTimeAsync(300);
    controller.select("日本"); expect(controller.state).toMatchObject({ interactionId: "id-2" });
    expect(calls.mock.calls[0]?.[2].aborted).toBe(true);
  });

  it("times out at 5s even when a hung transport ignores abort, and ignores late success", async () => {
    const request = pending(); const { controller, calls } = setup(() => request.promise);
    controller.select("日本"); await vi.advanceTimersByTimeAsync(5299);
    expect(controller.state.phase).toBe("loading"); await vi.advanceTimersByTimeAsync(1);
    expect(controller.state).toMatchObject({ phase: "error", code: "TIMEOUT", retryable: true });
    expect(calls.mock.calls[0]?.[2].aborted).toBe(true);
    request.resolve(response("id-1")); await vi.advanceTimersByTimeAsync(0);
    expect(controller.state).toMatchObject({ phase: "error", code: "TIMEOUT" });
  });

  it("Retry dispatches with a fresh ID and ignores the timed out result", async () => {
    const old = pending(); const { controller, calls } = setup((_text, id) => id === "id-1" ? old.promise : Promise.resolve(response(id)));
    controller.select("日本"); await vi.advanceTimersByTimeAsync(5300); controller.retry();
    expect(controller.state).toMatchObject({ phase: "loading", interactionId: "id-2" });
    await vi.advanceTimersByTimeAsync(0); expect(calls).toHaveBeenCalledTimes(2);
    old.resolve(response("id-1")); await vi.advanceTimersByTimeAsync(0);
    expect(controller.state).toMatchObject({ phase: "result", interactionId: "id-2" });
  });

  it("Close clears debounce and raw selected content", async () => {
    const { controller, calls } = setup(); controller.select("PRIVATE日本"); controller.close();
    await vi.advanceTimersByTimeAsync(6000); expect(calls).not.toHaveBeenCalled(); expect(controller.state).toEqual({ phase: "hidden" });
  });

  it("Close aborts in-flight work and ignores late errors", async () => {
    const request = pending(); const { controller, calls } = setup(() => request.promise);
    controller.select("日本"); await vi.advanceTimersByTimeAsync(300); controller.close();
    request.reject(new Error("PRIVATE日本")); await vi.advanceTimersByTimeAsync(0);
    expect(controller.state).toEqual({ phase: "hidden" }); expect(calls.mock.calls[0]?.[2].aborted).toBe(true);
  });

  it("rejects response correlation mismatches", async () => {
    const { controller } = setup(async () => response("wrong")); controller.select("日本"); await vi.advanceTimersByTimeAsync(300);
    expect(controller.state).toMatchObject({ phase: "error", code: "INVALID_RESPONSE" });
  });

  it("maps unexpected and synchronous errors to content-free failures", async () => {
    const { controller } = setup(() => { throw new Error("PRIVATE日本"); }); controller.select("日本"); await vi.advanceTimersByTimeAsync(300);
    expect(controller.state).toEqual({ phase: "error", text: "日本", interactionId: "id-1", code: "NETWORK_ERROR", retryable: true });
  });

  it("preserves nonretryable structured failures", async () => {
    const { controller, calls } = setup(async () => { throw new AnalysisFailure("VALIDATION_FAILED", false); });
    controller.select("日本"); await vi.advanceTimersByTimeAsync(300); controller.retry();
    expect(controller.state).toMatchObject({ phase: "error", code: "VALIDATION_FAILED", retryable: false }); expect(calls).toHaveBeenCalledTimes(1);
  });

  it("Retry while hidden/loading/result has no effect", async () => {
    const { controller, calls } = setup(); controller.retry(); controller.select("日本"); controller.retry();
    await vi.advanceTimersByTimeAsync(300); controller.retry(); expect(calls).toHaveBeenCalledTimes(1);
  });
});
