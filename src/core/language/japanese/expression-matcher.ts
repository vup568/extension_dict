import type { IpadicFeatures } from '@aiktb/kuromoji'
import type { PackedExpressionIndex } from '../../dictionary/packed-format'
import type { DictionaryEntry, ExpressionMatch } from '../../../shared/types'

export interface ExpressionIndex {
  readonly maxFormLength: number
  readonly byForm: ReadonlyMap<string, readonly { readonly entryId: string; readonly common: boolean }[]>
}

export type ExpressionLookup = (term: string) => Promise<readonly DictionaryEntry[]>

interface TokenOffset {
  readonly start: number
  readonly end: number
}

interface RawMatch extends ExpressionMatch {
  readonly common: boolean
}

export function createExpressionIndex(packed: PackedExpressionIndex): ExpressionIndex {
  const byForm = new Map<string, { entryId: string; common: boolean }[]>()
  for (const item of packed.entries) {
    for (const form of item.forms) {
      const refs = byForm.get(form) ?? []
      refs.push({ entryId: item.id, common: item.common })
      byForm.set(form, refs)
    }
  }
  return { maxFormLength: packed.maxFormLength, byForm }
}

function isClauseBoundary(token: IpadicFeatures): boolean {
  return token.pos === '記号'
}

function isContentStem(token: IpadicFeatures): boolean {
  return (token.pos === '動詞' || token.pos === '形容詞') && token.pos_detail_1 === '自立'
}

function isAttachable(token: IpadicFeatures): boolean {
  if (token.pos === '助動詞') return true
  if (
    token.pos === '助詞' &&
    token.pos_detail_1 === '接続助詞' &&
    ['て', 'で', 'ちゃ', 'じゃ'].includes(token.surface_form)
  ) {
    return true
  }
  return (
    (token.pos === '動詞' || token.pos === '形容詞') &&
    (token.pos_detail_1 === '非自立' || token.pos_detail_1 === '接尾')
  )
}

function baseForm(token: IpadicFeatures): string {
  return token.basic_form !== '*' && token.basic_form.length > 0 ? token.basic_form : token.surface_form
}

/** Align token surfaces back to the original UTF-16 string. */
function locateTokens(text: string, tokens: readonly IpadicFeatures[]): TokenOffset[] {
  const offsets: TokenOffset[] = []
  let cursor = 0
  for (const token of tokens) {
    let start = text.indexOf(token.surface_form, cursor)
    if (start < 0) start = Math.max(0, token.word_position - 1)
    const end = start + token.surface_form.length
    offsets.push({ start, end })
    cursor = end
  }
  return offsets
}

function englishGlosses(entry: DictionaryEntry): string[] {
  const glosses: string[] = []
  for (const sense of entry.senses) {
    if (!sense.partsOfSpeech.includes('exp')) continue
    for (const gloss of sense.glosses) if (!glosses.includes(gloss)) glosses.push(gloss)
  }
  return glosses
}

function displayEnd(tokens: readonly IpadicFeatures[], coreEnd: number): number {
  let end = coreEnd + 1
  while (end < tokens.length) {
    const token = tokens[end]
    if (token === undefined || isClauseBoundary(token) || !isAttachable(token)) break
    end += 1
  }
  return end
}

export async function detectExpressions(
  text: string,
  tokens: readonly IpadicFeatures[],
  index: ExpressionIndex,
  lookup: ExpressionLookup,
): Promise<ExpressionMatch[]> {
  if (text.length === 0 || tokens.length === 0 || index.byForm.size === 0) return []

  const offsets = locateTokens(text, tokens)
  const lookupCache = new Map<string, Promise<readonly DictionaryEntry[]>>()
  const raw: RawMatch[] = []
  const seen = new Set<string>()

  for (let startToken = 0; startToken < tokens.length; startToken++) {
    const first = tokens[startToken]
    if (first === undefined || isClauseBoundary(first)) continue
    let literal = ''

    for (let coreEnd = startToken; coreEnd < tokens.length; coreEnd++) {
      const token = tokens[coreEnd]
      if (token === undefined || isClauseBoundary(token)) break
      literal += token.surface_form

      const candidates: { canonical: string; inflected: boolean }[] = [{ canonical: literal, inflected: false }]
      if (isContentStem(token)) {
        const dictionaryForm = baseForm(token)
        if (dictionaryForm !== token.surface_form) {
          candidates.push({
            canonical: literal.slice(0, -token.surface_form.length) + dictionaryForm,
            inflected: true,
          })
        }
      }

      for (const candidate of candidates) {
        if (candidate.canonical.length > index.maxFormLength || coreEnd - startToken + 1 < 2) continue
        const refs = index.byForm.get(candidate.canonical)
        if (refs === undefined || refs.length === 0) continue

        const exclusiveEnd = candidate.inflected ? displayEnd(tokens, coreEnd) : coreEnd + 1
        const startOffset = offsets[startToken]?.start
        const endOffset = offsets[exclusiveEnd - 1]?.end
        if (startOffset === undefined || endOffset === undefined) continue

        const entriesPromise = lookupCache.get(candidate.canonical) ?? lookup(candidate.canonical)
        lookupCache.set(candidate.canonical, entriesPromise)
        const entries = await entriesPromise
        const refsById = new Map(refs.map((ref) => [ref.entryId, ref]))
        for (const entry of entries) {
          const ref = refsById.get(entry.id)
          if (ref === undefined) continue
          const key = `${entry.id}:${startOffset}:${endOffset}`
          if (seen.has(key)) continue
          seen.add(key)
          const inflectedSurface = tokens
            .slice(coreEnd, exclusiveEnd)
            .map((part) => part.surface_form)
            .join('')
          raw.push({
            entryId: entry.id,
            canonical: candidate.canonical,
            reading: entry.reading,
            surface: text.slice(startOffset, endOffset),
            start: startOffset,
            end: endOffset,
            meaningVi: entry.viGloss,
            glossesEn: englishGlosses(entry),
            formDescription: candidate.inflected ? `${baseForm(token)} → ${inflectedSurface}` : null,
            common: ref.common,
          })
        }
      }

      if (literal.length > index.maxFormLength) break
    }
  }

  // Prefer the longest surface interval. Common JMdict entries break exact
  // span ties; a contained candidate is omitted, but another occurrence at
  // different offsets remains independent.
  raw.sort(
    (a, b) =>
      b.end - b.start - (a.end - a.start) ||
      Number(b.common) - Number(a.common) ||
      a.start - b.start ||
      a.entryId.localeCompare(b.entryId),
  )
  const accepted: RawMatch[] = []
  for (const candidate of raw) {
    if (accepted.some((item) => item.start <= candidate.start && item.end >= candidate.end)) continue
    accepted.push(candidate)
  }
  accepted.sort((a, b) => a.start - b.start || b.end - b.start - (a.end - a.start))
  return accepted.map(({ common: _common, ...match }) => match)
}
