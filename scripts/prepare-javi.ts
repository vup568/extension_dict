/**
 * Converts the FVDP/OVDP Japanese→Vietnamese dictionary (GPL — see README
 * licensing) into compact chunks for the extension's `javi` IndexedDB store.
 *
 * Source: the star_nhatviet StarDict data (Hồ Ngọc Đức's Free Vietnamese
 * Dictionary Project, mirrored by OVDP on SourceForge), fetched as a clean
 * TSV from github.com/catusf/tudien. Cached in data/.
 *
 * The data is a hybrid and needs real cleaning:
 *  - ~1.7k DIRECT entries:      学生 → "- học viên, học sinh, sinh viên."
 *  - ~170k PIVOT entries:       食べる → "- {たべる} - {eat} , ăn; …"
 *    (the full FVDP English→Vietnamese article for each English gloss —
 *    only the first sense fragment per {english} token is worth keeping)
 *  - ~99.5k records with NO Vietnamese at all (English-only residue) —
 *    dropped via a Vietnamese-diacritics test.
 *
 * MUST run after prepare-dict (amends public/dict/index.json in place).
 */
import { existsSync, readFileSync, readdirSync, rmSync, statSync, writeFileSync } from 'node:fs'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { chunkFileName, javiChunkFileName } from '../src/core/dictionary/packed-format'
import type { DictIndexFile, PackedEntry, PackedJavi } from '../src/core/dictionary/packed-format'

const ROOT = fileURLToPath(new URL('..', import.meta.url))
const DATA_DIR = join(ROOT, 'data')
const OUT_DIR = join(ROOT, 'public', 'dict')
const CHUNK_SIZE = 5000
const SOURCE_URL = 'https://raw.githubusercontent.com/catusf/tudien/master/dict/star_nhatviet.tab'
const SOURCE_FILE = 'star_nhatviet.tab'
/** Data-cleaning revision — bump to force clients to reimport. */
const PACK_REVISION = 'jv1'

/** Vietnamese-specific letters — the "is this actually Vietnamese?" test. */
const VI_CHARS = /[àáảãạăằắẳẵặâầấẩẫậèéẻẽẹêềếểễệìíỉĩịòóỏõọôồốổỗộơờớởỡợùúủũụưừứửữựỳýỷỹỵđ]/i
/** A {…} token whose content is purely kana → the entry's reading. */
const KANA_TOKEN = /^[ぁ-ゖァ-ヺー・\s]+$/
const MAX_GLOSS_LENGTH = 160
const MAX_SENSES = 4
/**
 * Vietnamese nominalizer openers ("sự chạy" = the running). For words JMdict
 * marks as pure verbs/adjectives these fragments are the WRONG part of
 * speech — an artifact of pivoting through English noun+verb homographs.
 */
const NOUNISH = /^(?:sự|việc|cuộc|cái|con|người|đồ|thức|chỗ|nơi|lúc|giấc|thói|vị)\s/

/**
 * Hand-curated glosses (our own wording) for core words where the FVDP
 * pivot picks an absurd English homograph sense (言う → "vải chéo" via the
 * fabric sense of "say") or has no Vietnamese at all (続く).
 */
const OVERRIDES: Record<string, string> = {
  言う: 'nói, bảo; gọi là',
  見る: 'nhìn, xem, thấy',
  売る: 'bán',
  思う: 'nghĩ, cho rằng; cảm thấy',
  行く: 'đi, đi đến',
  来る: 'đến, tới',
  帰る: 'về, trở về',
  走る: 'chạy',
  続く: 'tiếp tục, kéo dài, tiếp diễn',
  続ける: 'tiếp tục (làm gì)',
  分かる: 'hiểu, biết, nhận ra',
  出る: 'ra, ra khỏi; xuất hiện',
  入る: 'vào, đi vào',
  乗る: 'lên (xe, tàu), đi (phương tiện)',
  待つ: 'chờ, đợi',
  会う: 'gặp, gặp gỡ',
  使う: 'dùng, sử dụng',
  作る: 'làm, chế tạo, tạo ra',
  取る: 'lấy, cầm, nắm',
  持つ: 'cầm, mang, sở hữu',
  飲む: 'uống',
  泳ぐ: 'bơi, bơi lội',
  立つ: 'đứng, đứng dậy',
  休む: 'nghỉ, nghỉ ngơi',
  働く: 'làm việc',
  寝る: 'ngủ, đi ngủ',
  起きる: 'dậy, thức dậy; xảy ra',
  着る: 'mặc (quần áo)',
  脱ぐ: 'cởi (quần áo, giày)',
  洗う: 'rửa, giặt',
  開ける: 'mở, mở ra',
  閉める: 'đóng, đóng lại',
  始める: 'bắt đầu',
  終わる: 'kết thúc, xong',
  知る: 'biết',
  教える: 'dạy; chỉ cho, cho biết',
  習う: 'học (theo người dạy)',
  歌う: 'hát',
  話す: 'nói, nói chuyện',
  死ぬ: 'chết',
  生きる: 'sống',
  住む: 'sống, cư trú, ở',
  速い: 'nhanh, mau',
  早い: 'sớm; nhanh',
  寒い: 'lạnh, rét',
  暖かい: 'ấm áp, ấm; nồng hậu',
  温かい: 'ấm, nóng (đồ ăn); ấm áp (tình cảm)',
}

/**
 * Forms JMdict lists ONLY with verbal/adjectival parts of speech — for
 * these, nounish pivot fragments are dropped. Built from the packed JMdict
 * chunks that prepare-dict already wrote to public/dict.
 */
function loadNonNounForms(index: DictIndexFile): Set<string> {
  const verdict = new Map<string, boolean>() // form → still non-noun-only?
  for (let i = 0; i < index.chunkCount; i++) {
    const chunk = JSON.parse(readFileSync(join(OUT_DIR, chunkFileName(i)), 'utf8')) as PackedEntry[]
    for (const entry of chunk) {
      const nonNoun = entry.s.every((sense) =>
        sense.p.every((pos) => pos.startsWith('v') || pos.startsWith('adj')),
      )
      for (const form of [...entry.k, ...entry.r]) {
        verdict.set(form, (verdict.get(form) ?? true) && nonNoun)
      }
    }
  }
  const forms = new Set<string>()
  for (const [form, nonNoun] of verdict) if (nonNoun) forms.add(form)
  return forms
}

async function fetchSource(): Promise<string> {
  const cached = join(DATA_DIR, SOURCE_FILE)
  if (existsSync(cached)) return cached
  console.log(`Downloading ${SOURCE_URL} …`)
  const res = await fetch(SOURCE_URL)
  if (!res.ok) throw new Error(`Download failed: HTTP ${res.status}`)
  writeFileSync(cached, Buffer.from(await res.arrayBuffer()))
  console.log(`Cached source at ${cached}`)
  return cached
}

/** Split into sense fragments at `,`/`;` outside parentheses. */
function splitFragments(segment: string): string[] {
  const fragments: string[] = []
  let depth = 0
  let start = 0
  for (let i = 0; i < segment.length; i++) {
    const ch = segment[i]
    if (ch === '(') depth += 1
    else if (ch === ')') depth = Math.max(0, depth - 1)
    else if ((ch === ',' || ch === ';') && depth === 0) {
      fragments.push(segment.slice(start, i))
      start = i + 1
    }
  }
  fragments.push(segment.slice(start))
  return fragments
}

/** Drop leading parenthetical labels like "(từ Mỹ,nghĩa Mỹ)". */
function stripLabels(fragment: string): string {
  let text = fragment.trim()
  for (;;) {
    const stripped = text.replace(/^\s*\([^)]*\)\s*/, '')
    if (stripped === text) break
    text = stripped
  }
  return text.trim()
}

/**
 * First USABLE sense fragment of one pivot piece. FVDP English articles can
 * open with inflection markers ("/saw/,  seen /seen/, thấy, …") — skip
 * anything with slashes; skip diacritic-less fragments except in the very
 * first slot (short primary senses like "cao" carry no diacritics).
 *
 * For verb/adjective-only words (avoidNounish) a nominalized first
 * fragment means the article leads with the WRONG English homograph sense
 * (立つ → "stand" the noun) — the whole piece is untrustworthy then, so
 * reject it rather than fish deeper and surface different noun junk.
 */
function acceptableFragment(piece: string, avoidNounish: boolean): string | null {
  const fragments = splitFragments(piece)
  for (let i = 0; i < fragments.length; i++) {
    const raw = fragments[i]
    if (raw === undefined) continue
    const fragment = stripLabels(raw)
    if (fragment.length === 0 || fragment.includes('/')) continue
    if (!VI_CHARS.test(fragment) && i > 0) continue
    if (avoidNounish && NOUNISH.test(fragment)) return null
    return fragment
  }
  return null
}

/** Cleans one raw body into {reading, gloss}; null when nothing Vietnamese. */
function cleanBody(body: string, avoidNounish: boolean): { reading?: string; gloss: string } | null {
  // Quick reject: no Vietnamese anywhere outside {…} tokens.
  if (!VI_CHARS.test(body.replace(/\{[^}]*\}/g, ''))) return null

  let reading: string | undefined
  // Split on {…} tokens; even parts are surrounding text, odd parts tokens.
  const parts = body.split(/\{([^}]*)\}/)
  const first = parts[1]
  if (first !== undefined && KANA_TOKEN.test(first)) reading = first.trim()

  if (parts.length === 1) {
    // DIRECT entry: keep the whole Vietnamese text (minus markup noise:
    // "*tính từ -" POS headers, "@…" cross-refs, "=example +: dịch" tails,
    // ". - " sense-line joints).
    const gloss = body
      .split(/[=+]/)[0]
      ?.replace(/@\S+/g, '')
      .replace(/\*\s*[^-*]{1,25}-\s*/g, '; ')
      .replace(/\.\s+-\s+/g, '; ')
      .replace(/^[\s;-]+/, '')
      .replace(/\s+,/g, ',')
      .replace(/\s+-\s*;/g, ';')
      .replace(/;\s*;/g, '; ')
      .replace(/\s+/g, ' ')
      .replace(/[\s(;,-]+$/, '') // dangling "(" etc. left by the "=" cut
      .trim()
    if (gloss === undefined || !VI_CHARS.test(gloss)) return null
    return { gloss: capLength(gloss) }
  }

  // PIVOT entry: each surrounding-text piece holds the Vietnamese article
  // for the preceding {english} token — keep its first sense fragment only.
  // The Vietnamese test runs on the PIECE, not the fragment: short primary
  // senses like "cao"/"cam" carry no diacritics themselves.
  const senses: string[] = []
  for (let i = 2; i < parts.length; i += 2) {
    const piece = parts[i]
    if (piece === undefined || !VI_CHARS.test(piece)) continue
    const fragment = acceptableFragment(piece.replace(/^[\s,;-]+/, ''), avoidNounish)
    if (fragment === null) continue
    if (!senses.includes(fragment)) senses.push(fragment)
    if (senses.length >= MAX_SENSES) break
  }
  if (senses.length === 0) return null
  const result: { reading?: string; gloss: string } = { gloss: capLength(senses.join('; ')) }
  if (reading !== undefined) result.reading = reading
  return result
}

function capLength(text: string): string {
  if (text.length <= MAX_GLOSS_LENGTH) return text
  const cut = text.slice(0, MAX_GLOSS_LENGTH)
  const lastSep = Math.max(cut.lastIndexOf(';'), cut.lastIndexOf(','))
  return (lastSep > 40 ? cut.slice(0, lastSep) : cut).trimEnd() + '…'
}

async function main(): Promise<void> {
  const indexPath = join(OUT_DIR, 'index.json')
  if (!existsSync(indexPath)) {
    throw new Error('public/dict/index.json not found — run `npm run prepare-dict` first (or `npm run prepare-data`).')
  }

  const index = JSON.parse(readFileSync(indexPath, 'utf8')) as DictIndexFile
  console.log('Building part-of-speech map from packed JMdict chunks …')
  const nonNounForms = loadNonNounForms(index)
  console.log(`${nonNounForms.size} verb/adjective-only forms (nounish pivot fragments will be skipped)`)

  const sourcePath = await fetchSource()
  console.log(`Reading ${sourcePath} …`)
  const lines = readFileSync(sourcePath, 'utf8').split('\n')

  const records: PackedJavi[] = []
  const seen = new Set<string>()
  for (const line of lines) {
    const tab = line.indexOf('\t')
    if (tab <= 0) continue
    const headword = line.slice(0, tab).trim()
    if (headword.length === 0 || seen.has(headword)) continue
    const override = OVERRIDES[headword]
    const cleaned =
      override !== undefined
        ? { gloss: override }
        : cleanBody(line.slice(tab + 1), nonNounForms.has(headword))
    if (cleaned === null) continue
    seen.add(headword)
    const packed: PackedJavi = { h: headword, v: cleaned.gloss }
    if ('reading' in cleaned && cleaned.reading !== undefined && cleaned.reading !== headword) {
      packed.r = cleaned.reading
    }
    records.push(packed)
  }
  // Hand-curated words absent from the source entirely (e.g. 続く).
  for (const [headword, gloss] of Object.entries(OVERRIDES)) {
    if (!seen.has(headword)) records.push({ h: headword, v: gloss })
  }
  console.log(`Packed ${records.length} ja→vi records (from ${lines.length} source lines)`)

  for (const file of readdirSync(OUT_DIR)) {
    if (/^javi-\d+\.json$/.test(file)) rmSync(join(OUT_DIR, file))
  }
  const chunkCount = Math.ceil(records.length / CHUNK_SIZE)
  for (let i = 0; i < chunkCount; i++) {
    writeFileSync(join(OUT_DIR, javiChunkFileName(i)), JSON.stringify(records.slice(i * CHUNK_SIZE, (i + 1) * CHUNK_SIZE)))
  }

  index.javi = {
    javiVersion: `fvdp-ovdp/${PACK_REVISION}`,
    javiCount: records.length,
    javiChunkCount: chunkCount,
  }
  writeFileSync(indexPath, JSON.stringify(index))

  const totalBytes = readdirSync(OUT_DIR)
    .filter((f) => /^javi-\d+\.json$/.test(f))
    .reduce((sum, f) => sum + statSync(join(OUT_DIR, f)).size, 0)
  console.log(`Wrote ${chunkCount} javi chunks to public/dict (${(totalBytes / 1024 / 1024).toFixed(1)} MB) and updated index.json`)
  console.log('Run `npm run build` to package the ja→vi data into the extension.')
}

try {
  await main()
} catch (error) {
  console.error('[prepare-javi] failed:', error instanceof Error ? error.message : error)
  process.exitCode = 1
}
