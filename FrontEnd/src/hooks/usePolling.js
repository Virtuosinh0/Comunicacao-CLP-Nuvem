import { useEffect, useState } from 'react'

/**
 * Busca dados periodicamente. A próxima busca só começa depois que a anterior termina
 * (API lenta não acumula requisições).
 *
 * Se uma busca falhar, `data` mantém o último valor recebido, mas `error` fica preenchido:
 * a tela deve deixar claro que esses dados podem não ser os atuais.
 *
 * @param fetcher    ({ signal }) => Promise — deve ser estável (definido fora do componente)
 * @param intervalMs intervalo entre o fim de uma busca e o início da próxima
 */
export function usePolling(fetcher, intervalMs) {
  const [state, setState] = useState({
    data: null,
    error: null,
    loading: true,
    lastSuccessAt: null,
    failingSince: null,
  })

  useEffect(() => {
    let cancelled = false
    let timer
    let controller

    const run = async () => {
      controller = new AbortController()
      const timeout = setTimeout(() => controller.abort(), Math.max(intervalMs, 5000))
      try {
        const data = await fetcher({ signal: controller.signal })
        if (!cancelled) {
          setState({ data, error: null, loading: false, lastSuccessAt: new Date(), failingSince: null })
        }
      } catch (error) {
        if (!cancelled) {
          setState((s) => ({ ...s, error, loading: false, failingSince: s.failingSince ?? new Date() }))
        }
      } finally {
        clearTimeout(timeout)
        if (!cancelled) timer = setTimeout(run, intervalMs)
      }
    }

    run()
    return () => {
      cancelled = true
      clearTimeout(timer)
      controller?.abort()
    }
  }, [fetcher, intervalMs])

  return state
}
