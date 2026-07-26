import { useState } from 'preact/hooks'
import type { DictionaryEntry, TokenInfo } from '../shared/types'

/** Senses shown before the "show more" toggle kicks in. */
const SENSE_CAP = 4

/** Everything the popup can display. */
export type PopupModel =
  | {
      readonly kind: 'entries'
      readonly entries: readonly DictionaryEntry[]
      readonly tokens: readonly TokenInfo[] | null
    }
  | {
      readonly kind: 'no-match'
      readonly tokens: readonly TokenInfo[] | null
      readonly deinflectionAvailable: boolean
    }
  /** Transient states: dictionary still importing, or unavailable. */
  | { readonly kind: 'status'; readonly text: string }

export interface PopupProps {
  readonly model: PopupModel
  readonly onTokenClick?: (lookupTerm: string) => void
}

export function Popup({ model, onTokenClick }: PopupProps) {
  switch (model.kind) {
    case 'status':
      return (
        <div class="panel" role="dialog" aria-label="Dictionary status">
          <div class="status">{model.text}</div>
        </div>
      )
    case 'no-match':
      return (
        <div class="panel" role="dialog" aria-label="Dictionary result">
          <div class="status">
            {model.deinflectionAvailable ? 'No match found' : 'No match found (tokenizer unavailable)'}
          </div>
          {model.tokens !== null && model.tokens.length > 0 && (
            <TokenStrip tokens={model.tokens} onTokenClick={onTokenClick} />
          )}
        </div>
      )
    case 'entries':
      return <EntriesView entries={model.entries} tokens={model.tokens} onTokenClick={onTokenClick} />
  }
}

/**
 * The clickable token list for phrase selections. Each chip shows the
 * surface text and looks up the token's dictionary form when clicked.
 */
function TokenStrip({
  tokens,
  onTokenClick,
}: {
  readonly tokens: readonly TokenInfo[]
  readonly onTokenClick?: (lookupTerm: string) => void
}) {
  return (
    <div class="tokens">
      {tokens.map((token, i) => (
        <button
          key={`${i}-${token.surface}`}
          class="token"
          lang="ja"
          title={token.lookupTerm === token.surface ? undefined : token.lookupTerm}
          onClick={() => onTokenClick?.(token.lookupTerm)}
        >
          {token.surface}
        </button>
      ))}
    </div>
  )
}

/**
 * Dictionary entries. When several match (homophones, multiple readings),
 * ‹ › cycles through them; long sense lists are capped behind "show more".
 */
function EntriesView({
  entries,
  tokens,
  onTokenClick,
}: {
  readonly entries: readonly DictionaryEntry[]
  readonly tokens: readonly TokenInfo[] | null
  readonly onTokenClick?: (lookupTerm: string) => void
}) {
  const [entryIndex, setEntryIndex] = useState(0)
  const [showAllSenses, setShowAllSenses] = useState(false)

  const entry = entries[entryIndex]
  if (entry === undefined) return null

  const senses = showAllSenses ? entry.senses : entry.senses.slice(0, SENSE_CAP)
  const hiddenCount = entry.senses.length - senses.length

  const cycle = (delta: number): void => {
    setEntryIndex((entryIndex + delta + entries.length) % entries.length)
    setShowAllSenses(false)
  }

  return (
    <div class="panel" role="dialog" aria-label="Dictionary entry">
      {tokens !== null && tokens.length > 0 && <TokenStrip tokens={tokens} onTokenClick={onTokenClick} />}
      {entries.length > 1 && (
        <div class="entry-nav">
          <button class="nav-btn" onClick={() => cycle(-1)} aria-label="Previous entry">
            ‹
          </button>
          <span>
            {entryIndex + 1} / {entries.length}
          </span>
          <button class="nav-btn" onClick={() => cycle(1)} aria-label="Next entry">
            ›
          </button>
        </div>
      )}
      <div class="headword">
        <span class="expression" lang="ja">
          {entry.expression}
        </span>
        <span class="reading" lang="ja">
          {entry.reading}
        </span>
      </div>
      <ol class="senses">
        {senses.map((sense, i) => (
          <li class="sense" key={i}>
            <span class="pos">{sense.partsOfSpeech.join(', ')}</span>
            <span>{sense.glosses.join('; ')}</span>
          </li>
        ))}
      </ol>
      {hiddenCount > 0 && (
        <button class="more-btn" onClick={() => setShowAllSenses(true)}>
          show {hiddenCount} more {hiddenCount > 1 ? 'senses' : 'sense'}
        </button>
      )}
    </div>
  )
}
