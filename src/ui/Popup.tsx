import { useState } from 'preact/hooks'
import type { DictionaryEntry } from '../shared/types'

/** Senses shown before the "show more" toggle kicks in. */
const SENSE_CAP = 4

/** Everything the popup can display. */
export type PopupModel =
  | { readonly kind: 'entries'; readonly entries: readonly DictionaryEntry[] }
  /** Transient states: dictionary still importing, or unavailable. */
  | { readonly kind: 'status'; readonly text: string }

export interface PopupProps {
  readonly model: PopupModel
}

export function Popup({ model }: PopupProps) {
  if (model.kind === 'status') {
    return (
      <div class="panel" role="dialog" aria-label="Dictionary status">
        <div class="status">{model.text}</div>
      </div>
    )
  }
  return <EntriesView entries={model.entries} />
}

/**
 * Dictionary entries. When several match (homophones, multiple readings),
 * ‹ › cycles through them; long sense lists are capped behind "show more".
 */
function EntriesView({ entries }: { readonly entries: readonly DictionaryEntry[] }) {
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
