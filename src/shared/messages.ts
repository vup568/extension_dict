/**
 * Typed messaging contracts between content scripts and the background
 * service worker. Every message is a member of a discriminated union so the
 * router can narrow on `type` exhaustively.
 */
import type { DictionaryEntry, GrammarPart, TargetLang, TokenInfo } from './types'

// ---------------------------------------------------------------------------
// Requests (content script → service worker)

export interface LookupRequest {
  readonly type: 'lookup'
  readonly text: string
}

export interface DictStatusRequest {
  readonly type: 'dict-status'
}

export interface GetPrefsRequest {
  readonly type: 'get-prefs'
}

export interface SetPrefsRequest {
  readonly type: 'set-prefs'
  readonly targetLang: TargetLang
}

/**
 * Online sentence translation. Runs in the service worker because the
 * endpoint needs the extension's host_permission grant to bypass CORS.
 */
export interface TranslateCloudRequest {
  readonly type: 'translate-cloud'
  readonly text: string
  readonly target: TargetLang
}

export type BackgroundRequest =
  | LookupRequest
  | DictStatusRequest
  | GetPrefsRequest
  | SetPrefsRequest
  | TranslateCloudRequest

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
      /** Set when the whole selection is one conjugated grammar unit. */
      readonly grammar: readonly GrammarPart[] | null
      /** False when the tokenizer failed to load (exact match only). */
      readonly deinflectionAvailable: boolean
    }
  | { readonly type: 'lookup-result'; readonly status: 'initializing'; readonly progress: DictProgress | null }
  | { readonly type: 'lookup-result'; readonly status: 'unavailable'; readonly reason: string }

export interface DictStatusResponse {
  readonly type: 'dict-status'
  readonly status: DictionaryStatus
}

export interface PrefsResponse {
  readonly type: 'prefs'
  readonly targetLang: TargetLang
}

export type TranslateCloudResponse =
  | { readonly type: 'translate-cloud-result'; readonly ok: true; readonly text: string }
  | { readonly type: 'translate-cloud-result'; readonly ok: false; readonly reason: string }

export type BackgroundResponse = LookupResponse | DictStatusResponse | PrefsResponse | TranslateCloudResponse
