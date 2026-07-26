/**
 * Typed messaging contracts between content scripts and the background
 * service worker. Every message is a member of a discriminated union so the
 * router can narrow on `type` exhaustively. Lookup messages arrive in Phase 3+.
 */

export interface PingRequest {
  readonly type: 'ping'
}

export interface PongResponse {
  readonly type: 'pong'
}

/** Union of every request a content script may send to the service worker. */
export type BackgroundRequest = PingRequest

/** Union of every response the service worker may send back. */
export type BackgroundResponse = PongResponse
