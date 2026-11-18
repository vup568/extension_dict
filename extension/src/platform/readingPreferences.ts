export interface PopupSize { width: number; height: number }
export interface ReadingPreferences { scanEnabled: boolean; size: PopupSize }
export interface ReadingPreferenceStore {
  subscribe(listener: (preferences: ReadingPreferences) => void): () => void;
  setSize(size: PopupSize): void;
  setScanEnabled(enabled: boolean): void;
}

const sizeKey = "jpReadingPopupSizeV1";
const scanKey = "jpReadingPointerScanV1";
export const defaultReadingPreferences: ReadingPreferences = { scanEnabled: false, size: { width: 380, height: 480 } };

export function normalizePopupSize(raw: unknown): PopupSize {
  const size = raw as Partial<PopupSize> | null | undefined;
  const dimension = (value: unknown, fallback: number, min: number, max: number) =>
    typeof value === "number" && Number.isFinite(value) ? Math.round(Math.min(max, Math.max(min, value))) : fallback;
  return { width: dimension(size?.width, 380, 280, 640), height: dimension(size?.height, 480, 160, 4096) };
}

interface StorageChanges { [key: string]: { newValue?: unknown } }
interface ExtensionStorage {
  local: {
    get(keys: string[], callback: (values: Record<string, unknown>) => void): void;
    set(values: Record<string, unknown>, callback: () => void): void;
  };
  onChanged: {
    addListener(listener: (changes: StorageChanges, area: string) => void): void;
    removeListener(listener: (changes: StorageChanges, area: string) => void): void;
  };
}

export function createReadingPreferenceStore(): ReadingPreferenceStore {
  const chrome = (globalThis as unknown as { chrome?: { storage?: ExtensionStorage; runtime?: { lastError?: unknown } } }).chrome;
  const storage = chrome?.storage;
  function save(key: string, value: unknown): void {
    try { storage?.local.set({ [key]: value }, () => { void chrome?.runtime?.lastError; }); }
    catch { /* The frame can keep using its in-memory settings. */ }
  }
  return {
    subscribe(listener) {
      if (!storage) return () => {};
      let active = true;
      let current = defaultReadingPreferences;
      let changedSize = false;
      let changedScan = false;
      function changed(changes: StorageChanges, area: string) {
        if (!active || area !== "local" || !(sizeKey in changes || scanKey in changes)) return;
        if (sizeKey in changes) {
          changedSize = true;
          current = { ...current, size: normalizePopupSize(changes[sizeKey]?.newValue) };
        }
        if (scanKey in changes) {
          changedScan = true;
          current = { ...current, scanEnabled: changes[scanKey]?.newValue === true };
        }
        listener(current);
      }
      storage.onChanged.addListener(changed);
      try {
        storage.local.get([sizeKey, scanKey], values => {
          if (chrome?.runtime?.lastError || !active) return;
          current = { size: changedSize ? current.size : normalizePopupSize(values[sizeKey]),
            scanEnabled: changedScan ? current.scanEnabled : values[scanKey] === true };
          listener(current);
        });
      } catch { /* Missing or disconnected storage keeps the safe defaults. */ }
      return () => { active = false; storage.onChanged.removeListener(changed); };
    },
    setSize: size => save(sizeKey, normalizePopupSize(size)),
    setScanEnabled: enabled => save(scanKey, enabled),
  };
}
