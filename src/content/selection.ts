/**
 * Selection detection. Language-agnostic: whether a selection is "relevant"
 * is decided entirely by the injected `shouldHandle` predicate.
 */
import { debounce } from '../shared/debounce'

/** Debounce so drag-selection / shift+arrow bursts don't thrash the popup. */
const DEBOUNCE_MS = 200

/**
 * Ignore huge selections (select-all etc.). Generous enough for several
 * sentences — sentence translation handles long selections. Measured on the
 * ruby-stripped text.
 */
const MAX_SELECTION_LENGTH = 2000

/**
 * Above this length callers should treat the selection as translate-only:
 * tokenizing and dictionary work on paragraph-sized text is slow and the
 * resulting hundred-chip token strip would be useless.
 */
export const LOOKUP_MAX_LENGTH = 500

/** Cheap raw-length cutoff before the pricier ruby-stripping path runs. */
const HARD_RAW_CAP = 4000

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
    const raw = selection.toString()
    if (raw.trim().length === 0 || raw.length > HARD_RAW_CAP) {
      handlers.onClear()
      return
    }
    const range = selection.getRangeAt(0)
    const text = extractText(raw, range).trim()
    if (text.length === 0 || text.length > MAX_SELECTION_LENGTH || !shouldHandle(text)) {
      handlers.onClear()
      return
    }
    handlers.onSelect(text, range)
  }

  const debounced = debounce(evaluate, DEBOUNCE_MS)
  const listener = (): void => debounced.run()
  document.addEventListener('selectionchange', listener)
  return () => {
    debounced.cancel()
    document.removeEventListener('selectionchange', listener)
  }
}

/** Furigana-stripped text of an arbitrary Range (see extractText). */
export function rangeText(range: Range): string {
  return extractText(range.toString(), range)
}

/**
 * Selection text with ruby annotations (furigana) removed. Sites like NHK
 * Easy News wrap readings in <ruby>…<rt>…</rt></ruby>, and
 * Selection.toString() includes the <rt> text — which would corrupt
 * tokenization, dictionary lookup, and translation alike. When the range
 * touches ruby markup, clone its contents, drop every rt/rp node, and read
 * the remaining text instead.
 */
function extractText(raw: string, range: Range): string {
  const container = range.commonAncestorContainer
  const root = container instanceof Element ? container : container.parentElement
  if (root === null) return raw
  if (root.closest('ruby') === null && root.querySelector('rt, rp') === null) return raw
  const fragment = range.cloneContents()
  for (const annotation of fragment.querySelectorAll('rt, rp')) annotation.remove()
  return fragment.textContent ?? ''
}
