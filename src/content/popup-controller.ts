/**
 * Owns the popup's host element, its closed shadow root, positioning, and
 * dismissal behavior. Language-agnostic: it renders whatever entries it is
 * given.
 *
 * Isolation strategy:
 * - The host is a custom tag (<jpdict-popup>) so broad page CSS selectors
 *   (div, span, …) never match it; its critical styles are inline.
 * - The shadow root is `closed` and the only reference to it lives inside
 *   this class, so page scripts can neither reach nor restyle the UI.
 */
import { h, render } from 'preact'
import { Popup } from '../ui'
import type { PopupModel, TranslationControls } from '../ui'
import popupCss from '../ui/popup.css?inline'

import type { TokenInfo } from '../shared/types'

/** Callbacks the popup content needs; provided by the composition root. */
export interface PopupHandlers {
  readonly onTokenClick?: (token: TokenInfo) => void
  readonly onRequestKanji?: () => void
  readonly onRequestTranslation?: () => void
  readonly translation?: TranslationControls
}

/** Gap between the selection rectangle and the popup. */
const GAP = 8
/** Minimum distance the popup keeps from every viewport edge. */
const MARGIN = 8
/**
 * Clicking / drag-selecting inside the popup collapses the page selection,
 * which fires a selectionchange "clear". Interactions inside the popup within
 * this window suppress that clear so the popup doesn't dismiss itself.
 */
const INNER_INTERACTION_GRACE_MS = 600

interface HostParts {
  readonly host: HTMLElement
  readonly shadow: ShadowRoot
  readonly mountPoint: HTMLElement
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(Math.max(value, min), Math.max(min, max))
}

export class PopupController {
  private parts: HostParts | null = null
  private anchorRange: Range | null = null
  private visible = false
  private showCount = 0
  private lastInnerInteraction = 0
  private repositionQueued = false
  private lastHandlers: PopupHandlers | undefined
  /** True once the user dragged the popup: it stays where they put it. */
  private userPinned = false
  private dragState: { pointerId: number; offsetX: number; offsetY: number } | null = null

  show(model: PopupModel, range: Range, handlers?: PopupHandlers): void {
    const parts = this.ensureHost()
    // Clone: the live Selection mutates its Range on the next selection,
    // which would corrupt our anchor for scroll repositioning.
    this.anchorRange = range.cloneRange()
    // New key per show() remounts <Popup>, resetting its internal state
    // (entry index, "show more") for every new selection.
    this.showCount += 1
    this.lastHandlers = handlers
    this.userPinned = false // a fresh selection re-anchors next to it
    this.endDrag()
    this.renderPopup(model)

    if (!this.visible) {
      this.visible = true
      this.attachGlobalListeners()
    }
    // Render hidden first; position once real dimensions are measurable.
    parts.host.style.visibility = 'hidden'
    requestAnimationFrame(() => this.reposition(true))
  }

  /**
   * Re-render the popup content in place — SAME key, so the component tree
   * is diffed instead of remounted and internal UI state (entry index,
   * "show more") survives. Used for async state updates like translation
   * progress. Repositions afterwards since the panel size may change.
   */
  update(model: PopupModel, handlers?: PopupHandlers): void {
    if (!this.visible || this.parts === null) return
    if (handlers !== undefined) this.lastHandlers = handlers
    this.renderPopup(model)
    this.onViewportChange()
  }

  private renderPopup(model: PopupModel): void {
    if (this.parts === null) return
    render(
      h(Popup, {
        model,
        onTokenClick: this.lastHandlers?.onTokenClick,
        onRequestKanji: this.lastHandlers?.onRequestKanji,
        onRequestTranslation: this.lastHandlers?.onRequestTranslation,
        translation: this.lastHandlers?.translation,
        key: this.showCount,
      }),
      this.parts.mountPoint,
    )
  }

  hide(): void {
    if (!this.visible) return
    this.visible = false
    this.anchorRange = null
    this.userPinned = false
    this.endDrag()
    this.detachGlobalListeners()
    if (this.parts !== null) {
      this.parts.host.style.visibility = 'hidden'
      render(null, this.parts.mountPoint) // unmount so no stale state lingers
    }
  }

  /**
   * True while a pointer interaction inside the popup just happened — the
   * caller uses this to ignore the selection-collapse it causes.
   */
  hasRecentInnerInteraction(): boolean {
    return performance.now() - this.lastInnerInteraction < INNER_INTERACTION_GRACE_MS
  }

  private ensureHost(): HostParts {
    if (this.parts !== null) {
      // SPAs can replace <html> subtrees; re-attach if we got disconnected.
      if (!this.parts.host.isConnected) document.documentElement.append(this.parts.host)
      return this.parts
    }
    const host = document.createElement('jpdict-popup')
    // position:fixed on a documentElement child never shifts page layout.
    host.style.cssText =
      'position:fixed;left:0;top:0;z-index:2147483647;visibility:hidden;'
    const shadow = host.attachShadow({ mode: 'closed' })
    const style = document.createElement('style')
    style.textContent = popupCss
    const mountPoint = document.createElement('div')
    shadow.append(style, mountPoint)

    const markInnerInteraction = (): void => {
      this.lastInnerInteraction = performance.now()
    }
    host.addEventListener('pointerdown', markInnerInteraction)
    host.addEventListener('pointerup', markInnerInteraction)
    // Drag-to-move: listening on the shadow root sees the real inner
    // targets (we own the closed root), unlike the retargeted host events.
    shadow.addEventListener('pointerdown', this.onShadowPointerDown)

    document.documentElement.append(host)
    this.parts = { host, shadow, mountPoint }
    return this.parts
  }

  /**
   * Position below the selection; flip above when there is no room, then
   * clamp both axes into the viewport.
   */
  private reposition(reveal = false): void {
    if (this.userPinned) return // dragged popups stay where the user put them
    if (!this.visible || this.parts === null || this.anchorRange === null) return
    const anchor = this.anchorRange.getBoundingClientRect()
    if (anchor.width === 0 && anchor.height === 0) {
      // The selected content was removed from the DOM (SPA rerender).
      this.hide()
      return
    }
    const panel = this.parts.shadow.querySelector('.panel')
    if (!(panel instanceof HTMLElement)) return
    const { width, height } = panel.getBoundingClientRect()

    let top = anchor.bottom + GAP
    if (top + height > window.innerHeight - MARGIN) {
      const above = anchor.top - GAP - height
      top = above >= MARGIN ? above : Math.max(MARGIN, window.innerHeight - MARGIN - height)
    }
    const left = Math.max(
      MARGIN,
      Math.min(anchor.left, window.innerWidth - MARGIN - width),
    )

    this.parts.host.style.left = `${Math.round(left)}px`
    this.parts.host.style.top = `${Math.round(top)}px`
    if (reveal) this.parts.host.style.visibility = 'visible'
  }

  /**
   * Start dragging unless the press landed on interactive or copyable
   * content (buttons, token chips, sense/translation text). The drag
   * handle, headword, and empty panel background all move the popup.
   */
  private readonly onShadowPointerDown = (event: Event): void => {
    if (!(event instanceof PointerEvent) || event.button !== 0) return
    const parts = this.parts
    if (parts === null || !this.isDraggableTarget(event)) return
    event.preventDefault() // keep the press from starting a text selection
    const rect = parts.host.getBoundingClientRect()
    this.userPinned = true
    this.dragState = {
      pointerId: event.pointerId,
      offsetX: event.clientX - rect.left,
      offsetY: event.clientY - rect.top,
    }
    window.addEventListener('pointermove', this.onDragMove, true)
    window.addEventListener('pointerup', this.onDragEnd, true)
    window.addEventListener('pointercancel', this.onDragEnd, true)
  }

  private isDraggableTarget(event: PointerEvent): boolean {
    for (const node of event.composedPath()) {
      if (!(node instanceof HTMLElement)) continue
      if (node.classList.contains('drag-handle')) return true
      if (
        node.tagName === 'BUTTON' ||
        node.classList.contains('tokens') ||
        node.classList.contains('senses') ||
        node.classList.contains('grammar') ||
        node.classList.contains('kanji-list') ||
        node.classList.contains('translate-result')
      ) {
        return false
      }
      if (node.classList.contains('panel')) return true
    }
    return false
  }

  private readonly onDragMove = (event: PointerEvent): void => {
    const drag = this.dragState
    const parts = this.parts
    if (drag === null || parts === null || event.pointerId !== drag.pointerId) return
    event.preventDefault()
    this.lastInnerInteraction = performance.now()
    const panel = parts.shadow.querySelector('.panel')
    const width = panel instanceof HTMLElement ? panel.offsetWidth : 0
    const height = panel instanceof HTMLElement ? panel.offsetHeight : 0
    const left = clamp(event.clientX - drag.offsetX, MARGIN, window.innerWidth - MARGIN - width)
    const top = clamp(event.clientY - drag.offsetY, MARGIN, window.innerHeight - MARGIN - height)
    parts.host.style.left = `${Math.round(left)}px`
    parts.host.style.top = `${Math.round(top)}px`
  }

  private readonly onDragEnd = (event: PointerEvent): void => {
    if (this.dragState !== null && event.pointerId !== this.dragState.pointerId) return
    this.endDrag()
  }

  private endDrag(): void {
    if (this.dragState === null) return
    this.dragState = null
    window.removeEventListener('pointermove', this.onDragMove, true)
    window.removeEventListener('pointerup', this.onDragEnd, true)
    window.removeEventListener('pointercancel', this.onDragEnd, true)
  }

  private readonly onKeyDown = (event: KeyboardEvent): void => {
    if (event.key === 'Escape') this.hide()
  }

  private readonly onPointerDown = (event: PointerEvent): void => {
    // Events from inside the closed shadow root are retargeted to the host,
    // so this single check distinguishes inside from outside clicks.
    if (this.parts !== null && event.target === this.parts.host) return
    this.hide()
  }

  private readonly onViewportChange = (): void => {
    if (this.repositionQueued) return
    this.repositionQueued = true
    requestAnimationFrame(() => {
      this.repositionQueued = false
      this.reposition()
    })
  }

  private attachGlobalListeners(): void {
    document.addEventListener('keydown', this.onKeyDown, true)
    document.addEventListener('pointerdown', this.onPointerDown, true)
    // capture:true also catches scrolls of inner scroll containers.
    window.addEventListener('scroll', this.onViewportChange, { capture: true, passive: true })
    window.addEventListener('resize', this.onViewportChange)
  }

  private detachGlobalListeners(): void {
    document.removeEventListener('keydown', this.onKeyDown, true)
    document.removeEventListener('pointerdown', this.onPointerDown, true)
    window.removeEventListener('scroll', this.onViewportChange, { capture: true })
    window.removeEventListener('resize', this.onViewportChange)
  }
}
