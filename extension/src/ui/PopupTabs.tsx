import { useRef, type KeyboardEvent } from "react";
import { WarningIcon } from "./popupIcons.tsx";

export interface PopupTab<Key extends string> {
  readonly key: Key;
  readonly label: string;
  readonly count?: number | undefined;
  /** Short state shown after the label when the capability failed, is unavailable or is partial. */
  readonly warning?: string | undefined;
}

export const tabId = (base: string, key: string) => `${base}-tab-${key}`;
export const panelId = (base: string, key: string) => `${base}-panel-${key}`;

/** WAI-ARIA tabs with roving focus; switching tabs only changes the view and never sends a request. */
export function PopupTabs<Key extends string>({ tabs, active, onSelect, idBase, disabled = false }: {
  tabs: readonly PopupTab<Key>[];
  active: Key;
  onSelect: (key: Key) => void;
  idBase: string;
  disabled?: boolean;
}) {
  const buttons = useRef(new Map<Key, HTMLButtonElement>());
  function move(event: KeyboardEvent<HTMLDivElement>) {
    const index = tabs.findIndex(tab => tab.key === active);
    const next = event.key === "ArrowRight" ? index + 1 : event.key === "ArrowLeft" ? index - 1
      : event.key === "Home" ? 0 : event.key === "End" ? tabs.length - 1 : null;
    if (next === null || disabled) return;
    event.preventDefault();
    const target = tabs[(next + tabs.length) % tabs.length]!;
    onSelect(target.key);
    buttons.current.get(target.key)?.focus();
  }
  return <div className="jp-tabs" role="tablist" aria-label="Nội dung tra cứu" onKeyDown={move}>
    {tabs.map(tab => {
      const selected = tab.key === active;
      return <button key={tab.key} ref={node => { if (node) buttons.current.set(tab.key, node); else buttons.current.delete(tab.key); }}
        type="button" role="tab" id={tabId(idBase, tab.key)} aria-controls={panelId(idBase, tab.key)} aria-selected={selected}
        tabIndex={selected ? 0 : -1} disabled={disabled} className={selected ? "jp-tab jp-tab-active" : "jp-tab"} onClick={() => onSelect(tab.key)}>
        <span>{tab.label}</span>
        {!disabled && tab.count !== undefined && tab.count > 0 && <span className="jp-tab-count">{tab.count}</span>}
        {!disabled && tab.warning && <><WarningIcon /><span className="jp-sr-only"> ({tab.warning})</span></>}
      </button>;
    })}
  </div>;
}
