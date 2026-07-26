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
import type { PopupModel } from '../ui'
import popupCss from '../ui/popup.css?inline'

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

export class PopupController {
  private parts: HostParts | null = null
  private anchorRange: Range | null = null
  private visible = false
  private showCount = 0
  private lastInnerInteraction = 0
  private repositionQueued = false

  show(model: PopupModel, range: Range): void {
    const parts = this.ensureHost()
    // Clone: the live Selection mutates its Range on the next selection,
    // which would corrupt our anchor for scroll repositioning.
    this.anchorRange = range.cloneRange()
    // New key per show() remounts <Popup>, resetting its internal state
    // (entry index, "show more") for every new selection.
    this.showCount += 1
    render(h(Popup, { model, key: this.showCount }), parts.mountPoint)

    if (!this.visible) {
      this.visible = true
      this.attachGlobalListeners()
    }
    // Render hidden first; position once real dimensions are measurable.
    parts.host.style.visibility = 'hidden'
    requestAnimationFrame(() => this.reposition(true))
  }

  hide(): void {
    if (!this.visible) return
    this.visible = false
    this.anchorRange = null
    this.detachGlobalListeners()
    if (this.parts !== null) {
      this.parts.host.style.visibility = 'hidden'
      render(null, this.parts.mountPoint) // unmount so no stale state lingers
    }
  }

  /** Called when the selection collapsed or became irrelevant. */
  handleSelectionCleared(): void {
    if (performance.now() - this.lastInnerInteraction < INNER_INTERACTION_GRACE_MS) return
    this.hide()
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

    document.documentElement.append(host)
    this.parts = { host, shadow, mountPoint }
    return this.parts
  }

  /**
   * Position below the selection; flip above when there is no room, then
   * clamp both axes into the viewport.
   */
  private reposition(reveal = false): void {
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
