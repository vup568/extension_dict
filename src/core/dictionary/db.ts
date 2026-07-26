/**
 * IndexedDB schema (via the `idb` typed wrapper).
 *
 * Store `entries` — one record per JMdict entry, keyed by id, plus a
 * precomputed `f` array of every searchable form (all kanji + kana strings).
 * The multiEntry index on `f` is what makes exact lookup a single O(log n)
 * index query that serves both 学生 and がくせい.
 *
 * Store `kanji` (schema v2) — one PackedKanji per character, keyed by the
 * literal, for the popup's Hán tự tab.
 *
 * Store `meta` — keyed records: `dict`/`kanjiDict` mark completed imports
 * (their presence = "ready"); `importProgress`/`kanjiImportProgress` track
 * chunks completed so an interrupted first import resumes instead of
 * restarting.
 */
import { openDB } from 'idb'
import type { DBSchema, IDBPDatabase } from 'idb'
import type { PackedEntry, PackedKanji } from './packed-format'
import type { TargetLang } from '../../shared/types'

export interface StoredEntry extends PackedEntry {
  /** Searchable forms: unique union of kanji + kana texts. */
  f: string[]
}

export type MetaRecord =
  | { key: 'dict'; dictVersion: string; entryCount: number; importedAt: number }
  | { key: 'importProgress'; dictVersion: string; chunksDone: number; chunkCount: number }
  | { key: 'kanjiDict'; kanjiVersion: string; kanjiCount: number; importedAt: number }
  | { key: 'kanjiImportProgress'; kanjiVersion: string; chunksDone: number; chunkCount: number }
  | { key: 'prefs'; targetLang: TargetLang }

interface JpDictDB extends DBSchema {
  entries: {
    key: string
    value: StoredEntry
    indexes: { forms: string }
  }
  kanji: {
    key: string
    value: PackedKanji
  }
  meta: {
    key: MetaRecord['key']
    value: MetaRecord
  }
}

export type DictionaryDatabase = IDBPDatabase<JpDictDB>

let dbPromise: Promise<DictionaryDatabase> | null = null

export function getDb(): Promise<DictionaryDatabase> {
  dbPromise ??= openDB<JpDictDB>('jpdict', 2, {
    upgrade(db, oldVersion) {
      if (oldVersion < 1) {
        const entries = db.createObjectStore('entries', { keyPath: 'id' })
        entries.createIndex('forms', 'f', { multiEntry: true })
        db.createObjectStore('meta', { keyPath: 'key' })
      }
      if (oldVersion < 2) {
        db.createObjectStore('kanji', { keyPath: 'l' })
      }
    },
  })
  return dbPromise
}
