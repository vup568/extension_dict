/**
 * Per-character kanji queries against the `kanji` store (KANJIDIC2 data),
 * for the popup's Hán tự tab.
 */
import { getDb } from './db'
import type { PackedKanji } from './packed-format'
import type { KanjiInfo } from '../../shared/types'

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
    strokes: packed.s ?? null,
    grade: packed.g ?? null,
    jlpt: packed.j ?? null,
    freq: packed.f ?? null,
  }
}
