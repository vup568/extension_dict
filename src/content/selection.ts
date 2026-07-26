/**
 * Selection detection. Language-agnostic: whether a selection is "relevant"
 * is decided entirely by the injected `shouldHandle` predicate.
 */
import { debounce } from '../shared/debounce'

/** Debounce so drag-selection / shift+arrow bursts don't thrash the popup. */
const DEBOUNCE_MS = 200

/** Ignore huge selections (select-all etc.) — they are never a word lookup. */
const MAX_SELECTION_LENGTH = 120

export interface SelectionHandlers {
  onSelect(text: string, range: Range): void
  onClear(): void
}

/**
 * Watches the document's selection via `selectionchange` (fires for mouse,
 * keyboard, and programmatic selections alike, including in SPAs).
 * Returns an unsubscribe function.
 */
export function watchSelection(
  shouldHandle: (text: string) => boolean,
  handlers: SelectionHandlers,
): () => void {
  const evaluate = (): void => {
    const selection = document.getSelection()
    if (selection === null || selection.rangeCount === 0 || selection.isCollapsed) {
      handlers.onClear()
      return
    }
    const text = selection.toString().trim()
    if (text.length === 0 || text.length > MAX_SELECTION_LENGTH || !shouldHandle(text)) {
      handlers.onClear()
      return
    }
    handlers.onSelect(text, selection.getRangeAt(0))
  }

  const debounced = debounce(evaluate, DEBOUNCE_MS)
  const listener = (): void => debounced.run()
  document.addEventListener('selectionchange', listener)
  return () => {
    debounced.cancel()
    document.removeEventListener('selectionchange', listener)
  }
}
