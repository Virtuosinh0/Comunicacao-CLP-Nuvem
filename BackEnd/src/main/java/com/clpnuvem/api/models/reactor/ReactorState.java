package com.clpnuvem.api.models.reactor;

import java.time.Instant;

/**
 * Estado atual de um tanque (última leitura recebida). opCode vazio = tanque sem OP.
 *
 * @param readAt     quando o DataManager leu o CLP (relógio do gateway)
 * @param receivedAt quando a API recebeu (relógio da API) — base para saber se o dado está desatualizado
 */
public record ReactorState(
        String plcId,
        String reactorId,
        String opCode,
        String productCode,
        Integer status,
        Double requestedWeightKg,
        Double producedWeightKg,
        Integer currentStep,
        Instant startTime,
        Instant endTime,
        Instant readAt,
        Instant receivedAt) {

    public boolean hasOp() {
        return opCode != null && !opCode.isBlank();
    }
}
