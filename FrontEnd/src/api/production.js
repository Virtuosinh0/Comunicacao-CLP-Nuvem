import { getJson } from './client.js'

/**
 * GET /api/v1/production/reactors — tanques com OP ativa.
 * Resposta: { total_active_ops, last_update, reactors: [{ plc_id, reactor_id, op_code, ..., stale }] }
 */
export function fetchActiveReactors(options) {
  return getJson('/api/v1/production/reactors', options)
}
