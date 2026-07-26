/**
 * Typed messaging contracts between content scripts and the background
 * service worker. Every message is a member of a discriminated union so the
 * router can narrow on `type` exhaustively.
 */
import type { DictionaryEntry, TokenInfo } from './types'

// ---------------------------------------------------------------------------
// Requests (content script → service worker)

export interface LookupRequest {
  readonly type: 'lookup'
  readonly text: string
}

export interface DictStatusRequest {
  readonly type: 'dict-status'
}

export type BackgroundRequest = LookupRequest | DictStatusRequest

// ---------------------------------------------------------------------------
// Responses (service worker → content script)

export interface DictProgress {
  readonly chunksDone: number
  readonly chunkCount: number
}

export type DictionaryStatus =
  | { readonly state: 'ready'; readonly entryCount: number; readonly dictVersion: string }
  /** progress is null when importing has been requested but no chunk landed yet. */
  | { readonly state: 'importing'; readonly progress: DictProgress | null }
  | { readonly state: 'unavailable'; readonly reason: string }

export type LookupResponse =
  | {
      readonly type: 'lookup-result'
      readonly status: 'ready'
      readonly matches: readonly DictionaryEntry[]
      /** Token list when the selection split into 2+ tokens, else null. */
      readonly tokens: readonly TokenInfo[] | null
      /** False when the tokenizer failed to load (exact match only). */
      readonly deinflectionAvailable: boolean
    }
  | { readonly type: 'lookup-result'; readonly status: 'initializing'; readonly progress: DictProgress | null }
  | { readonly type: 'lookup-result'; readonly status: 'unavailable'; readonly reason: string }

export interface DictStatusResponse {
  readonly type: 'dict-status'
  readonly status: DictionaryStatus
}

export type BackgroundResponse = LookupResponse | DictStatusResponse
