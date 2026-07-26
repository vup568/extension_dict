/**
 * Converts a jmdict-simplified English JSON release into the compact chunked
 * format the extension imports into IndexedDB on first run.
 *
 * Usage:
 *   npm run prepare-dict              # full jmdict-eng (default)
 *   npm run prepare-dict -- --common  # common-words-only edition
 *   npm run prepare-dict -- --file=data/jmdict-eng-3.x.x.json
 *
 * The source JSON is cached in data/ (auto-downloaded from the GitHub
 * releases of scriptin/jmdict-simplified on first run); output goes to
 * public/dict/ so Vite packages it into the extension. Both directories are
 * gitignored — this script is how you regenerate them.
 */
import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from 'node:fs'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { unzipSync } from 'fflate'
import { DICT_FORMAT_VERSION, chunkFileName } from '../src/core/dictionary/packed-format'
import type { DictIndexFile, PackedEntry, PackedSense } from '../src/core/dictionary/packed-format'
import type { JmdictFile, JmdictWord } from './jmdict-types'

const ROOT = fileURLToPath(new URL('..', import.meta.url))
const DATA_DIR = join(ROOT, 'data')
const OUT_DIR = join(ROOT, 'public', 'dict')
const CHUNK_SIZE = 5000
const RELEASES_API = 'https://api.github.com/repos/scriptin/jmdict-simplified/releases/latest'

const args = process.argv.slice(2)
const wantCommon = args.includes('--common')
const explicitFile = args.find((a) => a.startsWith('--file='))?.slice('--file='.length)

interface ReleaseAsset {
  name: string
  size: number
  browser_download_url: string
}

function findCachedJson(): string | null {
  if (!existsSync(DATA_DIR)) return null
  const pattern = wantCommon ? /^jmdict-eng-common-\d.*\.json$/ : /^jmdict-eng-\d.*\.json$/
  const file = readdirSync(DATA_DIR).find((name) => pattern.test(name))
  return file === undefined ? null : join(DATA_DIR, file)
}

async function downloadLatest(): Promise<string> {
  console.log(`No cached ${wantCommon ? 'common' : 'full'} JSON in data/ — querying latest release …`)
  const infoRes = await fetch(RELEASES_API, {
    headers: { accept: 'application/vnd.github+json', 'user-agent': 'jp-dict-extension-prepare' },
  })
  if (!infoRes.ok) throw new Error(`GitHub API request failed: HTTP ${infoRes.status} ${infoRes.statusText}`)
  const release = (await infoRes.json()) as { tag_name: string; assets: ReleaseAsset[] }
  const pattern = wantCommon ? /^jmdict-eng-common-\d.*\.json\.zip$/ : /^jmdict-eng-\d.*\.json\.zip$/
  const asset = release.assets.find((a) => pattern.test(a.name))
  if (asset === undefined) {
    throw new Error(
      `No matching asset in release ${release.tag_name}.\nAssets: ${release.assets.map((a) => a.name).join(', ')}`,
    )
  }
  console.log(`Downloading ${asset.name} (${(asset.size / 1024 / 1024).toFixed(1)} MB) …`)
  const zipRes = await fetch(asset.browser_download_url)
  if (!zipRes.ok) throw new Error(`Download failed: HTTP ${zipRes.status}`)
  const files = unzipSync(new Uint8Array(await zipRes.arrayBuffer()))
  const jsonName = Object.keys(files).find((n) => n.endsWith('.json'))
  const jsonData = jsonName === undefined ? undefined : files[jsonName]
  if (jsonName === undefined || jsonData === undefined) throw new Error('Downloaded zip did not contain a .json file')
  mkdirSync(DATA_DIR, { recursive: true })
  const outPath = join(DATA_DIR, jsonName)
  writeFileSync(outPath, jsonData)
  console.log(`Cached source at ${outPath}`)
  return outPath
}

function packWord(word: JmdictWord): PackedEntry | null {
  const senses: PackedSense[] = []
  for (const sense of word.sense) {
    // MVP simplification: sense-level appliesToKanji/appliesToKana
    // restrictions are ignored — every sense is shown for its entry.
    const glosses: string[] = []
    for (const gloss of sense.gloss) {
      if (gloss.lang === 'eng' && typeof gloss.text === 'string') glosses.push(gloss.text)
    }
    if (glosses.length > 0) senses.push({ p: sense.partOfSpeech, g: glosses })
  }
  if (senses.length === 0) return null
  return {
    id: word.id,
    k: word.kanji.map((f) => f.text),
    r: word.kana.map((f) => f.text),
    c: word.kanji.some((f) => f.common) || word.kana.some((f) => f.common),
    s: senses,
  }
}

async function main(): Promise<void> {
  const jsonPath = explicitFile ?? findCachedJson() ?? (await downloadLatest())
  console.log(`Reading ${jsonPath} …`)
  const raw = JSON.parse(readFileSync(jsonPath, 'utf8')) as JmdictFile
  const edition = raw.commonOnly ? 'common' : 'full'
  console.log(`jmdict-simplified ${raw.version} (${raw.dictDate}, ${edition}): ${raw.words.length} words`)

  const entries: PackedEntry[] = []
  for (const word of raw.words) {
    const packed = packWord(word)
    if (packed !== null) entries.push(packed)
  }
  console.log(`Packed ${entries.length} entries (${raw.words.length - entries.length} skipped without English glosses)`)

  rmSync(OUT_DIR, { recursive: true, force: true })
  mkdirSync(OUT_DIR, { recursive: true })
  const chunkCount = Math.ceil(entries.length / CHUNK_SIZE)
  for (let i = 0; i < chunkCount; i++) {
    writeFileSync(join(OUT_DIR, chunkFileName(i)), JSON.stringify(entries.slice(i * CHUNK_SIZE, (i + 1) * CHUNK_SIZE)))
  }
  const indexFile: DictIndexFile = {
    formatVersion: DICT_FORMAT_VERSION,
    dictVersion: `${raw.version}/${raw.dictDate}/${edition}`,
    edition,
    entryCount: entries.length,
    chunkCount,
    tags: raw.tags,
  }
  writeFileSync(join(OUT_DIR, 'index.json'), JSON.stringify(indexFile))

  const totalBytes = readdirSync(OUT_DIR).reduce((sum, f) => sum + statSync(join(OUT_DIR, f)).size, 0)
  console.log(`Wrote ${chunkCount} chunks + index.json to public/dict (${(totalBytes / 1024 / 1024).toFixed(1)} MB)`)
  console.log('Run `npm run build` to package the dictionary into the extension.')
}

try {
  await main()
} catch (error) {
  console.error('[prepare-dict] failed:', error instanceof Error ? error.message : error)
  process.exitCode = 1
}
