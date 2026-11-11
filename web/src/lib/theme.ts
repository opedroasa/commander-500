import { useCallback, useState } from 'react'

export type Theme = 'light' | 'dark'
const KEY = 'c500.theme'

function current(): Theme {
  return document.documentElement.dataset.theme === 'light' ? 'light' : 'dark'
}

/** Light/dark theme. The initial value is applied by the inline script in index.html (no flash). */
export function useTheme() {
  const [theme, setThemeState] = useState<Theme>(current)

  const setTheme = useCallback((t: Theme) => {
    document.documentElement.dataset.theme = t
    document.querySelector('meta[name="theme-color"]')?.setAttribute('content', t === 'light' ? '#ffffff' : '#0a1128')
    try {
      localStorage.setItem(KEY, t)
    } catch {
      /* storage unavailable: theme lasts for this page only */
    }
    setThemeState(t)
  }, [])

  const toggle = useCallback(() => setTheme(current() === 'light' ? 'dark' : 'light'), [setTheme])
  return { theme, setTheme, toggle }
}
