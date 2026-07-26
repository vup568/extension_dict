/**
 * Self-test for the grammar-pattern rules: every rule's `example` sentence
 * must trigger that rule through the real kuromoji tokenizer + matcher.
 *
 * Run: npm run test-grammar
 * Node's kuromoji loader expects GZIPPED dictionary files; point
 * KUROMOJI_GZ_DIR at a directory of gzip-compressed copies of
 * public/kuromoji/*.dat (kept outside the repo), else this script creates
 * one in the system temp directory on first run.
 */
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { gzipSync } from 'node:zlib'
import type { IpadicFeatures, Tokenizer } from '@aiktb/kuromoji'
import { GRAMMAR_RULES } from '../src/core/language/japanese/grammar-patterns'
import { detectPatterns } from '../src/core/language/japanese/grammar-matcher'

const ROOT = fileURLToPath(new URL('..', import.meta.url))
const SRC_DICT = join(ROOT, 'public', 'kuromoji')

function ensureGzDict(): string {
  const fromEnv = process.env.KUROMOJI_GZ_DIR
  if (fromEnv !== undefined && existsSync(join(fromEnv, 'base.dat'))) return fromEnv
  const target = join(tmpdir(), 'jpdict-kuromoji-gz')
  if (!existsSync(join(target, 'base.dat'))) {
    if (!existsSync(SRC_DICT)) {
      throw new Error('public/kuromoji missing — run `npm run prepare-tokenizer` first.')
    }
    console.log(`Gzipping IPADIC files into ${target} (one-time)…`)
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
    builder({ dicPath }).build((err, tk) => {
      if (err !== null && err !== undefined) reject(err instanceof Error ? err : new Error(String(err)))
      else resolve(tk)
    })
  })
}

const tokenizer = await buildTokenizer(ensureGzDict())

let failures = 0
for (const rule of GRAMMAR_RULES) {
  const tokens = tokenizer.tokenize(rule.example)
  const matches = detectPatterns(tokens)
  const hit = matches.some((m) => m.display === rule.display)
  if (!hit) {
    failures += 1
    console.error(`FAIL ${rule.id} (${rule.display}) — example「${rule.example}」detected: [${matches.map((m) => m.display).join(', ')}]`)
    for (const t of tokens) {
      console.error(
        `   ${t.surface_form} | ${t.pos},${t.pos_detail_1} | base=${t.basic_form} | ${t.conjugated_type}/${t.conjugated_form}`,
      )
    }
  }
}

const total = GRAMMAR_RULES.length
if (failures > 0) {
  console.error(`\n${failures}/${total} rules FAILED`)
  process.exitCode = 1
} else {
  console.log(`OK — all ${total} grammar rules detect their example sentence.`)
}
