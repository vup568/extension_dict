/**
 * Integration tests for fixed-expression detection. They use the real
 * Kuromoji tokenizer and a small, deterministic JMdict-shaped fixture at
 * the IndexedDB lookup boundary.
 */
import { strict as assert } from 'node:assert'
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { gzipSync } from 'node:zlib'
import type { IpadicFeatures, Tokenizer } from '@aiktb/kuromoji'
import { createExpressionIndex, detectExpressions } from '../src/core/language/japanese/expression-matcher'
import { detectPatterns } from '../src/core/language/japanese/grammar-matcher'
import { chunkFileName, javiChunkFileName } from '../src/core/dictionary/packed-format'
import type { DictIndexFile, PackedEntry, PackedExpressionIndex, PackedJavi } from '../src/core/dictionary/packed-format'
import type { DictionaryEntry } from '../src/shared/types'

const ROOT = fileURLToPath(new URL('..', import.meta.url))
const SRC_DICT = join(ROOT, 'public', 'kuromoji')

function ensureGzDict(): string {
  const fromEnv = process.env.KUROMOJI_GZ_DIR
  if (fromEnv !== undefined && existsSync(join(fromEnv, 'base.dat'))) return fromEnv
  const target = join(tmpdir(), 'jpdict-kuromoji-gz')
  if (!existsSync(join(target, 'base.dat'))) {
    if (!existsSync(SRC_DICT)) throw new Error('public/kuromoji missing — run `npm run prepare-tokenizer` first.')
    mkdirSync(target, { recursive: true })
    for (const file of readdirSync(SRC_DICT)) {
      if (file.endsWith('.dat')) {
        writeFileSync(join(target, file), gzipSync(readFileSync(join(SRC_DICT, file)), { level: 1 }))
      }
    }
  }
  return target
}

interface KuromojiModule {
  builder: (opts: { dicPath: string }) => { build: (cb: (err: unknown, tk: Tokenizer<IpadicFeatures>) => void) => void }
}

async function buildTokenizer(dicPath: string): Promise<Tokenizer<IpadicFeatures>> {
  const imported = (await import('@aiktb/kuromoji')) as unknown as KuromojiModule & { default?: KuromojiModule }
  const builder = (imported.default ?? imported).builder
  return new Promise((resolve, reject) => {
    builder({ dicPath }).build((err, tokenizer) => {
      if (err !== null && err !== undefined) reject(err instanceof Error ? err : new Error(String(err)))
      else resolve(tokenizer)
    })
  })
}

function entry(
  id: string,
  expression: string,
  reading: string,
  glosses: readonly string[],
  viGloss: string | null,
): DictionaryEntry {
  return {
    id,
    expression,
    reading,
    senses: [{ partsOfSpeech: ['exp', 'v1'], glosses }],
    viGloss,
  }
}

const care = entry(
  '1591990',
  '気をつける',
  'きをつける',
  ['to be careful', 'to pay attention', 'to take care'],
  'cẩn thận; chú ý; để tâm',
)
const longOverlap = entry('overlap-long', '猫の手を貸す', 'ねこのてをかす', ['to lend a hand'], null)
const shortOverlap = entry('overlap-short', '手を貸す', 'てをかす', ['to lend a hand'], null)

const packed: PackedExpressionIndex = {
  version: 1,
  maxFormLength: 6,
  entries: [
    { id: care.id, forms: ['気を付ける', '気をつける', 'きをつける'], common: true },
    { id: longOverlap.id, forms: ['猫の手を貸す'], common: false },
    { id: shortOverlap.id, forms: ['手を貸す'], common: false },
  ],
}
const expressionIndex = createExpressionIndex(packed)
const entriesById = new Map([care, longOverlap, shortOverlap].map((item) => [item.id, item]))
const lookup = async (term: string): Promise<readonly DictionaryEntry[]> => {
  const ids = packed.entries.filter((item) => item.forms.includes(term)).map((item) => item.id)
  return ids.flatMap((id) => {
    const found = entriesById.get(id)
    return found === undefined ? [] : [found]
  })
}

const tokenizer = await buildTokenizer(ensureGzDict())

async function match(text: string) {
  return detectExpressions(text, tokenizer.tokenize(text), expressionIndex, lookup)
}

for (const [surface, canonical, formDescription] of [
  ['気を付けて', '気を付ける', '付ける → 付けて'],
  ['気をつけて', '気をつける', 'つける → つけて'],
  ['気を付けました', '気を付ける', '付ける → 付けました'],
  ['気をつけないで', '気をつける', 'つける → つけないで'],
] as const) {
  const result = await match(surface)
  assert.equal(result.length, 1, `${surface} should produce exactly one expression`)
  assert.deepEqual(
    result[0],
    {
      entryId: '1591990',
      canonical,
      reading: 'きをつける',
      surface,
      start: 0,
      end: surface.length,
      meaningVi: 'cẩn thận; chú ý; để tâm',
      glossesEn: ['to be careful', 'to pay attention', 'to take care'],
      formDescription,
    },
    `${surface} should keep the displayed inflection while resolving its dictionary form`,
  )
}

const sentence = '東北地方から西日本では強い雨に気を付けて、土砂災害に警戒してください。'
const sentenceMatches = await match(sentence)
assert.equal(sentenceMatches.length, 1, 'an expression in the middle of a full sentence should be detected')
const expectedStart = sentence.indexOf('気を付けて')
assert.equal(sentenceMatches[0]?.start, expectedStart)
assert.equal(sentenceMatches[0]?.end, expectedStart + '気を付けて'.length)
assert.equal(sentenceMatches[0]?.surface, '気を付けて')

const repeated = await match('気を付けて、気を付けて。')
assert.equal(repeated.length, 2, 'repeated occurrences must not be deduplicated by entry id')
assert.deepEqual(repeated.map((item) => item.start), [0, 6])

const overlap = await match('猫の手を貸して。')
assert.deepEqual(overlap.map((item) => item.canonical), ['猫の手を貸す'], 'the longest containing expression should win')

for (const negative of ['電気をつけて', '元気をつけて', '今日は雨です。']) {
  assert.deepEqual(await match(negative), [], `${negative} must not produce an expression match`)
}

const fallback = overlap[0]
assert.equal(fallback?.meaningVi, null)
assert.deepEqual(fallback?.glossesEn, ['to lend a hand'], 'English JMdict glosses must survive when Vietnamese is absent')

// Generated-pack acceptance: prove the real preparation pipeline contains
// the expression, its spellings, English senses, and the hand-written VI gloss.
const publicDict = join(ROOT, 'public', 'dict')
const realPacked = JSON.parse(readFileSync(join(publicDict, 'expressions.json'), 'utf8')) as PackedExpressionIndex
const realRef = realPacked.entries.find((item) => item.forms.includes('気を付ける'))
assert.equal(realRef?.id, '1591990')
assert.deepEqual(realRef?.forms, ['気をつける', '気を付ける', 'きをつける'])

const dictIndex = JSON.parse(readFileSync(join(publicDict, 'index.json'), 'utf8')) as DictIndexFile
let packedEntry: PackedEntry | undefined
for (let i = 0; i < dictIndex.chunkCount && packedEntry === undefined; i++) {
  const chunk = JSON.parse(readFileSync(join(publicDict, chunkFileName(i)), 'utf8')) as PackedEntry[]
  packedEntry = chunk.find((item) => item.id === realRef?.id)
}
assert.ok(packedEntry, 'the indexed expression must resolve to a packaged JMdict entry')

let packedVi: PackedJavi | undefined
const javiChunkCount = dictIndex.javi?.javiChunkCount ?? 0
for (let i = 0; i < javiChunkCount && packedVi === undefined; i++) {
  const chunk = JSON.parse(readFileSync(join(publicDict, javiChunkFileName(i)), 'utf8')) as PackedJavi[]
  packedVi = chunk.find((item) => item.h === '気をつける')
}
assert.equal(packedVi?.v, 'cẩn thận; chú ý; để tâm')

const realEntry: DictionaryEntry = {
  id: packedEntry.id,
  expression: packedEntry.k[0] ?? packedEntry.r[0] ?? '',
  reading: packedEntry.r[0] ?? '',
  senses: packedEntry.s.map((sense) => ({ partsOfSpeech: sense.p, glosses: sense.g })),
  viGloss: packedVi?.v ?? null,
}
const realForms = new Set([...packedEntry.k, ...packedEntry.r])
const realLookup = async (term: string): Promise<readonly DictionaryEntry[]> =>
  realForms.has(term) ? [realEntry] : []
const realExpressionIndex = createExpressionIndex(realPacked)
const acceptanceTokens = tokenizer.tokenize(sentence)
const acceptance = await detectExpressions(
  sentence,
  acceptanceTokens,
  realExpressionIndex,
  realLookup,
)
assert.equal(acceptance[0]?.canonical, '気を付ける')
assert.equal(acceptance[0]?.meaningVi, 'cẩn thận; chú ý; để tâm')
assert.ok(
  detectPatterns(acceptanceTokens).some((pattern) => pattern.display === '〜て'),
  'the separate 〜て grammar-pattern card must remain present beside the expression',
)

const maxLengthSentence = '気を付けて。'.repeat(33).slice(0, 200)
const maxLengthTokens = tokenizer.tokenize(maxLengthSentence)
await detectExpressions(maxLengthSentence, maxLengthTokens, realExpressionIndex, realLookup)
const runs = 30
const benchStart = performance.now()
for (let i = 0; i < runs; i++) {
  await detectExpressions(maxLengthSentence, maxLengthTokens, realExpressionIndex, realLookup)
}
const averageMs = (performance.now() - benchStart) / runs
assert.ok(averageMs < 50, `200-character expression scan averaged ${averageMs.toFixed(2)}ms; expected <50ms`)

console.log(
  `OK — fixed-expression matcher handles inflections, offsets, overlap, negatives, gloss fallback, ` +
    `and the real packed data (${averageMs.toFixed(2)}ms average for 200 characters).`,
)
