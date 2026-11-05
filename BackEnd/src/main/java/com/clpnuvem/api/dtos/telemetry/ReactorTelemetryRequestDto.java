package com.clpnuvem.api.dto.telemetry;

import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

import java.time.Instant;

/**
 * Uma OP/tanque enviada pelo DataManager.
 * Sozinha em POST /api/v1/telemetry/reactors, ou dentro de {@link ReactorBatchTelemetry}.
 * Tanque sem OP: somente reactor_id e op_code = "" (demais campos ausentes/null).
 *
 * @param plcId     só no envio individual (no lote vai no topo)
 * @param timestamp só no envio individual (no lote vai no topo)
 */
public record ReactorTelemetry(
        String plcId,
        @NotBlank String reactorId,
        @NotNull String opCode,
        String productCode,
        Integer status,
        Double requestedWeightKg,
        Double producedWeightKg,
        Integer currentStep,
        Instant startTime,
        Instant endTime,
        Instant timestamp) {
}
