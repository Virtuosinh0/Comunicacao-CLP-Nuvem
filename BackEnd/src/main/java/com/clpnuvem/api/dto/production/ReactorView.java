package com.clpnuvem.api.dto.production;

import java.time.Instant;

/** Um tanque com OP ativa, como exibido na tela de Revisão de Produção. */
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
        Instant lastUpdate) {
}
