/**
 * Dictionary lookup queries against IndexedDB.
 */
import { getDb } from './db'
import type { StoredEntry } from './db'
import type { DictionaryEntry } from '../../shared/types'

/** Cap on entries returned for a single term. */
const MAX_MATCHES = 8

/**
 * Exact-match lookup: the term must equal one of an entry's kanji or kana
 * forms. Entries marked common in JMdict rank first.
 */
export async function lookupExact(term: string): Promise<DictionaryEntry[]> {
  const normalized = term.normalize('NFC').trim()
  if (normalized.length === 0) return []
  const db = await getDb()
  const stored = await db.getAllFromIndex('entries', 'forms', normalized)
  return stored
    .sort((a, b) => Number(b.c) - Number(a.c))
    .slice(0, MAX_MATCHES)
    .map(toDictionaryEntry)
}

function toDictionaryEntry(entry: StoredEntry): DictionaryEntry {
  return {
    id: entry.id,
    expression: entry.k[0] ?? entry.r[0] ?? '',
    reading: entry.r[0] ?? '',
    senses: entry.s.map((sense) => ({ partsOfSpeech: sense.p, glosses: sense.g })),
  }
}
