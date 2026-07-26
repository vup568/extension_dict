/**
 * Japanese LanguagePack: ties the JMdict store and kuromoji together.
 *
 * Resolution order for a selection:
 *  1. Exact match of the whole (NFC-normalized) selection — catches plain
 *     dictionary forms and multi-word entries JMdict lists directly (idioms
 *     like 猫の手も借りたい).
 *  2. Tokenize with kuromoji, then longest-prefix over token boundaries:
 *     for each prefix (longest first) try the literal surface AND the
 *     variant with the last token replaced by its basic_form. That variant
 *     IS the deinflection step — kuromoji already knows the dictionary form
 *     of every conjugated token:
 *       食べました → 食べ|まし|た  → prefix 食べ, basic_form 食べる ✓
 *       高くない   → 高く|ない    → prefix 高く, basic_form 高い ✓
 *  3. Nothing matched → empty matches; the UI shows "No match found".
 *
 * The clickable token strip is built from grammar-aware units:
 *  - junk tokens (pure digits/punctuation) are dropped;
 *  - a free-standing verb/adjective absorbs its following auxiliaries
 *    (助動詞, connective て/で, non-independent verbs/adjectives) into one
 *    unit — なり+まし+た → なりました, looked up as なる, with a per-part
 *    grammar breakdown for the UI;
 *  - runs of nouns are merged when JMdict knows the compound (IPADIC splits
 *    熱中症 into 熱中+症). Restricting this merge to nouns is what prevents
 *    accidents like まし+た matching ました, a kana reading of 真下.
 */
import type { IpadicFeatures } from '@aiktb/kuromoji'
import type { LanguagePack, LookupResolution } from '../language-pack'
import type { GrammarPart, TokenInfo } from '../../../shared/types'
import { existsExact, lookupExact } from '../../dictionary/lookup'
import { containsJapanese } from './detect'
import { describeAuxiliary } from './grammar'
import { getTokenizer, tokenizeIfReady, warmUpTokenizer } from './tokenizer'

/** Max adjacent noun tokens considered when merging a dictionary compound. */
const MAX_MERGE_TOKENS = 4

export const japanesePack: LanguagePack = {
  id: 'ja',
  containsRelevantText: containsJapanese,
  resolve,
}

async function resolve(text: string): Promise<LookupResolution> {
  const normalized = text.normalize('NFC').trim()
  if (normalized.length === 0) {
    return { matches: [], tokens: null, grammar: null, deinflectionAvailable: true }
  }

  const exact = await lookupExact(normalized)
  if (exact.length > 0) {
    // Never make an exact hit wait for kuromoji's multi-second first init:
    // tokenize only if already warm, and start warming otherwise so the
    // next lookup gets a token list too.
    const raw = tokenizeIfReady(normalized)
    if (raw === null) warmUpTokenizer()
    const parsed = await parseTokens(raw)
    return { matches: exact, tokens: parsed.list, grammar: parsed.soleGrammar, deinflectionAvailable: true }
  }

  let tokens: IpadicFeatures[]
  try {
    tokens = (await getTokenizer()).tokenize(normalized)
  } catch (error) {
    console.warn('[jpdict] tokenizer unavailable, exact match only:', error)
    return { matches: [], tokens: null, grammar: null, deinflectionAvailable: false }
  }
  const parsed = await parseTokens(tokens)

  for (let end = tokens.length; end >= 1; end--) {
    for (const candidate of prefixCandidates(tokens.slice(0, end))) {
      const matches = await lookupExact(candidate)
      if (matches.length > 0) {
        return { matches, tokens: parsed.list, grammar: parsed.soleGrammar, deinflectionAvailable: true }
      }
    }
  }
  return { matches: [], tokens: parsed.list, grammar: parsed.soleGrammar, deinflectionAvailable: true }
}

/** Lookup candidates for a token prefix: literal surface, then deinflected. */
function prefixCandidates(prefix: IpadicFeatures[]): string[] {
  const surfaces = prefix.map((token) => token.surface_form)
  const candidates = [surfaces.join('')]
  const last = prefix[prefix.length - 1]
  if (last !== undefined && last.basic_form !== '*' && last.basic_form !== last.surface_form) {
    candidates.push([...surfaces.slice(0, -1), last.basic_form].join(''))
  }
  return candidates
}

// ---------------------------------------------------------------------------
// Grammar-aware token units

interface ParsedTokens {
  /** The strip to display — null when fewer than 2 units remain. */
  readonly list: readonly TokenInfo[] | null
  /** Breakdown when the whole input is a single conjugated grammar unit. */
  readonly soleGrammar: readonly GrammarPart[] | null
}

async function parseTokens(tokens: IpadicFeatures[] | null): Promise<ParsedTokens> {
  if (tokens === null) return { list: null, soleGrammar: null }
  const units = await buildUnits(tokens)
  const sole = units.length === 1 ? (units[0]?.grammar ?? null) : null
  return { list: units.length >= 2 ? units : null, soleGrammar: sole }
}

/** Junk = pure digits/punctuation/symbols — noise in the token strip. */
function isJunkToken(token: IpadicFeatures): boolean {
  return token.pos === '記号' || !containsJapanese(token.surface_form)
}

/** Free-standing verb/adjective — opens a conjugated grammar unit. */
function isContentStem(token: IpadicFeatures): boolean {
  return (token.pos === '動詞' || token.pos === '形容詞') && token.pos_detail_1 === '自立'
}

/** May attach to an open grammar unit (auxiliaries and friends). */
function isAttachable(token: IpadicFeatures): boolean {
  if (token.pos === '助動詞') return true
  if (
    token.pos === '助詞' &&
    token.pos_detail_1 === '接続助詞' &&
    ['て', 'で', 'ちゃ', 'じゃ'].includes(token.surface_form)
  ) {
    return true
  }
  return (token.pos === '動詞' || token.pos === '形容詞') && token.pos_detail_1 === '非自立'
}

function isNounStart(token: IpadicFeatures): boolean {
  return token.pos === '名詞' || token.pos === '接頭詞'
}

function baseForm(token: IpadicFeatures): string {
  return token.basic_form !== '*' && token.basic_form.length > 0 ? token.basic_form : token.surface_form
}

async function buildUnits(tokens: IpadicFeatures[]): Promise<TokenInfo[]> {
  const units: TokenInfo[] = []
  let i = 0
  while (i < tokens.length) {
    const token = tokens[i]
    if (token === undefined || isJunkToken(token)) {
      i += 1
      continue
    }
    if (isContentStem(token)) {
      const parts = [token]
      let j = i + 1
      while (j < tokens.length) {
        const next = tokens[j]
        if (next === undefined || !isAttachable(next)) break
        parts.push(next)
        j += 1
      }
      units.push(parts.length >= 2 ? grammarUnit(parts) : singleToken(token))
      i = j
      continue
    }
    if (isNounStart(token)) {
      const merged = await mergeNounCompound(tokens, i)
      if (merged !== null) {
        units.push({ surface: merged.surface, lookupTerm: merged.surface, grammar: null })
        i += merged.span
        continue
      }
    }
    units.push(singleToken(token))
    i += 1
  }
  return units
}

/** A verb/adjective stem plus its auxiliaries, with the UI breakdown. */
function grammarUnit(parts: IpadicFeatures[]): TokenInfo {
  const stem = parts[0]
  if (stem === undefined) throw new Error('grammarUnit requires a stem token')
  const lookupTerm = baseForm(stem)
  const grammar: GrammarPart[] = [
    { surface: stem.surface_form, description: `stem of ${lookupTerm}` },
    ...parts.slice(1).map((part) => ({
      surface: part.surface_form,
      description: describeAuxiliary(baseForm(part), part.pos),
    })),
  ]
  return { surface: parts.map((p) => p.surface_form).join(''), lookupTerm, grammar }
}

/**
 * Longest JMdict compound over a run of noun tokens starting at `start`
 * (an optional prefix token first, then nouns only), via key-only probes.
 */
async function mergeNounCompound(
  tokens: IpadicFeatures[],
  start: number,
): Promise<{ surface: string; span: number } | null> {
  let runEnd = start + 1
  while (runEnd < tokens.length && runEnd - start < MAX_MERGE_TOKENS) {
    const next = tokens[runEnd]
    if (next === undefined || next.pos !== '名詞' || isJunkToken(next)) break
    runEnd += 1
  }
  for (let j = runEnd; j > start + 1; j--) {
    const surface = tokens
      .slice(start, j)
      .map((t) => t.surface_form)
      .join('')
    if (await existsExact(surface)) return { surface, span: j - start }
  }
  return null
}

function singleToken(token: IpadicFeatures): TokenInfo {
  return { surface: token.surface_form, lookupTerm: baseForm(token), grammar: null }
}
