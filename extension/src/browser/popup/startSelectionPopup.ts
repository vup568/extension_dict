import { classifyJapaneseText } from "../../boundary/classifyJapaneseText.ts";
import { extractRubySafeSelection } from "../selection/extractRubySafeSelection.ts";
import { AnalysisController, type Analyze } from "../../interaction/analysisController.ts";
import { createPopupView } from "./createPopupView.tsx";
import { createInteractionId } from "../../interaction/createInteractionId.ts";

export interface SelectionPopupOptions {
  readonly document: Document;
  readonly analyze: Analyze;
}

/** All DOM, focus and selection lifecycle concerns stay in this frame-local adapter. */
export function startSelectionPopup(options: SelectionPopupOptions): () => void {
  const doc = options.document;
  const win = doc.defaultView;
  if (win === null) return () => {};
  let range: Range | null = null;
  let insidePopup = false;
  let disposed = false;
  const controller = new AnalysisController({
    analyze: options.analyze,
    // getRandomValues is also available on ordinary HTTP host pages; randomUUID may not be.
    createId: () => createInteractionId((bytes) => win.crypto.getRandomValues(bytes)),
    onState: (state) => view.render(state),
  });
  const view = createPopupView(doc, () => {
    try { return range?.startContainer.isConnected ? range.getBoundingClientRect() : null; }
    catch { return null; }
  }, () => controller.close(), () => controller.retry());

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
    if (extracted.status !== "ok") { range = null; controller.close(); return; }
    const classification = classifyJapaneseText(extracted.text);
    if (classification.status !== "ok" || !classification.containsJapaneseText || selection === null) {
      range = null;
      controller.close();
      return;
    }
    const next = selection.getRangeAt(0);
    if (range !== null && range.startContainer === next.startContainer && range.startOffset === next.startOffset
      && range.endContainer === next.endContainer && range.endOffset === next.endOffset
      && controller.state.phase !== "hidden" && controller.state.text === extracted.text) {
      view.reposition();
      return;
    }
    range = next.cloneRange();
    controller.select(extracted.text);
  }

  function pointerDown(event: PointerEvent): void {
    insidePopup = view.containsEvent(event);
    if (!insidePopup) {
      // Dragging starts a new intent before the browser has updated its Range [ASYNC-002].
      range = null;
      controller.close();
    }
  }
  function keyDown(event: KeyboardEvent): void {
    if (event.key === "Escape") {
      // Respect the host's own Escape handling: do not stop/default-prevent its event.
      controller.close();
      range = null;
      insidePopup = false;
    } else if (!view.containsEvent(event)) {
      insidePopup = false;
    }
  }
  function pageHide(event: PageTransitionEvent): void {
    if (event.persisted) {
      // BFCache restores this same content-script instance; retain listeners, release content.
      controller.close();
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
    controller.close();
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
