/**
 * Sentence-level grammar analysis for the Ngữ pháp tab: JLPT pattern
 * detection (grammar-matcher) plus the per-unit conjugation breakdowns the
 * token strip already uses (japanese-pack's grammar-aware units).
 */
import { getTokenizer } from './tokenizer'
import { detectPatterns } from './grammar-matcher'
import { buildTokenUnits } from './japanese-pack'
import { getExpressionIndex } from './expression-index'
import { detectExpressions } from './expression-matcher'
import { lookupExact } from '../../dictionary/lookup'
import type { ExpressionMatch, GrammarAnalysis, GrammarUnitBreakdown } from '../../../shared/types'

export async function analyzeGrammar(text: string): Promise<GrammarAnalysis> {
  if (text.trim().length === 0) return { expressions: [], patterns: [], units: [] }
  const tokenizer = await getTokenizer()
  const tokens = tokenizer.tokenize(text)
  const patterns = detectPatterns(tokens)
  let expressions: ExpressionMatch[] = []
  try {
    expressions = await detectExpressions(text, tokens, await getExpressionIndex(), lookupExact)
  } catch (error) {
    // Grammar rules and conjugation remain useful if the optional lazy index
    // is temporarily unavailable (for example, during an extension reload).
    console.warn('[jpdict] expression index unavailable:', error)
  }
  const units: GrammarUnitBreakdown[] = []
  for (const unit of await buildTokenUnits(tokens)) {
    if (unit.grammar !== null && unit.grammar.length >= 2) {
      units.push({ surface: unit.surface, parts: unit.grammar })
    }
  }
  return { expressions, patterns, units }
}
