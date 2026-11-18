import { classifyJapaneseText } from "../../boundary/classifyJapaneseText.ts";
import { extractRubySafeSelection } from "../selection/extractRubySafeSelection.ts";
import { AnalysisController, type Analyze } from "../../interaction/analysisController.ts";
import { createPopupView } from "./createPopupView.tsx";
import { createInteractionId } from "../../interaction/createInteractionId.ts";
import type { Translate } from "../../platform/translationTransport.ts";
import { AnalysisFailure } from "../../api/analysisContract.ts";
import { startPointerScan } from "../selection/startPointerScan.ts";
import type { Scan, Speak } from "../../platform/readingTransport.ts";
import type { ReadingPreferenceStore } from "../../platform/readingPreferences.ts";

export interface SelectionPopupOptions {
  readonly document: Document;
  readonly analyze: Analyze;
  readonly translate?: Translate;
  readonly scan?: Scan;
  readonly speak?: Speak;
  readonly preferences?: ReadingPreferenceStore;
}

/** All DOM, focus and selection lifecycle concerns stay in this frame-local adapter. */
export function startSelectionPopup(options: SelectionPopupOptions): () => void {
  const doc = options.document;
  const win = doc.defaultView;
  if (win === null) return () => {};
  let range: Range | null = null;
  let scanRange: Range | null = null;
  let scanId: string | null = null;
  let scanLoading = false;
  let insidePopup = false;
  let disposed = false;
  const controller = new AnalysisController({
    analyze: options.analyze,
    // getRandomValues is also available on ordinary HTTP host pages; randomUUID may not be.
    createId: () => createInteractionId((bytes) => win.crypto.getRandomValues(bytes)),
    onState: (state) => view.render(state),
  });
  const view = createPopupView(doc, () => {
    const anchor = scanRange ?? range;
    try { return anchor?.startContainer.isConnected ? anchor.getBoundingClientRect() : null; }
    catch { return null; }
  }, close, () => scanId === null ? controller.retry() : pointerScan?.retry(), options.translate, {
    speak: options.speak,
    onSize: size => options.preferences?.setSize(size),
    onPin: () => pointerScan?.cancel(),
    ...(options.scan ? { onScanEnabled: (enabled: boolean) => {
      pointerScan?.setEnabled(enabled);
      options.preferences?.setScanEnabled(enabled);
    } } : {}),
  });

  const pointerScan = options.scan && startPointerScan(doc, options.scan, () => view.isPinned(), event => view.containsEvent(event),
    ({ source, interactionId, result }) => {
      if (scanId === interactionId && !scanLoading) {
        scanRange = source.anchor;
        view.reposition();
        return;
      }
      if (controller.state.phase !== "hidden") controller.close();
      scanRange = source.anchor;
      scanId = interactionId;
      scanLoading = false;
      const shared = { text: source.input.text, interactionId };
      view.render(result instanceof AnalysisFailure ? { phase: "error", ...shared, code: result.code, retryable: result.retryable }
        : { phase: "result", ...shared, response: result.analysis },
      { offset: source.input.offset, targetSpan: result instanceof AnalysisFailure ? null : result.targetSpan });
    }, (source, interactionId) => {
      if (source && interactionId && scanId === null && controller.state.phase === "hidden") {
        scanRange = source.anchor;
        scanId = interactionId;
        scanLoading = true;
        view.render({ phase: "loading", text: source.input.text, interactionId }, { offset: source.input.offset, targetSpan: null });
      } else if (source === null && scanLoading) {
        scanRange = null;
        scanId = null;
        scanLoading = false;
        view.render({ phase: "hidden" });
      }
    });
  const unsubscribe = options.preferences?.subscribe(preferences => {
    pointerScan?.setEnabled(preferences.scanEnabled);
    view.setPreferences(preferences);
  });

  function close(): void {
    pointerScan?.clear();
    scanRange = null;
    scanId = null;
    scanLoading = false;
    controller.close();
  }

  function refresh(): void {
    if (disposed) return;
    const selection = win?.getSelection() ?? null;
    if (view.containsNode(selection?.anchorNode ?? null) || view.containsNode(selection?.focusNode ?? null)) return;
    if (insidePopup) {
      if (selection === null || selection.isCollapsed || selection.rangeCount !== 1) return;
      const candidate = selection.getRangeAt(0);
      if (range !== null && range.startContainer === candidate.startContainer && range.startOffset === candidate.startOffset
        && range.endContainer === candidate.endContainer && range.endOffset === candidate.endOffset) return;
      insidePopup = false;
    }
    // Never use Selection.toString or page expansion as a fallback [DOM-001, PRIV-001].
    const extracted = extractRubySafeSelection(() => selection);
    if (extracted.status !== "ok") {
      range = null;
      if (scanId === null) controller.close();
      return;
    }
    const classification = classifyJapaneseText(extracted.text);
    if (classification.status !== "ok" || !classification.containsJapaneseText || selection === null) {
      range = null;
      close();
      return;
    }
    const next = selection.getRangeAt(0);
    if (range !== null && range.startContainer === next.startContainer && range.startOffset === next.startOffset
      && range.endContainer === next.endContainer && range.endOffset === next.endOffset
      && (scanId !== null || (controller.state.phase !== "hidden" && controller.state.text === extracted.text))) {
      view.reposition();
      return;
    }
    pointerScan?.clear();
    scanRange = null;
    scanId = null;
    scanLoading = false;
    range = next.cloneRange();
    controller.select(extracted.text);
  }

  function pointerDown(event: PointerEvent): void {
    insidePopup = view.containsEvent(event);
    if (!insidePopup) {
      // Dragging starts a new intent before the browser has updated its Range [ASYNC-002].
      range = null;
      close();
    }
  }
  function keyDown(event: KeyboardEvent): void {
    if (event.key === "Escape") {
      // Respect the host's own Escape handling: do not stop/default-prevent its event.
      close();
      range = null;
      insidePopup = false;
    } else if (!view.containsEvent(event)) {
      insidePopup = false;
    }
  }
  function pageHide(event: PageTransitionEvent): void {
    if (event.persisted) {
      // BFCache restores this same content-script instance; retain listeners, release content.
      close();
      range = null;
      insidePopup = false;
    } else {
      dispose();
    }
  }
  function dispose(): void {
    if (disposed) return;
    disposed = true;
    doc.removeEventListener("selectionchange", refresh);
    doc.removeEventListener("pointerdown", pointerDown, true);
    doc.removeEventListener("pointerup", refresh);
    doc.removeEventListener("keydown", keyDown, true);
    doc.removeEventListener("scroll", view.reposition, true);
    win?.removeEventListener("resize", view.reposition);
    win?.visualViewport?.removeEventListener("resize", view.reposition);
    win?.visualViewport?.removeEventListener("scroll", view.reposition);
    win?.removeEventListener("pagehide", pageHide);
    unsubscribe?.();
    close();
    pointerScan?.dispose();
    view.dispose();
    range = null;
  }

  doc.addEventListener("selectionchange", refresh);
  doc.addEventListener("pointerdown", pointerDown, true);
  doc.addEventListener("pointerup", refresh);
  doc.addEventListener("keydown", keyDown, true);
  doc.addEventListener("scroll", view.reposition, true);
  win.addEventListener("resize", view.reposition);
  win.visualViewport?.addEventListener("resize", view.reposition);
  win.visualViewport?.addEventListener("scroll", view.reposition);
  win.addEventListener("pagehide", pageHide);
  return dispose;
}
