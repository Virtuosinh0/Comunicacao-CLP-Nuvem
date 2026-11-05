import { fetchActiveReactors } from '../../api/production.js'
import ApiStatus from '../../components/ApiStatus.jsx'
import { usePolling } from '../../hooks/usePolling.js'
import ReactorCard from './ReactorCard.jsx'

/** Mesmo ciclo do DataManager: buscar mais rápido não traz dado novo. */
const POLL_INTERVAL_MS = 5000

export default function Producao() {
  const { data, error, loading, lastSuccessAt, failingSince } = usePolling(fetchActiveReactors, POLL_INTERVAL_MS)
  const reactors = data?.reactors ?? []
  const staleCount = reactors.filter((r) => r.stale).length

  return (
    <section className="page">
      <div className="page__head">
        <div>
          <h1>Revisão de Produção</h1>
          <p className="page__subtitle">
            {data ? `${data.total_active_ops} OP(s) em produção` : 'OPs e reatores em andamento'}
          </p>
        </div>
        <ApiStatus loading={loading} error={error} lastSuccessAt={lastSuccessAt} failingSince={failingSince} />
      </div>

      {staleCount > 0 && (
        <div className="notice notice--stale" role="status">
          {staleCount === reactors.length
            ? 'Nenhum tanque está recebendo leitura nova do CLP. Verifique o DataManager e a comunicação com os CLPs.'
            : `${staleCount} tanque(s) sem leitura nova do CLP. Os valores desses tanques podem não ser os atuais.`}
        </div>
      )}

      {data && reactors.length === 0 && <div className="placeholder">Nenhuma OP em produção no momento.</div>}

      {!data && !loading && <div className="placeholder">Não foi possível carregar os dados.</div>}

      {reactors.length > 0 && (
        <div className={`reactors${error ? ' is-outdated' : ''}`}>
          {reactors.map((reactor) => (
            <ReactorCard key={`${reactor.plc_id}/${reactor.reactor_id}`} reactor={reactor} now={lastSuccessAt} />
          ))}
        </div>
      )}
    </section>
  )
}
