package com.clpnuvem.api.dto.production;

import java.time.Instant;

/**
 * Um tanque com OP ativa, como exibido na tela de Revisão de Produção.
 *
 * @param remainingWeightKg requested - produced, sem arredondar nem cortar: negativo = produziu acima do pedido
 * @param lastUpdate        quando o CLP foi lido
 * @param stale             true = sem leitura nova há mais que telemetry.stale-after (valores podem não ser os atuais)
 */
public record ReactorView(
        String plcId,
        String reactorId,
        String opCode,
        String productCode,
        Integer status,
        Integer priority,
        Double requestedWeightKg,
        Double producedWeightKg,
        Double remainingWeightKg,
        Integer currentStep,
        Instant startTime,
        Instant endTime,
        String finalDestination,
        Instant lastUpdate,
        boolean stale) {
}
