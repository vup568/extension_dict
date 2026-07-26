import { useState } from 'preact/hooks'
import type { DictionaryEntry, GrammarPart, KanjiInfo, TargetLang, TokenInfo, TranslationState } from '../shared/types'

/** Senses shown before the "show more" toggle kicks in. */
const SENSE_CAP = 4

/**
 * Hán tự tab data lifecycle: 'idle' until the tab is first opened (the
 * Popup then asks the content script to fetch), 'loading' while in flight.
 */
export type KanjiTabState =
  | 'idle'
  | 'loading'
  | { readonly ready: boolean; readonly kanji: readonly KanjiInfo[] }

/** Everything the popup can display. */
export type PopupModel =
  | {
      readonly kind: 'result'
      /** Empty array → "no match" display in the Từ vựng tab. */
      readonly entries: readonly DictionaryEntry[]
      readonly tokens: readonly TokenInfo[] | null
      /** Breakdown of the conjugated unit being viewed, when there is one. */
      readonly grammar: readonly GrammarPart[] | null
      readonly deinflectionAvailable: boolean
      /** Unique kanji of the selection — the Hán tự tab count and query. */
      readonly kanjiChars: readonly string[]
      readonly kanjiTab: KanjiTabState
      /** null → not started yet (single-word selections translate lazily). */
      readonly translation: TranslationState | null
    }
  /** Long selections: translation only, no tabs. */
  | { readonly kind: 'translation'; readonly translation: TranslationState }
  /** Transient states: dictionary still importing, or unavailable. */
  | { readonly kind: 'status'; readonly text: string }

/** Environment + callbacks for the sentence-translation controls. */
export interface TranslationControls {
  readonly targetLang: TargetLang
  readonly onTargetLangChange: (target: TargetLang) => void
  readonly onRetry: () => void
  /** True when the on-device pack could be downloaded but isn't yet. */
  readonly offlinePackAvailable: boolean
  readonly onDownloadPack: () => void
}

export interface PopupProps {
  readonly model: PopupModel
  readonly onTokenClick?: (token: TokenInfo) => void
  /** Fired when the Hán tự tab is opened while its data is still 'idle'. */
  readonly onRequestKanji?: () => void
  /** Fired when the Dịch tab is opened before any translation started. */
  readonly onRequestTranslation?: () => void
  readonly translation?: TranslationControls
}

export function Popup({ model, onTokenClick, onRequestKanji, onRequestTranslation, translation }: PopupProps) {
  switch (model.kind) {
    case 'status':
      return (
        <div class="panel" role="dialog" aria-label="Trạng thái từ điển">
          <DragHandle />
          <div class="status">{model.text}</div>
        </div>
      )
    case 'translation':
      return (
        <div class="panel" role="dialog" aria-label="Bản dịch">
          <DragHandle />
          <TranslateSection state={model.translation} controls={translation} />
        </div>
      )
    case 'result':
      return (
        <ResultView
          model={model}
          onTokenClick={onTokenClick}
          onRequestKanji={onRequestKanji}
          onRequestTranslation={onRequestTranslation}
          translationControls={translation}
        />
      )
  }
}

/** Grip strip: the popup can be dragged by this (or any empty panel area). */
function DragHandle() {
  return <div class="drag-handle" aria-hidden="true" />
}

// ---------------------------------------------------------------------------
// Tabbed result view

type ResultModel = Extract<PopupModel, { kind: 'result' }>
type TabId = 'vocab' | 'kanji' | 'translate'

function ResultView({
  model,
  onTokenClick,
  onRequestKanji,
  onRequestTranslation,
  translationControls,
}: {
  readonly model: ResultModel
  readonly onTokenClick?: (token: TokenInfo) => void
  readonly onRequestKanji?: () => void
  readonly onRequestTranslation?: () => void
  readonly translationControls?: TranslationControls
}) {
  const [active, setActive] = useState<TabId>('vocab')
  const openTab = (tab: TabId): void => {
    setActive(tab)
    if (tab === 'kanji' && model.kanjiTab === 'idle') onRequestKanji?.()
    if (tab === 'translate' && model.translation === null) onRequestTranslation?.()
  }
  const hasKanji = model.kanjiChars.length > 0
  const tab = (id: TabId, label: string) => (
    <button
      class={`tab${active === id ? ' active' : ''}`}
      role="tab"
      aria-selected={active === id}
      onClick={() => openTab(id)}
    >
      {label}
    </button>
  )
  return (
    <div class="panel" role="dialog" aria-label="Từ điển">
      <DragHandle />
      <div class="tabs" role="tablist">
        {tab('vocab', 'Từ vựng')}
        {hasKanji && tab('kanji', `Hán tự (${model.kanjiChars.length})`)}
        {tab('translate', 'Dịch')}
      </div>
      {active === 'vocab' && (
        <VocabView
          key={model.entries[0]?.id ?? 'no-match'}
          entries={model.entries}
          tokens={model.tokens}
          grammar={model.grammar}
          deinflectionAvailable={model.deinflectionAvailable}
          onTokenClick={onTokenClick}
        />
      )}
      {active === 'kanji' && <KanjiView state={model.kanjiTab} />}
      {active === 'translate' &&
        (model.translation !== null ? (
          <TranslateSection state={model.translation} controls={translationControls} />
        ) : (
          <div class="status">Đang dịch…</div>
        ))}
    </div>
  )
}

// ---------------------------------------------------------------------------
// Từ vựng tab

/**
 * The clickable token list for phrase selections. Each chip shows the
 * surface text and looks up the token's dictionary form when clicked.
 */
function TokenStrip({
  tokens,
  onTokenClick,
}: {
  readonly tokens: readonly TokenInfo[]
  readonly onTokenClick?: (token: TokenInfo) => void
}) {
  return (
    <div class="tokens">
      {tokens.map((token, i) => (
        <button
          key={`${i}-${token.surface}`}
          class="token"
          lang="ja"
          title={token.lookupTerm === token.surface ? undefined : token.lookupTerm}
          onClick={() => onTokenClick?.(token)}
        >
          {token.surface}
        </button>
      ))}
    </div>
  )
}

/** Per-part breakdown of a conjugated grammar unit (なりました etc.). */
function GrammarSection({ parts }: { readonly parts: readonly GrammarPart[] }) {
  return (
    <div class="grammar">
      <div class="section-label">Cách chia</div>
      <ul class="grammar-list">
        {parts.map((part, i) => (
          <li class="grammar-row" key={`${i}-${part.surface}`}>
            <span class="grammar-surface" lang="ja">
              {part.surface}
            </span>
            <span class="grammar-desc">{part.description}</span>
          </li>
        ))}
      </ul>
    </div>
  )
}

/**
 * Dictionary entries (or the no-match note). When several entries match
 * (homophones, multiple readings), ‹ › cycles through them; long sense
 * lists are capped behind "show more". Keyed by the first entry id from the
 * parent so its state resets when a token click swaps the content.
 */
function VocabView({
  entries,
  tokens,
  grammar,
  deinflectionAvailable,
  onTokenClick,
}: {
  readonly entries: readonly DictionaryEntry[]
  readonly tokens: readonly TokenInfo[] | null
  readonly grammar: readonly GrammarPart[] | null
  readonly deinflectionAvailable: boolean
  readonly onTokenClick?: (token: TokenInfo) => void
}) {
  const [entryIndex, setEntryIndex] = useState(0)
  const [showAllSenses, setShowAllSenses] = useState(false)

  const entry = entries[entryIndex]
  const allSenses = entry?.senses ?? []
  const senses = showAllSenses ? allSenses : allSenses.slice(0, SENSE_CAP)
  const hiddenCount = allSenses.length - senses.length

  const cycle = (delta: number): void => {
    setEntryIndex((entryIndex + delta + entries.length) % entries.length)
    setShowAllSenses(false)
  }

  return (
    <>
      {tokens !== null && tokens.length > 0 && <TokenStrip tokens={tokens} onTokenClick={onTokenClick} />}
      {entry === undefined ? (
        <div class="status">
          {deinflectionAvailable
            ? 'Không tìm thấy trong từ điển'
            : 'Không tìm thấy (bộ tách từ chưa sẵn sàng)'}
        </div>
      ) : (
        <>
          {entries.length > 1 && (
            <div class="entry-nav">
              <button class="nav-btn" onClick={() => cycle(-1)} aria-label="Mục trước">
                ‹
              </button>
              <span>
                {entryIndex + 1} / {entries.length}
              </span>
              <button class="nav-btn" onClick={() => cycle(1)} aria-label="Mục sau">
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
              xem thêm {hiddenCount} nghĩa
            </button>
          )}
        </>
      )}
      {grammar !== null && <GrammarSection parts={grammar} />}
    </>
  )
}

// ---------------------------------------------------------------------------
// Hán tự tab

function KanjiView({ state }: { readonly state: KanjiTabState }) {
  if (state === 'idle' || state === 'loading') {
    return <div class="status">Đang tải dữ liệu Hán tự…</div>
  }
  if (state.kanji.length === 0) {
    return (
      <div class="status">
        {state.ready
          ? 'Chưa có dữ liệu cho Hán tự này.'
          : 'Dữ liệu Hán tự đang được nhập (xem % trên icon extension) — thử lại sau nhé.'}
      </div>
    )
  }
  return (
    <div class="kanji-list">
      {state.kanji.map((info) => (
        <KanjiCard key={info.literal} info={info} />
      ))}
      {!state.ready && <div class="status">Dữ liệu Hán tự chưa nhập xong — một số chữ có thể còn thiếu.</div>}
    </div>
  )
}

function KanjiCard({ info }: { readonly info: KanjiInfo }) {
  const meta: string[] = []
  if (info.strokes !== null) meta.push(`${info.strokes} nét`)
  if (info.jlpt !== null) meta.push(`JLPT N${info.jlpt}`)
  if (info.grade !== null) meta.push(gradeLabel(info.grade))
  if (info.freq !== null) meta.push(`top ${info.freq} báo chí`)
  return (
    <div class="kanji-card">
      <div class="kanji-head">
        <span class="kanji-literal" lang="ja">
          {info.literal}
        </span>
        <span class="kanji-headline">
          {info.hanViet.length > 0 && <span class="kanji-hanviet">{info.hanViet.join(' · ')}</span>}
          {info.meanings.length > 0 && <span class="kanji-meanings">{info.meanings.join('; ')}</span>}
        </span>
      </div>
      {info.on.length > 0 && (
        <div class="kanji-row">
          <span class="kanji-row-label">Âm on</span>
          <span lang="ja">{info.on.join('、')}</span>
        </div>
      )}
      {info.kun.length > 0 && (
        <div class="kanji-row">
          <span class="kanji-row-label">Âm kun</span>
          <span lang="ja">{info.kun.join('、')}</span>
        </div>
      )}
      {meta.length > 0 && <div class="kanji-meta">{meta.join(' · ')}</div>}
    </div>
  )
}

function gradeLabel(grade: number): string {
  if (grade >= 1 && grade <= 6) return `lớp ${grade} tiểu học`
  if (grade === 8) return 'jōyō (trung học)'
  return 'kanji tên riêng'
}

// ---------------------------------------------------------------------------
// Dịch tab

/**
 * Auto-translation area; VI|EN retranslates on switch. Also used standalone
 * for long, translation-only selections.
 */
function TranslateSection({
  state,
  controls,
}: {
  readonly state: TranslationState
  readonly controls?: TranslationControls
}) {
  if (controls === undefined) return null
  const busy = state.status === 'downloading' || state.status === 'translating'
  const langBtn = (lang: TargetLang, label: string) => (
    <button
      class={`lang-btn${controls.targetLang === lang ? ' active' : ''}`}
      disabled={busy}
      onClick={() => controls.onTargetLangChange(lang)}
    >
      {label}
    </button>
  )
  return (
    <div class="translate">
      <div class="translate-row">
        <span class="section-label">Bản dịch</span>
        <span class="lang-toggle">
          {langBtn('vi', 'VI')}
          {langBtn('en', 'EN')}
        </span>
      </div>
      {state.status === 'translating' && <div class="translate-note">Đang dịch…</div>}
      {state.status === 'downloading' && (
        <div class="translate-note">Đang tải gói dịch offline (chỉ một lần)… {state.pct}%</div>
      )}
      {state.status === 'error' && (
        <div class="translate-note error">
          {state.message}{' '}
          <button class="link-btn" onClick={controls.onRetry}>
            Thử lại
          </button>
        </div>
      )}
      {state.status === 'done' && (
        <div
          class="translate-result"
          title={state.engine === 'device' ? 'Dịch trên máy (offline, riêng tư)' : 'Dịch online (Google)'}
        >
          {state.text}
        </div>
      )}
      {state.status === 'done' && state.engine === 'cloud' && controls.offlinePackAvailable && (
        <button
          class="link-btn"
          title="Chrome tải gói ngôn ngữ một lần; sau đó dịch riêng tư và hoạt động offline"
          onClick={controls.onDownloadPack}
        >
          ⬇ Tải gói dịch offline
        </button>
      )}
    </div>
  )
}
