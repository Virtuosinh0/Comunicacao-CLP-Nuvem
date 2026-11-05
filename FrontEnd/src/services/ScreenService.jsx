import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'

/**
 * Serviço de troca de tela.
 *
 * - Navegação manual: goTo(id), next(), prev() e setas ← → do teclado.
 * - URL: a tela atual fica no hash (ex.: #/producao), permitindo abrir uma tela direto pelo link.
 */

const ScreenContext = createContext(null)

function readScreenFromHash(screens) {
  const id = window.location.hash.replace(/^#\/?/, '')
  return screens.some((s) => s.id === id) ? id : null
}

function isTypingTarget(target) {
  return target instanceof HTMLElement && target.closest('input, textarea, select, [contenteditable="true"]')
}

export function ScreenProvider({ screens, children }) {
  const [currentId, setCurrentId] = useState(() => readScreenFromHash(screens) ?? screens[0].id)

  const goTo = useCallback(
    (id) => {
      if (screens.some((s) => s.id === id)) setCurrentId(id)
    },
    [screens],
  )

  const step = useCallback(
    (direction) => {
      setCurrentId((current) => {
        const index = screens.findIndex((s) => s.id === current)
        if (index === -1) return screens[0].id
        return screens[(index + direction + screens.length) % screens.length].id
      })
    },
    [screens],
  )

  const next = useCallback(() => step(1), [step])
  const prev = useCallback(() => step(-1), [step])

  // Tela atual → URL (replaceState para não encher o histórico).
  useEffect(() => {
    const hash = `#/${currentId}`
    if (window.location.hash !== hash) window.history.replaceState(null, '', hash)
  }, [currentId])

  // URL → tela atual (link colado ou hash editado manualmente).
  useEffect(() => {
    const onHashChange = () => {
      const id = readScreenFromHash(screens)
      if (id) goTo(id)
    }
    window.addEventListener('hashchange', onHashChange)
    return () => window.removeEventListener('hashchange', onHashChange)
  }, [screens, goTo])

  // Teclado: ← tela anterior, → próxima tela.
  useEffect(() => {
    const onKeyDown = (event) => {
      if (event.altKey || event.ctrlKey || event.metaKey || isTypingTarget(event.target)) return
      if (event.key === 'ArrowRight') next()
      else if (event.key === 'ArrowLeft') prev()
    }
    window.addEventListener('keydown', onKeyDown)
    return () => window.removeEventListener('keydown', onKeyDown)
  }, [next, prev])

  const value = useMemo(
    () => ({
      screens,
      currentId,
      current: screens.find((s) => s.id === currentId) ?? screens[0],
      goTo,
      next,
      prev,
    }),
    [screens, currentId, goTo, next, prev],
  )

  return <ScreenContext.Provider value={value}>{children}</ScreenContext.Provider>
}

/** Acesso ao serviço de troca de tela a partir de qualquer componente. */
export function useScreen() {
  const context = useContext(ScreenContext)
  if (!context) throw new Error('useScreen deve ser usado dentro de <ScreenProvider>.')
  return context
}
