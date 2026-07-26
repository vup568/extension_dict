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

export interface DictIndexFile {
  formatVersion: number
  /** Source version identity, e.g. "3.6.1/2026-07-01/full". */
  dictVersion: string
  edition: 'full' | 'common'
  entryCount: number
  chunkCount: number
  /** JMdict tag code → human-readable description (e.g. "n" → "noun"). */
  tags: Record<string, string>
}

export const DICT_FORMAT_VERSION = 1

export function chunkFileName(index: number): string {
  return `chunk-${String(index).padStart(3, '0')}.json`
}
