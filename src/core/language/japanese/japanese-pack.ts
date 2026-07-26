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
 * Whenever the selection splits into 2+ tokens, the full token list is
 * returned so the UI can offer per-token lookup.
 */
import type { IpadicFeatures } from '@aiktb/kuromoji'
import type { LanguagePack, LookupResolution } from '../language-pack'
import type { TokenInfo } from '../../../shared/types'
import { lookupExact } from '../../dictionary/lookup'
import { containsJapanese } from './detect'
import { getTokenizer, tokenizeIfReady, warmUpTokenizer } from './tokenizer'

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
    return { matches: exact, tokens: toTokenList(tokens), deinflectionAvailable: true }
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
        return { matches, tokens: toTokenList(tokens), deinflectionAvailable: true }
      }
    }
  }
  return { matches: [], tokens: toTokenList(tokens), deinflectionAvailable: true }
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

function toTokenList(tokens: IpadicFeatures[] | null): readonly TokenInfo[] | null {
  if (tokens === null || tokens.length < 2) return null
  return tokens.map((token) => ({
    surface: token.surface_form,
    lookupTerm:
      token.basic_form !== '*' && token.basic_form.length > 0 ? token.basic_form : token.surface_form,
  }))
}
