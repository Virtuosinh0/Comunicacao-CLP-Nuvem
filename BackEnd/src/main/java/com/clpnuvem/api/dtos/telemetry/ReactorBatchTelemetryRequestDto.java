package com.clpnuvem.api.dtos.telemetry;

import jakarta.validation.Valid;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.NotNull;

import java.time.Instant;
import java.util.List;

/** POST /api/v1/telemetry/reactors/batch: todas as OPs de um CLP em um único envio. */
public record ReactorBatchTelemetryRequestDto(
        @NotBlank String plcId,
        @NotNull Instant timestamp,
        @NotNull List<@Valid @NotNull ReactorTelemetryRequestDto> reactors) {
}
