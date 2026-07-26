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

export async function getKanjiInfo(chars: readonly string[]): Promise<KanjiQueryResult> {
  const db = await getDb()
  const marker = await db.get('meta', 'kanjiDict')
  const ready = marker?.key === 'kanjiDict'
  const kanji: KanjiInfo[] = []
  for (const ch of chars.slice(0, MAX_KANJI_PER_REQUEST)) {
    const packed = await db.get('kanji', ch)
    if (packed !== undefined) kanji.push(toKanjiInfo(packed))
  }
  return { ready, kanji }
}

function toKanjiInfo(packed: PackedKanji): KanjiInfo {
  return {
    literal: packed.l,
    hanViet: packed.v ?? [],
    on: packed.o ?? [],
    kun: packed.u ?? [],
    meanings: packed.m ?? [],
    viMeaning: null,
    strokes: packed.s ?? null,
    jlpt: packed.j ?? null,
    radical: resolveRadical(packed.b),
  }
}

function resolveRadical(num: number | undefined): KanjiRadicalInfo | null {
  if (num === undefined) return null
  const radical = radicalByNumber(num)
  if (radical === null) return null
  return { glyph: radical.glyph, hanViet: radical.hanViet, variant: radical.variant ?? null }
}
