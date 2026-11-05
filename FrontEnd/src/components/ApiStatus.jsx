import { formatTime } from '../utils/format.js'

/**
 * Situação da comunicação com a API (horário do navegador).
 * Em falha, avisa que os dados exibidos são da última resposta recebida.
 */
export default function ApiStatus({ loading, error, lastSuccessAt, failingSince }) {
  if (error) {
    return (
      <div className="api-status api-status--error" role="alert">
        <strong>{error.message}</strong>
        {lastSuccessAt
          ? ` desde ${formatTime(failingSince)}. Os dados abaixo são da última resposta (${formatTime(lastSuccessAt)}) e podem não ser os atuais.`
          : ` desde ${formatTime(failingSince)}. Tentando novamente…`}
      </div>
    )
  }

  if (loading) {
    return <div className="api-status">Conectando à API…</div>
  }

  return (
    <div className="api-status api-status--ok">
      <span className="api-status__dot" aria-hidden="true" />
      Atualizado às {formatTime(lastSuccessAt)}
    </div>
  )
}
