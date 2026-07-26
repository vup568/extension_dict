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
import type { StoredEntry } from './db'
import { DICT_FORMAT_VERSION, chunkFileName } from './packed-format'
import type { DictIndexFile, PackedEntry } from './packed-format'
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

  const existing = await db.get('meta', 'dict')
  if (existing?.key === 'dict' && existing.dictVersion === index.dictVersion) return

  const progress = await db.get('meta', 'importProgress')
  let nextChunk = 0
  if (progress?.key === 'importProgress' && progress.dictVersion === index.dictVersion) {
    nextChunk = progress.chunksDone
  } else if (existing !== undefined || progress !== undefined) {
    // Data from a different dictionary version is (partially) present:
    // rebuild from scratch.
    const tx = db.transaction(['entries', 'meta'], 'readwrite')
    void tx.objectStore('entries').clear()
    void tx.objectStore('meta').clear()
    await tx.done
  }

  for (let i = nextChunk; i < index.chunkCount; i++) {
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
    opts.onProgress?.({ chunksDone: i + 1, chunkCount: index.chunkCount })
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
