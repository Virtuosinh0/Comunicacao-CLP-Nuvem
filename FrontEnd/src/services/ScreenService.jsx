import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'

/**
 * Serviço de troca de tela.
 *
 * - Navegação manual: goTo(id), next(), prev() e setas ← → do teclado.
 * - Troca automática: alterna entre as telas com `inRotation: true` a cada N segundos.
 *   Qualquer navegação manual reinicia a contagem.
 * - URL: a tela atual fica no hash (ex.: #/producao), permitindo abrir uma tela direto pelo link.
 * - Preferência de troca automática fica salva no navegador.
 */

const ScreenContext = createContext(null)

const STORAGE_KEY = 'clp-nuvem:screen-rotation'
const MIN_INTERVAL_SECONDS = 5

function readScreenFromHash(screens) {
  const id = window.location.hash.replace(/^#\/?/, '')
  return screens.some((s) => s.id === id) ? id : null
}

function loadRotation(defaults) {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (raw) return { ...defaults, ...JSON.parse(raw) }
  } catch {
    // localStorage indisponível (modo privado, bloqueado etc.): usa o padrão.
  }
  return defaults
}

function saveRotation(rotation) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(rotation))
  } catch {
    // Ignora: a preferência só não será lembrada.
  }
}

function isTypingTarget(target) {
  return target instanceof HTMLElement && target.closest('input, textarea, select, [contenteditable="true"]')
}

export function ScreenProvider({ screens, defaultIntervalSeconds = 30, autoRotateByDefault = false, children }) {
  const [currentId, setCurrentId] = useState(() => readScreenFromHash(screens) ?? screens[0].id)
  // Incrementa a cada navegação: reinicia o temporizador e a barra de progresso.
  const [cycle, setCycle] = useState(0)
  const [rotation, setRotation] = useState(() =>
    loadRotation({ enabled: autoRotateByDefault, intervalSeconds: defaultIntervalSeconds }),
  )

  const restartCycle = useCallback(() => setCycle((c) => c + 1), [])

  const goTo = useCallback(
    (id) => {
      if (!screens.some((s) => s.id === id)) return
      setCurrentId(id)
      restartCycle()
    },
    [screens, restartCycle],
  )

  const step = useCallback(
    (direction, onlyRotation = false) => {
      setCurrentId((current) => {
        const list = onlyRotation ? screens.filter((s) => s.inRotation) : screens
        if (list.length === 0) return current
        const index = list.findIndex((s) => s.id === current)
        if (index === -1) return list[direction > 0 ? 0 : list.length - 1].id
        return list[(index + direction + list.length) % list.length].id
      })
      restartCycle()
    },
    [screens, restartCycle],
  )

  const next = useCallback(() => step(1), [step])
  const prev = useCallback(() => step(-1), [step])

  const setAutoRotate = useCallback(
    (enabled) => {
      setRotation((r) => ({ ...r, enabled }))
      restartCycle()
    },
    [restartCycle],
  )

  const setIntervalSeconds = useCallback(
    (seconds) => {
      const value = Math.max(MIN_INTERVAL_SECONDS, Number(seconds) || MIN_INTERVAL_SECONDS)
      setRotation((r) => ({ ...r, intervalSeconds: value }))
      restartCycle()
    },
    [restartCycle],
  )

  // Troca automática.
  useEffect(() => {
    if (!rotation.enabled) return undefined
    const timer = setTimeout(() => step(1, true), rotation.intervalSeconds * 1000)
    return () => clearTimeout(timer)
  }, [rotation.enabled, rotation.intervalSeconds, cycle, step])

  useEffect(() => saveRotation(rotation), [rotation])

  // Tela atual → URL (replaceState para a rotação não encher o histórico).
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
      cycle,
      rotation,
      goTo,
      next,
      prev,
      setAutoRotate,
      setIntervalSeconds,
    }),
    [screens, currentId, cycle, rotation, goTo, next, prev, setAutoRotate, setIntervalSeconds],
  )

  return <ScreenContext.Provider value={value}>{children}</ScreenContext.Provider>
}

/** Acesso ao serviço de troca de tela a partir de qualquer componente. */
export function useScreen() {
  const context = useContext(ScreenContext)
  if (!context) throw new Error('useScreen deve ser usado dentro de <ScreenProvider>.')
  return context
}
