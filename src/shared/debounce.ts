export interface Debounced {
  run(): void
  cancel(): void
}

/** Trailing-edge debounce: `fn` runs `waitMs` after the last `run()` call. */
export function debounce(fn: () => void, waitMs: number): Debounced {
  let timer: ReturnType<typeof setTimeout> | undefined
  return {
    run(): void {
      if (timer !== undefined) clearTimeout(timer)
      timer = setTimeout(() => {
        timer = undefined
        fn()
      }, waitMs)
    },
    cancel(): void {
      if (timer !== undefined) {
        clearTimeout(timer)
        timer = undefined
      }
    },
  }
}
