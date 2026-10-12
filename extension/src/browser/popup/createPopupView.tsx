import { createRoot, type Root } from "react-dom/client";
import { flushSync } from "react-dom";
import type { AnalysisState } from "../../interaction/analysisController.ts";
import { PopupShell } from "../../ui/PopupShell.tsx";
import stylesheet from "../../ui/popup.css?inline";

export interface PopupView {
  render(state: AnalysisState): void;
  reposition(): void;
  containsEvent(event: Event): boolean;
  containsNode(node: Node | null): boolean;
  dispose(): void;
}

export function createPopupView(doc: Document, anchor: () => DOMRect | null, close: () => void, retry: () => void): PopupView {
  let host: HTMLElement | null = null;
  let shadow: ShadowRoot | null = null;
  let root: Root | null = null;
  let previousFocus: HTMLElement | null = null;

  function position(): void {
    const win = doc.defaultView;
    const rect = anchor();
    if (host === null || win === null || rect === null) return;
    const viewport = win.visualViewport;
    const width = viewport?.width ?? win.innerWidth;
    const height = viewport?.height ?? win.innerHeight;
    const offsetLeft = viewport?.offsetLeft ?? 0;
    const offsetTop = viewport?.offsetTop ?? 0;
    const popupWidth = Math.max(1, Math.min(380, width - 24));
    host.style.setProperty("width", `${popupWidth}px`, "important");
    host.style.setProperty("max-height", `${Math.max(1, height - 24)}px`, "important");
    host.style.setProperty("--jp-max-height", `${Math.max(1, height - 24)}px`, "important");
    const popupHeight = Math.min(host.getBoundingClientRect().height, height - 24);
    const left = Math.max(offsetLeft + 12, Math.min(rect.left, offsetLeft + width - popupWidth - 12));
    const below = rect.bottom + 8;
    const top = below + popupHeight <= offsetTop + height - 12 ? below : rect.top - popupHeight - 8;
    host.style.setProperty("left", `${left}px`, "important");
    host.style.setProperty("top", `${Math.max(offsetTop + 12, Math.min(top, offsetTop + height - popupHeight - 12))}px`, "important");
  }

  function remove(): void {
    const restore = shadow?.activeElement !== null && shadow?.activeElement !== undefined;
    root?.unmount();
    host?.remove();
    root = null;
    host = null;
    shadow = null;
    if (restore && previousFocus?.isConnected) previousFocus.focus({ preventScroll: true });
    previousFocus = null;
  }

  return {
    render(state) {
      if (state.phase === "hidden") { remove(); return; }
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
          "box-sizing": "border-box", isolation: "isolate", "color-scheme": "light",
        })) host.style.setProperty(property, value, "important");
        shadow = host.attachShadow({ mode: "open" });
        const style = doc.createElement("style");
        style.textContent = stylesheet;
        const container = doc.createElement("div");
        shadow.append(style, container);
        (doc.body ?? doc.documentElement).append(host);
        // The top layer keeps host transforms/overflow from changing fixed positioning.
        if (typeof host.showPopover === "function") {
          host.setAttribute("popover", "manual");
          host.showPopover();
        }
        root = createRoot(container);
      }
      // Commit shell before any transport dispatch; preserve focus on automatic opening.
      flushSync(() => root?.render(<PopupShell state={state} onClose={close} onRetry={retry} />));
      position();
    },
    reposition: position,
    containsEvent: (event) => host !== null && event.composedPath().includes(host),
    containsNode: (node) => node !== null && host !== null && (host === node || host.contains(node) || shadow?.contains(node) === true),
    dispose: remove,
  };
}
