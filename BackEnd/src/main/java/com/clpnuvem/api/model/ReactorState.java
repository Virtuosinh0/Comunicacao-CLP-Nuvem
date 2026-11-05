package com.clpnuvem.api.model;

import java.time.Instant;

/** Estado atual de um tanque (última leitura recebida). opCode vazio = tanque sem OP. */
public record ReactorState(
        String plcId,
        String reactorId,
        String opCode,
        String productCode,
        Integer status,
        Integer priority,
        Double requestedWeightKg,
        Double producedWeightKg,
        Integer currentStep,
        Instant startTime,
        Instant endTime,
        String finalDestination,
        Instant lastUpdate) {

    public boolean hasOp() {
        return opCode != null && !opCode.isBlank();
    }
}
