/**
 * Per-character kanji queries against the `kanji` store (KANJIDIC2 data),
 * for the popup's Hán tự tab. Radical numbers are resolved to display info
 * (glyph + Hán Việt name) here so the UI stays language-agnostic.
 */
import { getDb } from './db'
import type { PackedKanji } from './packed-format'
import { radicalByNumber } from '../language/japanese/radicals'
import type { KanjiInfo, KanjiRadicalInfo } from '../../shared/types'

/** Defensive cap on characters answered per request. */
const MAX_KANJI_PER_REQUEST = 50

export interface KanjiQueryResult {
  /** False while the kanji data set is still importing (or absent). */
  readonly ready: boolean
  readonly kanji: readonly KanjiInfo[]
}

/** Keep single-character ja→vi glosses card-sized. */
const MAX_VI_MEANING_LENGTH = 90

export async function getKanjiInfo(chars: readonly string[]): Promise<KanjiQueryResult> {
  const db = await getDb()
  const marker = await db.get('meta', 'kanjiDict')
  const ready = marker?.key === 'kanjiDict'
  const kanji: KanjiInfo[] = []
  for (const ch of chars.slice(0, MAX_KANJI_PER_REQUEST)) {
    const packed = await db.get('kanji', ch)
    if (packed === undefined) continue
    const javi = await db.get('javi', ch)
    kanji.push(toKanjiInfo(packed, javi?.v ?? null))
  }
  return { ready, kanji }
}

function toKanjiInfo(packed: PackedKanji, viMeaning: string | null): KanjiInfo {
  return {
    literal: packed.l,
    hanViet: packed.v ?? [],
    on: packed.o ?? [],
    kun: packed.u ?? [],
    meanings: packed.m ?? [],
    viMeaning: viMeaning === null ? null : capViMeaning(viMeaning),
    strokes: packed.s ?? null,
    jlpt: packed.j ?? null,
    radical: resolveRadical(packed.b),
  }
}

function capViMeaning(text: string): string {
  if (text.length <= MAX_VI_MEANING_LENGTH) return text
  const cut = text.slice(0, MAX_VI_MEANING_LENGTH)
  const lastSep = Math.max(cut.lastIndexOf(';'), cut.lastIndexOf(','))
  return (lastSep > 20 ? cut.slice(0, lastSep) : cut).trimEnd() + '…'
}

function resolveRadical(num: number | undefined): KanjiRadicalInfo | null {
  if (num === undefined) return null
  const radical = radicalByNumber(num)
  if (radical === null) return null
  return { glyph: radical.glyph, hanViet: radical.hanViet, variant: radical.variant ?? null }
}
