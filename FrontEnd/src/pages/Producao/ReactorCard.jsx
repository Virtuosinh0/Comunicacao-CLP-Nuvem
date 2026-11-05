import { describeOpStatus } from '../../config/opStatus.js'
import {
  EMPTY,
  formatDateTime,
  formatDuration,
  formatKg,
  formatNumber,
  formatText,
  formatTime,
  progressPercent,
} from '../../utils/format.js'

const percentFormat = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 0 })

/** Um tanque com OP ativa. Mostra só o que veio da API; ausente = "—". */
export default function ReactorCard({ reactor, now }) {
  const r = reactor
  const percent = progressPercent(r.produced_weight_kg, r.requested_weight_kg)
  const over = typeof r.remaining_weight_kg === 'number' && r.remaining_weight_kg < 0
  // Duração: até o fim da OP, se houver; senão até agora.
  const durationEnd = r.end_time ?? now
  const status = describeOpStatus(r.status)

  return (
    <article className={`reactor${r.stale ? ' is-stale' : ''}`} aria-label={`Tanque ${r.reactor_id}`}>
      <header className="reactor__head">
        <h2 className="reactor__id">{r.reactor_id}</h2>
        {r.stale && (
          <span className="badge badge--stale" title="A API não recebe leitura nova deste tanque. Os valores podem não ser os atuais.">
            Sem atualização
          </span>
        )}
        <span className="reactor__opcode" title="OP">{formatText(r.op_code)}</span>
      </header>

      <div className="reactor__op">
        <span className="label">Produto</span>
        <span className="value value--strong">{formatText(r.product_code)}</span>
      </div>

      <div className="reactor__progress">
        <div className="reactor__progress-head">
          <span>
            {formatKg(r.produced_weight_kg)} <span className="muted">de {formatKg(r.requested_weight_kg)}</span>
          </span>
          <span className="reactor__percent">{percent === null ? EMPTY : `${percentFormat.format(percent)}%`}</span>
        </div>
        <div
          className="progress"
          role="progressbar"
          aria-valuemin={0}
          aria-valuemax={100}
          aria-valuenow={percent === null ? undefined : Math.round(percent)}
          aria-valuetext={percent === null ? 'Sem quantidade programada' : undefined}
        >
          {percent !== null && <div className="progress__fill" style={{ width: `${Math.min(percent, 100)}%` }} />}
        </div>
        <div className="reactor__remaining">
          {over ? (
            <span className="over">Acima do programado: {formatKg(-r.remaining_weight_kg)}</span>
          ) : (
            <>Restante: {formatKg(r.remaining_weight_kg)}</>
          )}
        </div>
      </div>

      <dl className="reactor__grid">
        <div>
          <dt>Tarefa</dt>
          <dd>{formatNumber(r.current_step)}</dd>
        </div>
        <div>
          <dt>Início</dt>
          <dd>{formatDateTime(r.start_time)}</dd>
        </div>
        <div>
          <dt>{r.end_time ? 'Fim' : 'Em produção há'}</dt>
          <dd>{r.end_time ? formatDateTime(r.end_time) : formatDuration(r.start_time, durationEnd)}</dd>
        </div>
      </dl>

      <div className={`op-status op-status--${status.ihm}`}>
        <span className="op-status__title">Status da OP</span>
        {status.label}
      </div>

      <footer className="reactor__foot">Leitura do CLP: {formatTime(r.last_update)}</footer>
    </article>
  )
}
