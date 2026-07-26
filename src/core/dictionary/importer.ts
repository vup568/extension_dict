/**
 * First-install import of the packaged dictionary chunks into IndexedDB.
 *
 * Resumable by design: the chunks-completed counter is committed in the SAME
 * IndexedDB transaction as each chunk's entries, so if the MV3 service worker
 * is killed mid-import (30s idle limit, browser shutdown …) the next start
 * continues at the first missing chunk instead of restarting.
 *
 * Deliberately chrome-API-free: URL resolution and progress side effects are
 * injected via configureImporter, keeping this module portable to Firefox
 * and unit-testable.
 */
import { getDb } from './db'
import type { DictionaryDatabase, StoredEntry } from './db'
import { DICT_FORMAT_VERSION, chunkFileName, javiChunkFileName, kanjiChunkFileName } from './packed-format'
import type { DictIndexFile, PackedEntry, PackedJavi, PackedKanji } from './packed-format'
import type { DictProgress, DictionaryStatus } from '../../shared/messages'

export interface ImporterOptions {
  /** Resolves a packaged dictionary file name to a fetchable URL. */
  fileUrl(name: string): string
  /** Called after every imported chunk. */
  onProgress?(progress: DictProgress): void
  /** Called when an import attempt fails. */
  onError?(reason: string): void
}

let options: ImporterOptions | null = null
let activeImport: Promise<void> | null = null
let lastError: string | null = null

export function configureImporter(opts: ImporterOptions): void {
  options = opts
}

/**
 * Kick off (or resume) the import. Idempotent and cheap to call often: it
 * no-ops while an import is running, and a completed import short-circuits
 * after one local metadata check.
 */
export function startImport(): void {
  if (activeImport !== null) return
  const opts = options
  if (opts === null) throw new Error('configureImporter() must be called before startImport()')
  lastError = null
  activeImport = runImport(opts)
    .catch((error: unknown) => {
      lastError = error instanceof Error ? error.message : String(error)
      opts.onError?.(lastError)
    })
    .finally(() => {
      activeImport = null
    })
}

export async function getDictionaryStatus(): Promise<DictionaryStatus> {
  const db = await getDb()
  const dict = await db.get('meta', 'dict')
  if (dict?.key === 'dict') {
    return { state: 'ready', entryCount: dict.entryCount, dictVersion: dict.dictVersion }
  }
  if (lastError !== null) return { state: 'unavailable', reason: lastError }
  const progress = await db.get('meta', 'importProgress')
  return {
    state: 'importing',
    progress:
      progress?.key === 'importProgress'
        ? { chunksDone: progress.chunksDone, chunkCount: progress.chunkCount }
        : null,
  }
}

async function runImport(opts: ImporterOptions): Promise<void> {
  const db = await getDb()
  const index = (await fetchPackagedJson(opts.fileUrl('index.json'))) as DictIndexFile
  if (index.formatVersion !== DICT_FORMAT_VERSION) {
    throw new Error(`Packaged dictionary has unsupported format version ${index.formatVersion}`)
  }

  // Resume points are computed up front (each also wipes stale data from a
  // different version) so the badge percentage reflects the combined work
  // actually left this run, entries and kanji together.
  const entryNext = await prepareEntryImport(db, index)
  const kanjiNext = await prepareKanjiImport(db, index)
  const javiNext = await prepareJaviImport(db, index)
  const kanjiInfo = index.kanji
  const javiInfo = index.javi
  const pendingTotal =
    (entryNext === null ? 0 : index.chunkCount - entryNext) +
    (kanjiNext === null || kanjiInfo === undefined ? 0 : kanjiInfo.kanjiChunkCount - kanjiNext) +
    (javiNext === null || javiInfo === undefined ? 0 : javiInfo.javiChunkCount - javiNext)
  let pendingDone = 0
  const reportChunk = (): void => {
    pendingDone += 1
    opts.onProgress?.({ chunksDone: pendingDone, chunkCount: pendingTotal })
  }

  if (entryNext !== null) {
    for (let i = entryNext; i < index.chunkCount; i++) {
      const chunk = (await fetchPackagedJson(opts.fileUrl(chunkFileName(i)))) as PackedEntry[]
      const tx = db.transaction(['entries', 'meta'], 'readwrite')
      const entryStore = tx.objectStore('entries')
      for (const packed of chunk) void entryStore.put(toStored(packed))
      void tx.objectStore('meta').put({
        key: 'importProgress',
        dictVersion: index.dictVersion,
        chunksDone: i + 1,
        chunkCount: index.chunkCount,
      })
      await tx.done
      reportChunk()
    }
    const done = db.transaction('meta', 'readwrite')
    void done.store.put({
      key: 'dict',
      dictVersion: index.dictVersion,
      entryCount: index.entryCount,
      importedAt: Date.now(),
    })
    void done.store.delete('importProgress')
    await done.done
  }

  if (kanjiNext !== null && kanjiInfo !== undefined) {
    for (let i = kanjiNext; i < kanjiInfo.kanjiChunkCount; i++) {
      const chunk = (await fetchPackagedJson(opts.fileUrl(kanjiChunkFileName(i)))) as PackedKanji[]
      const tx = db.transaction(['kanji', 'meta'], 'readwrite')
      const kanjiStore = tx.objectStore('kanji')
      for (const packed of chunk) void kanjiStore.put(packed)
      void tx.objectStore('meta').put({
        key: 'kanjiImportProgress',
        kanjiVersion: kanjiInfo.kanjiVersion,
        chunksDone: i + 1,
        chunkCount: kanjiInfo.kanjiChunkCount,
      })
      await tx.done
      reportChunk()
    }
    const done = db.transaction('meta', 'readwrite')
    void done.store.put({
      key: 'kanjiDict',
      kanjiVersion: kanjiInfo.kanjiVersion,
      kanjiCount: kanjiInfo.kanjiCount,
      importedAt: Date.now(),
    })
    void done.store.delete('kanjiImportProgress')
    await done.done
  }

  if (javiNext !== null && javiInfo !== undefined) {
    for (let i = javiNext; i < javiInfo.javiChunkCount; i++) {
      const chunk = (await fetchPackagedJson(opts.fileUrl(javiChunkFileName(i)))) as PackedJavi[]
      const tx = db.transaction(['javi', 'meta'], 'readwrite')
      const javiStore = tx.objectStore('javi')
      for (const packed of chunk) void javiStore.put(packed)
      void tx.objectStore('meta').put({
        key: 'javiImportProgress',
        javiVersion: javiInfo.javiVersion,
        chunksDone: i + 1,
        chunkCount: javiInfo.javiChunkCount,
      })
      await tx.done
      reportChunk()
    }
    const done = db.transaction('meta', 'readwrite')
    void done.store.put({
      key: 'javiDict',
      javiVersion: javiInfo.javiVersion,
      javiCount: javiInfo.javiCount,
      importedAt: Date.now(),
    })
    void done.store.delete('javiImportProgress')
    await done.done
  }
}

/**
 * Where to resume the entry import, wiping (partial) data left by another
 * dictionary version. Null when the packaged version is fully imported.
 */
async function prepareEntryImport(db: DictionaryDatabase, index: DictIndexFile): Promise<number | null> {
  const existing = await db.get('meta', 'dict')
  if (existing?.key === 'dict' && existing.dictVersion === index.dictVersion) return null
  const progress = await db.get('meta', 'importProgress')
  if (progress?.key === 'importProgress' && progress.dictVersion === index.dictVersion) {
    return progress.chunksDone
  }
  if (existing !== undefined || progress !== undefined) {
    // Data from a different dictionary version is (partially) present:
    // rebuild from scratch. Clearing meta also drops the kanji/javi
    // markers, so those imports naturally rerun against the fresh package.
    const tx = db.transaction(['entries', 'kanji', 'javi', 'meta'], 'readwrite')
    void tx.objectStore('entries').clear()
    void tx.objectStore('kanji').clear()
    void tx.objectStore('javi').clear()
    void tx.objectStore('meta').clear()
    await tx.done
  }
  return 0
}

/**
 * Same for the kanji chunks. Must run AFTER prepareEntryImport (whose
 * version-change wipe clears the kanji markers this reads). Null when up to
 * date or when the package contains no kanji data.
 */
async function prepareKanjiImport(db: DictionaryDatabase, index: DictIndexFile): Promise<number | null> {
  const info = index.kanji
  if (info === undefined) return null
  const existing = await db.get('meta', 'kanjiDict')
  if (existing?.key === 'kanjiDict' && existing.kanjiVersion === info.kanjiVersion) return null
  const progress = await db.get('meta', 'kanjiImportProgress')
  if (progress?.key === 'kanjiImportProgress' && progress.kanjiVersion === info.kanjiVersion) {
    return progress.chunksDone
  }
  if (existing !== undefined || progress !== undefined) {
    const tx = db.transaction(['kanji', 'meta'], 'readwrite')
    void tx.objectStore('kanji').clear()
    void tx.objectStore('meta').delete('kanjiDict')
    void tx.objectStore('meta').delete('kanjiImportProgress')
    await tx.done
  }
  return 0
}

/** Same for the ja→vi gloss chunks. */
async function prepareJaviImport(db: DictionaryDatabase, index: DictIndexFile): Promise<number | null> {
  const info = index.javi
  if (info === undefined) return null
  const existing = await db.get('meta', 'javiDict')
  if (existing?.key === 'javiDict' && existing.javiVersion === info.javiVersion) return null
  const progress = await db.get('meta', 'javiImportProgress')
  if (progress?.key === 'javiImportProgress' && progress.javiVersion === info.javiVersion) {
    return progress.chunksDone
  }
  if (existing !== undefined || progress !== undefined) {
    const tx = db.transaction(['javi', 'meta'], 'readwrite')
    void tx.objectStore('javi').clear()
    void tx.objectStore('meta').delete('javiDict')
    void tx.objectStore('meta').delete('javiImportProgress')
    await tx.done
  }
  return 0
}

function toStored(packed: PackedEntry): StoredEntry {
  return { ...packed, f: [...new Set([...packed.k, ...packed.r])] }
}

async function fetchPackagedJson(url: string): Promise<unknown> {
  let response: Response
  try {
    response = await fetch(url)
  } catch {
    throw new Error(
      'Packaged dictionary data is missing — run `npm run prepare-dict`, then `npm run build`, and reload the extension.',
    )
  }
  if (!response.ok) throw new Error(`Failed to load ${url}: HTTP ${response.status}`)
  return response.json()
}
