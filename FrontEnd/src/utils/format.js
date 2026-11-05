/** Formatação para exibição (pt-BR, horário local do navegador). Valor ausente → "—". */

export const EMPTY = '—'

const kgFormat = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 1 })
const timeFormat = new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit', second: '2-digit' })
const dateTimeFormat = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  hour: '2-digit',
  minute: '2-digit',
})

const isNumber = (value) => typeof value === 'number' && Number.isFinite(value)

export function formatKg(value) {
  return isNumber(value) ? `${kgFormat.format(value)} kg` : EMPTY
}

export function formatNumber(value) {
  return isNumber(value) ? String(value) : EMPTY
}

export function formatText(value) {
  return typeof value === 'string' && value.trim() !== '' ? value.trim() : EMPTY
}

function toDate(value) {
  if (!value) return null
  const date = value instanceof Date ? value : new Date(value)
  return Number.isNaN(date.getTime()) ? null : date
}

/** 12:11:09 */
export function formatTime(value) {
  const date = toDate(value)
  return date ? timeFormat.format(date) : EMPTY
}

/** 05/11 10:51 — ou só a hora se for hoje. */
export function formatDateTime(value) {
  const date = toDate(value)
  if (!date) return EMPTY
  const today = new Date()
  const sameDay = date.toDateString() === today.toDateString()
  return sameDay ? timeFormat.format(date).slice(0, 5) : dateTimeFormat.format(date)
}

/** Duração entre duas datas: "1 h 23 min", "45 min". Null se faltar alguma data ou for negativa. */
export function formatDuration(from, to) {
  const start = toDate(from)
  const end = toDate(to)
  if (!start || !end) return EMPTY
  const minutes = Math.floor((end - start) / 60000)
  if (minutes < 0) return EMPTY
  const h = Math.floor(minutes / 60)
  const m = minutes % 60
  return h > 0 ? `${h} h ${String(m).padStart(2, '0')} min` : `${m} min`
}

/**
 * Percentual produzido/programado. Null se não houver quantidade programada (> 0).
 * Não é limitado a 100: produção acima do pedido aparece como > 100%.
 */
export function progressPercent(produced, requested) {
  if (!isNumber(produced) || !isNumber(requested) || requested <= 0) return null
  return (produced / requested) * 100
}
