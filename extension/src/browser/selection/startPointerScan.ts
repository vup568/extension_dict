import { AnalysisFailure } from "../../api/analysisContract.ts";
import type { ScanResponse } from "../../api/readingContracts.ts";
import { createInteractionId } from "../../interaction/createInteractionId.ts";
import type { Scan } from "../../platform/readingTransport.ts";
import { extractPointerWindow, type PointerWindow } from "./extractPointerWindow.ts";

export interface PointerScanResult {
  source: PointerWindow;
  interactionId: string;
  result: ScanResponse | AnalysisFailure;
}

export function startPointerScan(doc: Document, scan: Scan, blocked: () => boolean,
  insidePopup: (event: Event) => boolean, publish: (result: PointerScanResult) => void,
  onPending: (source: PointerWindow | null, interactionId?: string) => void) {
  const win = doc.defaultView!;
  let enabled = false;
  let shift = false;
  let point: { x: number; y: number } | null = null;
  let timer: number | undefined;
  let pending: AbortController | null = null;
  let lastDispatch = -Infinity;
  let current: PointerScanResult | null = null;

  function cancel() {
    win.clearTimeout(timer);
    timer = undefined;
    const request = pending;
    pending = null;
    request?.abort();
    if (request) onPending(null);
  }

  function sameSource(a: PointerWindow, b: PointerWindow) {
    return a.input.text === b.input.text && a.input.offset === b.input.offset;
  }

  function dispatch(source: PointerWindow, isRetry: boolean) {
    if (!isRetry && current !== null && !(current.result instanceof AnalysisFailure) && sameSource(source, current.source)) {
      // Reposition a duplicate result at this glyph without a new interaction or request.
      current = { ...current, source };
      publish(current);
      return;
    }
    cancel();
    lastDispatch = win.performance.now();
    const request = new AbortController();
    pending = request;
    const interactionId = createInteractionId(bytes => win.crypto.getRandomValues(bytes));
    function complete(result: PointerScanResult["result"]) {
      if (pending !== request) return;
      win.clearTimeout(timer);
      timer = undefined;
      pending = null;
      if (!enabled || (!isRetry && blocked()) || !source.anchor.startContainer.isConnected) { onPending(null); return; }
      if (!isRetry) {
        if (!shift || point === null) { onPending(null); return; }
        const latest = extractPointerWindow(doc, point.x, point.y);
        if (latest === null || !sameSource(source, latest)) { onPending(null); return; }
        source = latest;
      }
      current = { source, interactionId, result };
      publish(current);
    }
    onPending(source, interactionId);
    timer = win.setTimeout(() => {
      request.abort();
      complete(new AnalysisFailure("TIMEOUT", true));
    }, 5000);
    void Promise.resolve().then(() => scan(source.input, interactionId, request.signal)).then(complete, error => {
      complete(error instanceof AnalysisFailure ? error : new AnalysisFailure("NETWORK_ERROR", true));
    });
  }

  function schedule() {
    cancel();
    if (!enabled || !shift || point === null || blocked()) return;
    timer = win.setTimeout(() => {
      if (!enabled || !shift || point === null || blocked()) return;
      const source = extractPointerWindow(doc, point.x, point.y);
      if (source !== null) dispatch(source, false);
    }, Math.max(300, 300 + lastDispatch - win.performance.now()));
  }

  function pointerMove(event: PointerEvent) {
    if (!event.isTrusted || !enabled || event.pointerType !== "mouse") return;
    if (insidePopup(event) || event.buttons !== 0) { point = null; cancel(); return; }
    const unchanged = point?.x === event.clientX && point.y === event.clientY && shift === event.shiftKey;
    point = { x: event.clientX, y: event.clientY };
    shift = event.shiftKey;
    if (!unchanged) schedule();
  }
  function keyDown(event: KeyboardEvent) {
    if (!event.isTrusted || !enabled || event.key !== "Shift" || shift || insidePopup(event)) return;
    shift = true;
    schedule();
  }
  function keyUp(event: KeyboardEvent) {
    if (!event.isTrusted || event.key !== "Shift") return;
    shift = false;
    cancel();
  }
  function resetPointer() { point = null; shift = false; cancel(); }
  function leave(event: PointerEvent) { if (event.relatedTarget === null) resetPointer(); }

  doc.addEventListener("pointermove", pointerMove, true);
  doc.addEventListener("pointerout", leave, true);
  doc.addEventListener("keydown", keyDown, true);
  doc.addEventListener("keyup", keyUp, true);
  doc.addEventListener("scroll", cancel, true);
  win.addEventListener("blur", resetPointer);
  return {
    cancel,
    clear() { resetPointer(); current = null; },
    setEnabled(value: boolean) { enabled = value; if (!value) resetPointer(); },
    retry() {
      if (!enabled || current === null || !(current.result instanceof AnalysisFailure) || !current.result.retryable || pending !== null) return;
      const source = current.source;
      cancel();
      // An explicit retry is allowed while inside the popup, with the same dispatch floor.
      timer = win.setTimeout(() => {
        if (source.anchor.startContainer.isConnected) dispatch(source, true);
      }, Math.max(0, 300 + lastDispatch - win.performance.now()));
    },
    dispose() {
      resetPointer();
      current = null;
      doc.removeEventListener("pointermove", pointerMove, true);
      doc.removeEventListener("pointerout", leave, true);
      doc.removeEventListener("keydown", keyDown, true);
      doc.removeEventListener("keyup", keyUp, true);
      doc.removeEventListener("scroll", cancel, true);
      win.removeEventListener("blur", resetPointer);
    },
  };
}
