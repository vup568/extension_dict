/**
 * The compact on-disk dictionary format produced by scripts/prepare-dict.ts
 * and consumed by the importer. Field names are single letters because the
 * full dictionary is ~200k entries — short keys save tens of MB across the
 * packaged chunks and the IndexedDB copy.
 */

export interface PackedSense {
  /** Part-of-speech tag codes (e.g. "n", "v1", "adj-i") from JMdict. */
  p: string[]
  /** English glosses. */
  g: string[]
}

export interface PackedEntry {
  /** JMdict sequence id. */
  id: string
  /** Kanji forms, most common first (may be empty for kana-only words). */
  k: string[]
  /** Kana readings. */
  r: string[]
  /** True if any form is marked common in JMdict — used for ranking. */
  c: boolean
  s: PackedSense[]
}

/**
 * One KANJIDIC2 character, packed by scripts/prepare-kanji.ts. Optional
 * fields are simply absent when the source has no data — with 13k records
 * that saves noticeably over explicit nulls.
 */
export interface PackedKanji {
  /** The character itself (store key). */
  l: string
  /** Hán Việt (Sino-Vietnamese) readings, e.g. ["Học"]. */
  v?: string[]
  /** On readings (katakana). */
  o?: string[]
  /** Kun readings (hiragana, may contain okurigana dots: まな.ぶ). */
  u?: string[]
  /** English meanings. */
  m?: string[]
  /** Stroke count. */
  s?: number
  /**
   * JLPT level on the modern N-scale (5,4,2,1), converted at pack time from
   * KANJIDIC2's pre-2010 1–4 scale (4→N5, 3→N4, 2→N2, 1→N1; the old scale
   * had no N3 equivalent).
   */
  j?: number
  /** Kangxi (classical) radical number 1–214 — the bộ thủ. */
  b?: number
}

/** Kanji data present in the package — absent when only prepare-dict ran. */
export interface KanjiIndexInfo {
  kanjiVersion: string
  kanjiCount: number
  kanjiChunkCount: number
}

/**
 * One Japanese→Vietnamese gloss record (FVDP/OVDP data, GPL — see README),
 * packed by scripts/prepare-javi.ts. Cleaned at pack time: pivot noise
 * stripped, capped length.
 */
export interface PackedJavi {
  /** Headword (kanji form or kana) — store key. */
  h: string
  /** Kana reading when the source states one (homograph disambiguation). */
  r?: string
  /** Vietnamese gloss, senses joined by "; ". */
  v: string
}

/** Ja→vi gloss data present in the package. */
export interface JaviIndexInfo {
  javiVersion: string
  javiCount: number
  javiChunkCount: number
}

export interface DictIndexFile {
  formatVersion: number
  /** Source version identity, e.g. "3.6.1/2026-07-01/full". */
  dictVersion: string
  edition: 'full' | 'common'
  entryCount: number
  chunkCount: number
  /** JMdict tag code → human-readable description (e.g. "n" → "noun"). */
  tags: Record<string, string>
  kanji?: KanjiIndexInfo
  javi?: JaviIndexInfo
}

export const DICT_FORMAT_VERSION = 1

export function chunkFileName(index: number): string {
  return `chunk-${String(index).padStart(3, '0')}.json`
}

export function kanjiChunkFileName(index: number): string {
  return `kanji-${String(index).padStart(3, '0')}.json`
}

export function javiChunkFileName(index: number): string {
  return `javi-${String(index).padStart(3, '0')}.json`
}
