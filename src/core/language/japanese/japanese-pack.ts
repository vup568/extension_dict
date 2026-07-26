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
 * Whenever the selection splits into 2+ tokens, a cleaned-up token list is
 * returned for the clickable strip: junk tokens (pure digits/punctuation)
 * are dropped, and adjacent tokens that form a JMdict compound are merged
 * (IPADIC splits 熱中症 into 熱中+症; the dictionary knows better).
 */
import type { IpadicFeatures } from '@aiktb/kuromoji'
import type { LanguagePack, LookupResolution } from '../language-pack'
import type { TokenInfo } from '../../../shared/types'
import { existsExact, lookupExact } from '../../dictionary/lookup'
import { containsJapanese } from './detect'
import { getTokenizer, tokenizeIfReady, warmUpTokenizer } from './tokenizer'

/** Max adjacent tokens considered when merging into a dictionary compound. */
const MAX_MERGE_TOKENS = 4

export const japanesePack: LanguagePack = {
  id: 'ja',
  containsRelevantText: containsJapanese,
  resolve,
}

async function resolve(text: string): Promise<LookupResolution> {
  const normalized = text.normalize('NFC').trim()
  if (normalized.length === 0) return { matches: [], tokens: null, deinflectionAvailable: true }

  const exact = await lookupExact(normalized)
  if (exact.length > 0) {
    // Never make an exact hit wait for kuromoji's multi-second first init:
    // tokenize only if already warm, and start warming otherwise so the
    // next lookup gets a token list too.
    const tokens = tokenizeIfReady(normalized)
    if (tokens === null) warmUpTokenizer()
    return { matches: exact, tokens: await toTokenList(tokens), deinflectionAvailable: true }
  }

  let tokens: IpadicFeatures[]
  try {
    tokens = (await getTokenizer()).tokenize(normalized)
  } catch (error) {
    console.warn('[jpdict] tokenizer unavailable, exact match only:', error)
    return { matches: [], tokens: null, deinflectionAvailable: false }
  }

  for (let end = tokens.length; end >= 1; end--) {
    for (const candidate of prefixCandidates(tokens.slice(0, end))) {
      const matches = await lookupExact(candidate)
      if (matches.length > 0) {
        return { matches, tokens: await toTokenList(tokens), deinflectionAvailable: true }
      }
    }
  }
  return { matches: [], tokens: await toTokenList(tokens), deinflectionAvailable: true }
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

/** Junk = pure digits/punctuation/symbols — noise in the token strip. */
function isJunkToken(token: IpadicFeatures): boolean {
  return token.pos === '記号' || !containsJapanese(token.surface_form)
}

/**
 * Build the clickable token list: drop junk, then greedily merge adjacent
 * tokens whose joined surface exists in JMdict (longest match wins, capped
 * at MAX_MERGE_TOKENS, never across a junk boundary). Existence checks use
 * the key-only existsExact probe, so a sentence costs a few dozen index
 * probes at ~0.1ms each.
 */
async function toTokenList(tokens: IpadicFeatures[] | null): Promise<readonly TokenInfo[] | null> {
  if (tokens === null || tokens.length < 2) return null
  const out: TokenInfo[] = []
  let i = 0
  while (i < tokens.length) {
    const token = tokens[i]
    if (token === undefined || isJunkToken(token)) {
      i += 1
      continue
    }
    // Longest junk-free run starting at i, capped at the merge window.
    let runEnd = i + 1
    while (runEnd < tokens.length && runEnd - i < MAX_MERGE_TOKENS) {
      const next = tokens[runEnd]
      if (next === undefined || isJunkToken(next)) break
      runEnd += 1
    }
    let merged: { surface: string; span: number } | null = null
    for (let j = runEnd; j > i + 1; j--) {
      const surface = tokens
        .slice(i, j)
        .map((t) => t.surface_form)
        .join('')
      if (await existsExact(surface)) {
        merged = { surface, span: j - i }
        break
      }
    }
    if (merged !== null) {
      out.push({ surface: merged.surface, lookupTerm: merged.surface })
      i += merged.span
    } else {
      out.push(toSingleToken(token))
      i += 1
    }
  }
  return out.length >= 2 ? out : null
}

function toSingleToken(token: IpadicFeatures): TokenInfo {
  return {
    surface: token.surface_form,
    lookupTerm:
      token.basic_form !== '*' && token.basic_form.length > 0 ? token.basic_form : token.surface_form,
  }
}
