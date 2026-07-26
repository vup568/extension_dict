/**
 * Sentence-level grammar analysis for the Ngữ pháp tab: JLPT pattern
 * detection (grammar-matcher) plus the per-unit conjugation breakdowns the
 * token strip already uses (japanese-pack's grammar-aware units).
 */
import { getTokenizer } from './tokenizer'
import { detectPatterns } from './grammar-matcher'
import { buildTokenUnits } from './japanese-pack'
import type { GrammarAnalysis, GrammarUnitBreakdown } from '../../../shared/types'

export async function analyzeGrammar(text: string): Promise<GrammarAnalysis> {
  const normalized = text.normalize('NFC').trim()
  if (normalized.length === 0) return { patterns: [], units: [] }
  const tokenizer = await getTokenizer()
  const tokens = tokenizer.tokenize(normalized)
  const patterns = detectPatterns(tokens)
  const units: GrammarUnitBreakdown[] = []
  for (const unit of await buildTokenUnits(tokens)) {
    if (unit.grammar !== null && unit.grammar.length >= 2) {
      units.push({ surface: unit.surface, parts: unit.grammar })
    }
  }
  return { patterns, units }
}
