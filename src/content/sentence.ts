/**
 * Expands a selection to the sentence that contains it, for the Ngữ pháp
 * tab ("trích cả câu đó ra"): two auxiliary Ranges cover the text before
 * and after the selection inside its block-level container(s); each side is
 * furigana-stripped (rangeText) and cut at the nearest sentence terminator.
 */
import { rangeText } from './selection'
import type { SentenceInfo } from '../shared/types'

/** Where a sentence can end. */
const TERMINATOR = /[。｡．！？!?\n]/

/** Block-ish containers that bound a sentence even without punctuation. */
const BLOCK_SELECTOR =
  'p,div,li,td,th,dd,dt,blockquote,article,section,main,aside,figcaption,figure,h1,h2,h3,h4,h5,h6,body'

/** Max characters kept on each side of the selection. */
const EDGE_CAP = 100

export function extractSentence(range: Range): SentenceInfo | null {
  try {
    const startBlock = blockOf(range.startContainer)
    const endBlock = blockOf(range.endContainer)
    if (startBlock === null || endBlock === null) return null

    const beforeRange = document.createRange()
    beforeRange.selectNodeContents(startBlock)
    beforeRange.setEnd(range.startContainer, range.startOffset)
    const afterRange = document.createRange()
    afterRange.selectNodeContents(endBlock)
    afterRange.setStart(range.endContainer, range.endOffset)

    const before = tailSentence(rangeText(beforeRange))
    const after = headSentence(rangeText(afterRange))
    const selection = rangeText(range)

    return {
      sentence: before + selection + after,
      selStart: before.length,
      selEnd: before.length + selection.length,
    }
  } catch {
    // Detached ranges / exotic DOM: the caller falls back to the selection.
    return null
  }
}

function blockOf(node: Node): Element | null {
  const element = node instanceof Element ? node : node.parentElement
  return element?.closest(BLOCK_SELECTOR) ?? null
}

/** Text after the LAST terminator, whitespace-trimmed, capped. */
function tailSentence(text: string): string {
  let cut = text
  for (let i = text.length - 1; i >= 0; i--) {
    const ch = text[i]
    if (ch !== undefined && TERMINATOR.test(ch)) {
      cut = text.slice(i + 1)
      break
    }
  }
  return cut.trimStart().slice(-EDGE_CAP)
}

/** Text up to and INCLUDING the first terminator, trimmed, capped. */
function headSentence(text: string): string {
  const match = TERMINATOR.exec(text)
  const cut = match === null ? text : text.slice(0, match.index + match[0].length)
  return cut.trimEnd().slice(0, EDGE_CAP)
}
