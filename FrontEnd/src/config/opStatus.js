/**
 * Status da OP (campo "status" do UDT no CLP), com a cor da paleta da IHM.
 * Código fora desta lista é exibido como "Status N (desconhecido)" — nunca adivinhado.
 *
 * ihm = cor da IHM (classes .op-status--<ihm> em global.css):
 *   ok (48,48,48) · ativo (0,224,0) · pausado (240,192,64) · manual (255,160,0) · descarga (0,128,0)
 *   finalizado (0,176,240) · extra (128,0,128) = laboratório · erro (255,32,32) · cancela (176,0,0)
 */
export const OP_STATUS = {
  0: { label: 'Programado', ihm: 'ok' },
  1: { label: 'Carregada na máquina', ihm: 'ok' },
  2: { label: 'Em execução', ihm: 'ativo' },
  3: { label: 'Pausada', ihm: 'pausado' },
  4: { label: 'Análise C.Q.', ihm: 'extra' },
  5: { label: 'Aprovado C.Q.', ihm: 'extra' },
  6: { label: 'Liberado C.Q.', ihm: 'extra' },
  7: { label: 'Reprovado C.Q.', ihm: 'erro' },
  8: { label: 'Liberado para envase', ihm: 'descarga' },
  9: { label: 'Em processo de envase', ihm: 'descarga' },
  10: { label: 'Aguardando limpeza', ihm: 'manual' },
  11: { label: 'Finalizada', ihm: 'finalizado' },
}

/** { code, label, ihm } para exibição. */
export function describeOpStatus(code) {
  if (typeof code !== 'number' || !Number.isInteger(code)) {
    return { code: null, label: 'Status não informado', ihm: 'desconhecido' }
  }
  const known = OP_STATUS[code]
  return known
    ? { code, ...known }
    : { code, label: `Status ${code} (desconhecido)`, ihm: 'desconhecido' }
}
