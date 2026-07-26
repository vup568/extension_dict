/**
 * Typed messaging contracts between content scripts and the background
 * service worker. Every message is a member of a discriminated union so the
 * router can narrow on `type` exhaustively.
 */
import type { DictionaryEntry } from './types'

// ---------------------------------------------------------------------------
// Requests (content script → service worker)

export interface PingRequest {
  readonly type: 'ping'
}

export interface LookupRequest {
  readonly type: 'lookup'
  readonly text: string
}

export interface DictStatusRequest {
  readonly type: 'dict-status'
}

export type BackgroundRequest = PingRequest | LookupRequest | DictStatusRequest

// ---------------------------------------------------------------------------
// Responses (service worker → content script)

export interface PongResponse {
  readonly type: 'pong'
}

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
  | { readonly type: 'lookup-result'; readonly status: 'ready'; readonly matches: readonly DictionaryEntry[] }
  | { readonly type: 'lookup-result'; readonly status: 'initializing'; readonly progress: DictProgress | null }
  | { readonly type: 'lookup-result'; readonly status: 'unavailable'; readonly reason: string }

export interface DictStatusResponse {
  readonly type: 'dict-status'
  readonly status: DictionaryStatus
}

export type BackgroundResponse = PongResponse | LookupResponse | DictStatusResponse
