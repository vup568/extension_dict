/** Lazy singleton for the compact JMdict expression index. */
import { EXPRESSION_INDEX_FILE } from '../../dictionary/packed-format'
import type { PackedExpressionIndex } from '../../dictionary/packed-format'
import { createExpressionIndex } from './expression-matcher'
import type { ExpressionIndex } from './expression-matcher'

let indexUrl = `/dict/${EXPRESSION_INDEX_FILE}`
let ready: ExpressionIndex | null = null
let pending: Promise<ExpressionIndex> | null = null

export function configureExpressionIndex(url: string): void {
  indexUrl = url
  ready = null
  pending = null
}

export function getExpressionIndex(): Promise<ExpressionIndex> {
  if (ready !== null) return Promise.resolve(ready)
  if (pending !== null) return pending
  const attempt = fetch(indexUrl)
    .then(async (response) => {
      if (!response.ok) throw new Error(`Failed to load ${indexUrl}: HTTP ${response.status}`)
      const packed = (await response.json()) as PackedExpressionIndex
      if (packed.version !== 1) throw new Error(`Unsupported expression index version ${String(packed.version)}`)
      return createExpressionIndex(packed)
    })
  pending = attempt
  attempt.then(
    (index) => {
      ready = index
      pending = null
    },
    () => {
      if (pending === attempt) pending = null
    },
  )
  return attempt
}
