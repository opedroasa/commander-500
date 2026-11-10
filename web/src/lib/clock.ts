import { useSyncExternalStore } from 'react'

/** Shared ticking clock: one interval for every timer on the page. */
const clock = { now: Date.now(), listeners: new Set<() => void>(), handle: 0 }

function subscribe(listener: () => void) {
  clock.listeners.add(listener)
  if (clock.listeners.size === 1) {
    clock.now = Date.now()
    clock.handle = window.setInterval(() => {
      clock.now = Date.now()
      clock.listeners.forEach((l) => l())
    }, 500)
  }
  return () => {
    clock.listeners.delete(listener)
    if (clock.listeners.size === 0) window.clearInterval(clock.handle)
  }
}

/** Current client time (ms), re-rendering every 500 ms while mounted. */
export function useNow(): number {
  return useSyncExternalStore(subscribe, () => clock.now)
}

/** Fresh client time for one-off derivations (e.g. server offset when a response arrives). */
export function lastNow(): number {
  clock.now = Date.now()
  return clock.now
}
