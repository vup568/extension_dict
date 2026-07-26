/**
 * Online sentence translation via the key-less Google Translate web endpoint
 * (client=gtx) — the same endpoint popular dictionary extensions use.
 *
 * This is NOT an official API: it needs no key and costs nothing, but it is
 * undocumented, rate-limited per IP, and Google may change or block it at
 * any time — so every failure path must degrade into a readable error, and
 * the on-device engine remains the preferred path once its pack exists.
 *
 * Runs in the service worker: the fetch needs the extension's
 * host_permission grant to bypass CORS (page contexts cannot call it).
 */
import type { TargetLang } from '../../shared/types'

const ENDPOINT = 'https://translate.googleapis.com/translate_a/single'
const TIMEOUT_MS = 10_000

export async function cloudTranslate(text: string, target: TargetLang): Promise<string> {
  const url = `${ENDPOINT}?client=gtx&sl=ja&tl=${target}&dt=t`
  let response: Response
  try {
    // POST body (not URL query): multi-sentence CJK selections URL-encode to
    // many KB and would overflow URL limits on a GET.
    response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8' },
      body: `q=${encodeURIComponent(text)}`,
      signal: AbortSignal.timeout(TIMEOUT_MS),
    })
  } catch {
    throw new Error(
      navigator.onLine
        ? 'Online translation is unreachable right now — try again shortly.'
        : 'No internet connection — download the offline pack to translate offline.',
    )
  }
  if (response.status === 429) {
    throw new Error('Online translation is rate-limited right now — try again in a minute.')
  }
  if (!response.ok) throw new Error(`Online translation failed (HTTP ${response.status})`)

  let data: unknown
  try {
    data = await response.json()
  } catch {
    throw new Error('Online translation returned an unexpected response.')
  }
  const translated = extractSegments(data)
  if (translated === null) throw new Error('Online translation returned an unexpected response.')
  return translated
}

/** Response shape: [[["translated","original",…], …], …] — join segment[0]s. */
function extractSegments(data: unknown): string | null {
  if (!Array.isArray(data) || !Array.isArray(data[0])) return null
  const parts: string[] = []
  for (const segment of data[0] as unknown[]) {
    if (Array.isArray(segment) && typeof segment[0] === 'string') parts.push(segment[0])
  }
  return parts.length > 0 ? parts.join('') : null
}
