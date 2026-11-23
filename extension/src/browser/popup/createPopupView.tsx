import { createRoot, type Root } from "react-dom/client";
import { flushSync } from "react-dom";
import type { PointerEvent as ReactPointerEvent } from "react";
import type { ConjugationSpan } from "../../api/analysisContract.ts";
import type { SpeechSource } from "../../api/readingContracts.ts";
import type { AnalysisState } from "../../interaction/analysisController.ts";
import { PopupShell } from "../../ui/PopupShell.tsx";
import stylesheet from "../../ui/popup.css?inline";
import resultsStylesheet from "../../ui/reading-results.css?inline";
import type { Translate } from "../../platform/translationTransport.ts";
import { defaultReadingPreferences, normalizePopupSize, type PopupSize, type ReadingPreferences } from "../../platform/readingPreferences.ts";
import { createSpeechSession, SpeechPanel } from "../../ui/SpeechPanel.tsx";
import { GripIcon, PinIcon, SettingsIcon } from "../../ui/popupIcons.tsx";
import type { Speak } from "../../platform/readingTransport.ts";

interface ScanPresentation { offset: number; targetSpan: ConjugationSpan | null }
interface PopupControls {
  onSize?: (size: PopupSize) => void;
  onScanEnabled?: (enabled: boolean) => void;
  onPin?: () => void;
  speak?: Speak | undefined;
}

export interface PopupView {
  render(state: AnalysisState, scan?: ScanPresentation): void;
  setPreferences(preferences: ReadingPreferences): void;
  isPinned(): boolean;
  reposition(): void;
  containsEvent(event: Event): boolean;
  containsNode(node: Node | null): boolean;
  dispose(): void;
}

export function createPopupView(doc: Document, anchor: () => DOMRect | null, close: () => void, retry: () => void,
  translate?: Translate, controls: PopupControls = {}): PopupView {
  let host: HTMLElement | null = null;
  let shadow: ShadowRoot | null = null;
  let root: Root | null = null;
  let previousFocus: HTMLElement | null = null;
  let state: AnalysisState = { phase: "hidden" };
  let scan: ScanPresentation | undefined;
  let preferences = defaultReadingPreferences;
  let pinned = false;
  let finishResize: (() => void) | undefined;
  const speech = controls.speak && createSpeechSession(controls.speak);

  function visibleSize(size: PopupSize): PopupSize {
    const win = doc.defaultView!;
    return { width: Math.max(1, Math.min(size.width, (win.visualViewport?.width ?? win.innerWidth) - 24)),
      height: Math.max(1, Math.min(size.height, (win.visualViewport?.height ?? win.innerHeight) - 24)) };
  }

  function position(): void {
    const win = doc.defaultView;
    const rect = anchor();
    if (host === null || win === null || rect === null) return;
    const viewport = win.visualViewport;
    const width = viewport?.width ?? win.innerWidth;
    const height = viewport?.height ?? win.innerHeight;
    const offsetLeft = viewport?.offsetLeft ?? 0;
    const offsetTop = viewport?.offsetTop ?? 0;
    const { width: popupWidth, height: preferredHeight } = visibleSize(preferences.size);
    const below = rect.bottom + 8;
    const belowSpace = offsetTop + height - 12 - below;
    const aboveSpace = rect.top - 8 - offsetTop - 12;
    // Keep the pointer's source glyph exposed while the scan is pending and while its result is open.
    const popupHeight = scan ? Math.max(1, Math.min(preferredHeight, Math.max(belowSpace, aboveSpace))) : preferredHeight;
    host.style.setProperty("width", `${popupWidth}px`, "important");
    host.style.setProperty("--jp-popup-height", `${popupHeight}px`, "important");
    const left = Math.max(offsetLeft + 12, Math.min(rect.left, offsetLeft + width - popupWidth - 12));
    const top = popupHeight <= belowSpace ? below : rect.top - popupHeight - 8;
    host.style.setProperty("left", `${left}px`, "important");
    host.style.setProperty("top", `${Math.max(offsetTop + 12, Math.min(top, offsetTop + height - popupHeight - 12))}px`, "important");
  }

  function remove(): void {
    finishResize?.();
    speech?.release(undefined, false);
    pinned = false;
    const restore = shadow?.activeElement !== null && shadow?.activeElement !== undefined;
    root?.unmount();
    host?.remove();
    root = null;
    host = null;
    shadow = null;
    if (restore && previousFocus?.isConnected) previousFocus.focus({ preventScroll: true });
    previousFocus = null;
  }

  function resize(size: PopupSize, persist: boolean) {
    const clamped = normalizePopupSize(visibleSize(normalizePopupSize(size)));
    preferences = { ...preferences, size: clamped };
    position();
    if (persist) controls.onSize?.(clamped);
  }

  function beginResize(event: ReactPointerEvent<HTMLButtonElement>) {
    if (event.button !== 0) return;
    event.preventDefault();
    finishResize?.();
    const win = doc.defaultView!;
    const handle = event.currentTarget;
    const { pointerId, clientX, clientY } = event;
    const initial = host?.getBoundingClientRect() ?? visibleSize(preferences.size);
    try { handle.setPointerCapture(pointerId); }
    catch { return; }
    const move = (next: PointerEvent) => {
      if (next.pointerId === pointerId) resize({ width: initial.width + next.clientX - clientX,
        height: initial.height + next.clientY - clientY }, false);
    };
    const end = (next: PointerEvent) => { if (next.pointerId === pointerId) finishResize?.(); };
    finishResize = () => {
      finishResize = undefined;
      handle.removeEventListener("pointermove", move);
      handle.removeEventListener("pointerup", end);
      handle.removeEventListener("pointercancel", end);
      handle.removeEventListener("lostpointercapture", end);
      win.removeEventListener("blur", blur);
      if (handle.hasPointerCapture(pointerId)) handle.releasePointerCapture(pointerId);
      controls.onSize?.(preferences.size);
    };
    const blur = () => finishResize?.();
    handle.addEventListener("pointermove", move);
    handle.addEventListener("pointerup", end);
    handle.addEventListener("pointercancel", end);
    handle.addEventListener("lostpointercapture", end);
    win.addEventListener("blur", blur);
  }

  function draw(): void {
    const current = state;
    if (current.phase === "hidden" || root === null) return;
    const renderSpeech = speech && ((source: SpeechSource) =>
      <SpeechPanel key={JSON.stringify(source)} session={speech} source={source} />);
    const sizeButton = (label: string, symbol: string, width: number, height: number) =>
      <button type="button" className="jp-button jp-size-button" aria-label={label} title={label} onClick={() => {
        const initial = host?.getBoundingClientRect() ?? visibleSize(preferences.size);
        resize({ width: initial.width + width, height: initial.height + height }, true);
      }}>{symbol}</button>;
    const pinLabel = pinned ? "Bỏ ghim" : "Ghim";
    const buttons = <>
      <button type="button" className="jp-icon-button" aria-pressed={pinned} aria-label={pinLabel} title={pinLabel}
        onClick={() => { pinned = !pinned; if (pinned) controls.onPin?.(); draw(); }}>
        <PinIcon filled={pinned} />
      </button>
      <details className="jp-settings">
        <summary className="jp-icon-button" aria-label="Cài đặt popup" title="Cài đặt"><SettingsIcon /></summary>
        <div className="jp-settings-menu">
          {controls.onScanEnabled && <label className="jp-switch"><input type="checkbox" checked={preferences.scanEnabled}
            onClick={event => { if (!event.nativeEvent.isTrusted) event.preventDefault(); }} onChange={event => {
            if (!event.nativeEvent.isTrusted) return;
            const enabled = event.currentTarget.checked;
            preferences = { ...preferences, scanEnabled: enabled };
            controls.onScanEnabled?.(enabled);
            draw();
          }} /><span>Tra từ bằng con trỏ</span>
            <span className="jp-fine">Khi giữ Shift và dừng chuột, một đoạn ngắn quanh con trỏ sẽ được gửi tới máy chủ tra cứu.</span>
          </label>}
          <div className="jp-size-buttons" role="group" aria-label="Kích thước popup">
            <span>Chiều rộng</span>{sizeButton("Giảm chiều rộng", "−", -20, 0)}{sizeButton("Tăng chiều rộng", "+", 20, 0)}
            <span>Chiều cao</span>{sizeButton("Giảm chiều cao", "−", 0, -20)}{sizeButton("Tăng chiều cao", "+", 0, 20)}
          </div>
          <p className="jp-fine">Hoặc kéo góc dưới bên phải để đổi kích thước.</p>
        </div>
      </details>
      <button className="jp-resize-handle" type="button" aria-label="Kéo đổi kích thước popup" tabIndex={-1} onPointerDown={beginResize}><GripIcon /></button>
    </>;
    // Commit shell before transport dispatch while preserving the host's focus.
    flushSync(() => root?.render(<PopupShell key={current.interactionId} state={current} onClose={close} onRetry={retry} translate={translate}
      controls={buttons} isScan={scan !== undefined} {...(scan ? { targetSpan: scan.targetSpan, scanOffset: scan.offset } : {})}
      {...(renderSpeech ? { renderSpeech } : {})} />));
    position();
  }

  return {
    render(next, scanPresentation) {
      if (next.phase !== "hidden" && (state.phase === "hidden" || next.interactionId !== state.interactionId)) {
        finishResize?.();
        speech?.release(undefined, false);
        pinned = false;
      }
      state = next;
      scan = scanPresentation;
      if (next.phase === "hidden") { remove(); return; }
      if (host === null) {
        let focused = doc.activeElement;
        while (focused?.shadowRoot?.activeElement) focused = focused.shadowRoot.activeElement;
        previousFocus = focused instanceof HTMLElement ? focused : null;
        host = doc.createElement("jp-reading-popup");
        host.setAttribute("data-jp-reading-popup", "");
        // EARS[State]: WHILE hosted on arbitrary pages, isolate popup CSS [BROWSER-003].
        for (const [property, value] of Object.entries({
          all: "initial", position: "fixed", display: "block", visibility: "visible",
          opacity: "1", transform: "none", filter: "none", margin: "0", padding: "0",
          border: "0", "z-index": "2147483647", "pointer-events": "auto",
          // The popup follows the reader's light or dark preference rather than the host page's scheme.
          "box-sizing": "border-box", isolation: "isolate", "color-scheme": "light dark",
        })) host.style.setProperty(property, value, "important");
        shadow = host.attachShadow({ mode: "open" });
        const style = doc.createElement("style");
        style.textContent = stylesheet + resultsStylesheet;
        const container = doc.createElement("div");
        shadow.append(style, container);
        // The top layer keeps host transforms/overflow from changing fixed positioning.
        if (typeof host.showPopover === "function") {
          (doc.body ?? doc.documentElement).append(host);
          host.setAttribute("popover", "manual");
          try { host.showPopover(); }
          catch { host.removeAttribute("popover"); }
        }
        // A transformed body would otherwise become the containing block for the fixed fallback.
        if (!host.hasAttribute("popover")) doc.documentElement.append(host);
        root = createRoot(container);
      }
      draw();
    },
    setPreferences(next) {
      preferences = finishResize ? { ...next, size: preferences.size } : next;
      draw();
    },
    isPinned: () => pinned,
    reposition: position,
    containsEvent: (event) => host !== null && event.composedPath().includes(host),
    containsNode: (node) => node !== null && host !== null && (host === node || host.contains(node) || shadow?.contains(node) === true),
    dispose: remove,
  };
}
