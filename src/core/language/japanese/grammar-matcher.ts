/**
 * Matches the grammar-pattern rules against a kuromoji token stream.
 *
 * Scan model: at each token position every rule is tried; the LONGEST match
 * wins (ties → the rule listed first in the table), its tokens are consumed,
 * and scanning continues after it. That is what makes 〜たことがある beat
 * plain 〜た, and 〜ましょう beat 〜ます, without explicit priorities.
 * Each rule is reported at most once per sentence (first occurrence).
 */
import type { IpadicFeatures } from '@aiktb/kuromoji'
import { GRAMMAR_RULES } from './grammar-patterns'
import type { GrammarRule, TokenMatcher } from './grammar-patterns'
import type { GrammarPatternMatch } from '../../../shared/types'

function tokenMatches(token: IpadicFeatures, matcher: TokenMatcher): boolean {
  if (matcher.surface !== undefined && !matcher.surface.includes(token.surface_form)) return false
  if (matcher.base !== undefined && !matcher.base.includes(token.basic_form)) return false
  if (matcher.pos !== undefined && !matcher.pos.includes(token.pos)) return false
  if (matcher.detail !== undefined && !matcher.detail.includes(token.pos_detail_1)) return false
  if (matcher.conj !== undefined && !matcher.conj.includes(token.conjugated_form)) return false
  return true
}

/**
 * Try `rule` at token index `start`; returns the exclusive end index of the
 * match, or null. Optional matchers are skipped when they don't match.
 */
function matchAt(tokens: readonly IpadicFeatures[], start: number, rule: GrammarRule): number | null {
  let index = start
  for (const matcher of rule.matchers) {
    const token = tokens[index]
    if (token !== undefined && tokenMatches(token, matcher)) {
      index += 1
      continue
    }
    if (matcher.optional === true) continue
    return null
  }
  return index > start ? index : null
}

export function detectPatterns(tokens: readonly IpadicFeatures[]): GrammarPatternMatch[] {
  const found: GrammarPatternMatch[] = []
  const seen = new Set<string>()
  let i = 0
  while (i < tokens.length) {
    let bestRule: GrammarRule | null = null
    let bestEnd = i
    for (const rule of GRAMMAR_RULES) {
      const end = matchAt(tokens, i, rule)
      if (end !== null && end > bestEnd) {
        bestRule = rule
        bestEnd = end
      }
    }
    if (bestRule === null) {
      i += 1
      continue
    }
    if (!seen.has(bestRule.id)) {
      seen.add(bestRule.id)
      found.push({
        display: bestRule.display,
        level: bestRule.level,
        description: bestRule.description,
        surface: tokens
          .slice(i, bestEnd)
          .map((t) => t.surface_form)
          .join(''),
      })
    }
    i = bestEnd
  }
  return found
}
