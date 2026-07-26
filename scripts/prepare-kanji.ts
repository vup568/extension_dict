/**
 * Converts a jmdict-simplified kanjidic2-en JSON release into compact chunks
 * for the extension's `kanji` IndexedDB store (the Hán tự tab: Hán Việt +
 * on/kun readings, meanings, strokes, JLPT).
 *
 * Usage:
 *   npm run prepare-kanji                       # latest release (cached)
 *   npm run prepare-kanji -- --file=data/x.json # local kanjidic2 JSON
 *
 * MUST run after prepare-dict: it amends public/dict/index.json in place
 * (and prepare-dict wipes public/dict entirely). `npm run prepare-data`
 * runs both in the right order.
 */
import { existsSync, mkdirSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from 'node:fs'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { unzipSync } from 'fflate'
import { kanjiChunkFileName } from '../src/core/dictionary/packed-format'
import type { DictIndexFile, PackedKanji } from '../src/core/dictionary/packed-format'
import type { Kanjidic2Character, Kanjidic2File } from './kanjidic-types'

const ROOT = fileURLToPath(new URL('..', import.meta.url))
const DATA_DIR = join(ROOT, 'data')
const OUT_DIR = join(ROOT, 'public', 'dict')
const CHUNK_SIZE = 2000
const RELEASES_API = 'https://api.github.com/repos/scriptin/jmdict-simplified/releases/latest'

/** Old pre-2010 JLPT scale → modern N-scale (the old scale had no N3). */
const JLPT_OLD_TO_N: Record<number, number> = { 4: 5, 3: 4, 2: 2, 1: 1 }

const explicitFile = process.argv
  .slice(2)
  .find((a) => a.startsWith('--file='))
  ?.slice('--file='.length)

interface ReleaseAsset {
  name: string
  size: number
  browser_download_url: string
}

function findCachedJson(): string | null {
  if (!existsSync(DATA_DIR)) return null
  const file = readdirSync(DATA_DIR).find((name) => /^kanjidic2-en-\d.*\.json$/.test(name))
  return file === undefined ? null : join(DATA_DIR, file)
}

async function downloadLatest(): Promise<string> {
  console.log('No cached kanjidic2 JSON in data/ — querying latest release …')
  const infoRes = await fetch(RELEASES_API, {
    headers: { accept: 'application/vnd.github+json', 'user-agent': 'jp-dict-extension-prepare' },
  })
  if (!infoRes.ok) throw new Error(`GitHub API request failed: HTTP ${infoRes.status} ${infoRes.statusText}`)
  const release = (await infoRes.json()) as { tag_name: string; assets: ReleaseAsset[] }
  const asset = release.assets.find((a) => /^kanjidic2-en-\d.*\.json\.zip$/.test(a.name))
  if (asset === undefined) {
    throw new Error(
      `No kanjidic2-en asset in release ${release.tag_name}.\nAssets: ${release.assets.map((a) => a.name).join(', ')}`,
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

function packCharacter(char: Kanjidic2Character): PackedKanji | null {
  const hanViet: string[] = []
  const on: string[] = []
  const kun: string[] = []
  const meanings: string[] = []
  for (const group of char.readingMeaning?.groups ?? []) {
    for (const reading of group.readings) {
      if (reading.type === 'vietnam') hanViet.push(reading.value)
      else if (reading.type === 'ja_on') on.push(reading.value)
      else if (reading.type === 'ja_kun') kun.push(reading.value)
    }
    for (const meaning of group.meanings) {
      if (meaning.lang === 'en') meanings.push(meaning.value)
    }
  }
  // A character with nothing to display would render an empty card.
  if (hanViet.length === 0 && on.length === 0 && kun.length === 0 && meanings.length === 0) return null

  const packed: PackedKanji = { l: char.literal }
  if (hanViet.length > 0) packed.v = hanViet
  if (on.length > 0) packed.o = on
  if (kun.length > 0) packed.u = kun
  if (meanings.length > 0) packed.m = meanings
  const strokes = char.misc.strokeCounts[0]
  if (strokes !== undefined) packed.s = strokes
  if (char.misc.grade !== null) packed.g = char.misc.grade
  const jlpt = char.misc.jlptLevel === null ? undefined : JLPT_OLD_TO_N[char.misc.jlptLevel]
  if (jlpt !== undefined) packed.j = jlpt
  if (char.misc.frequency !== null) packed.f = char.misc.frequency
  return packed
}

async function main(): Promise<void> {
  const indexPath = join(OUT_DIR, 'index.json')
  if (!existsSync(indexPath)) {
    throw new Error('public/dict/index.json not found — run `npm run prepare-dict` first (or `npm run prepare-data`).')
  }

  const jsonPath = explicitFile ?? findCachedJson() ?? (await downloadLatest())
  console.log(`Reading ${jsonPath} …`)
  const raw = JSON.parse(readFileSync(jsonPath, 'utf8')) as Kanjidic2File
  console.log(`kanjidic2 ${raw.version} (${raw.dictDate}): ${raw.characters.length} characters`)

  const packed: PackedKanji[] = []
  let withHanViet = 0
  for (const char of raw.characters) {
    const record = packCharacter(char)
    if (record === null) continue
    packed.push(record)
    if (record.v !== undefined) withHanViet += 1
  }
  console.log(`Packed ${packed.length} kanji (${withHanViet} with Hán Việt readings)`)

  // Replace any kanji chunks from a previous run (prepare-dict wipes the
  // whole directory, but a prepare-kanji re-run must clean up after itself).
  for (const file of readdirSync(OUT_DIR)) {
    if (/^kanji-\d+\.json$/.test(file)) rmSync(join(OUT_DIR, file))
  }
  const chunkCount = Math.ceil(packed.length / CHUNK_SIZE)
  for (let i = 0; i < chunkCount; i++) {
    writeFileSync(join(OUT_DIR, kanjiChunkFileName(i)), JSON.stringify(packed.slice(i * CHUNK_SIZE, (i + 1) * CHUNK_SIZE)))
  }

  const index = JSON.parse(readFileSync(indexPath, 'utf8')) as DictIndexFile
  index.kanji = {
    kanjiVersion: `${raw.version}/${raw.dictDate}`,
    kanjiCount: packed.length,
    kanjiChunkCount: chunkCount,
  }
  writeFileSync(indexPath, JSON.stringify(index))

  const totalBytes = readdirSync(OUT_DIR)
    .filter((f) => /^kanji-\d+\.json$/.test(f))
    .reduce((sum, f) => sum + statSync(join(OUT_DIR, f)).size, 0)
  console.log(`Wrote ${chunkCount} kanji chunks to public/dict (${(totalBytes / 1024 / 1024).toFixed(1)} MB) and updated index.json`)
  console.log('Run `npm run build` to package the kanji data into the extension.')
}

try {
  await main()
} catch (error) {
  console.error('[prepare-kanji] failed:', error instanceof Error ? error.message : error)
  process.exitCode = 1
}
