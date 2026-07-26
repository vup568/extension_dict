/**
 * Lazy kuromoji tokenizer singleton for the service worker.
 *
 * Initialization loads ~96 MB of IPADIC data and takes a second or two, and
 * the MV3 worker can be killed after ~30s idle — so the tokenizer is built
 * on first use (never at worker start), rebuilt transparently after worker
 * restarts, and a failed init can be retried on the next call.
 *
 * PATH GOTCHA: kuromoji's DictionaryLoader collapses consecutive slashes in
 * the URLs it builds, which would corrupt an absolute chrome-extension://
 * URL. The configured dicPath must therefore be ROOT-RELATIVE ("/kuromoji");
 * inside the service worker, fetch() resolves it against the extension
 * origin, which is exactly what we want.
 */
import { builder } from '@aiktb/kuromoji'
import type { IpadicFeatures, Tokenizer } from '@aiktb/kuromoji'

let dicPath: string | null = null
let ready: Tokenizer<IpadicFeatures> | null = null
let pending: Promise<Tokenizer<IpadicFeatures>> | null = null

export function configureTokenizer(rootRelativeDicPath: string): void {
  dicPath = rootRelativeDicPath
}

export function getTokenizer(): Promise<Tokenizer<IpadicFeatures>> {
  if (ready !== null) return Promise.resolve(ready)
  if (pending !== null) return pending
  const path = dicPath
  if (path === null) return Promise.reject(new Error('configureTokenizer() must be called first'))

  const attempt = new Promise<Tokenizer<IpadicFeatures>>((resolve, reject) => {
    builder({ dicPath: path }).build((err, tokenizer) => {
      if (err !== null && err !== undefined) {
        reject(err instanceof Error ? err : new Error(String(err)))
      } else {
        resolve(tokenizer)
      }
    })
  })
  pending = attempt
  attempt.then(
    (tokenizer) => {
      ready = tokenizer
      pending = null
    },
    () => {
      // Clear so a later call can retry; the error reaches the caller.
      if (pending === attempt) pending = null
    },
  )
  return attempt
}

/** Non-blocking: tokens if the tokenizer is already warm, else null. */
export function tokenizeIfReady(text: string): IpadicFeatures[] | null {
  return ready === null ? null : ready.tokenize(text)
}

/** Fire-and-forget init so a later lookup finds the tokenizer warm. */
export function warmUpTokenizer(): void {
  void getTokenizer().catch(() => {
    /* reported on the blocking path in japanese-pack */
  })
}
