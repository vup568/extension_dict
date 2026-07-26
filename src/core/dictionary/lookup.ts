/**
 * Dictionary lookup queries against IndexedDB.
 */
import { getDb } from './db'
import type { StoredEntry } from './db'
import type { DictionaryEntry } from '../../shared/types'

/** Cap on entries returned for a single term. */
const MAX_MATCHES = 8

/**
 * Cheap existence probe (used by dictionary-guided token merging): fetches
 * only an index key, never a full entry record.
 */
export async function existsExact(term: string): Promise<boolean> {
  const normalized = term.normalize('NFC').trim()
  if (normalized.length === 0) return false
  const db = await getDb()
  return (await db.getKeyFromIndex('entries', 'forms', normalized)) !== undefined
}

/**
 * Exact-match lookup: the term must equal one of an entry's kanji or kana
 * forms. Entries marked common in JMdict rank first. Each result is
 * enriched with the FVDP ja→vi gloss when the `javi` store has its
 * headword (reading-checked to avoid homograph mixups).
 */
export async function lookupExact(term: string): Promise<DictionaryEntry[]> {
  const normalized = term.normalize('NFC').trim()
  if (normalized.length === 0) return []
  const db = await getDb()
  const stored = await db.getAllFromIndex('entries', 'forms', normalized)
  const top = stored.sort((a, b) => Number(b.c) - Number(a.c)).slice(0, MAX_MATCHES)
  return Promise.all(top.map((entry) => toDictionaryEntry(db, entry)))
}

async function toDictionaryEntry(
  db: Awaited<ReturnType<typeof getDb>>,
  entry: StoredEntry,
): Promise<DictionaryEntry> {
  const expression = entry.k[0] ?? entry.r[0] ?? ''
  const reading = entry.r[0] ?? ''
  return {
    id: entry.id,
    expression,
    reading,
    senses: entry.s.map((sense) => ({ partsOfSpeech: sense.p, glosses: sense.g })),
    viGloss: await viGlossFor(db, expression, reading),
  }
}

/**
 * Vietnamese gloss for an entry: the kanji-form record first (its stated
 * reading must agree when both sides have one), falling back to the kana
 * record only for kana-only words — kana pivot records merge homographs
 * and would gloss the wrong 学生/学制 otherwise.
 */
async function viGlossFor(
  db: Awaited<ReturnType<typeof getDb>>,
  expression: string,
  reading: string,
): Promise<string | null> {
  const record = await db.get('javi', expression)
  if (record !== undefined) {
    const readingMatches = record.r === undefined || reading.length === 0 || record.r === reading
    if (readingMatches) return record.v
  }
  return null
}
