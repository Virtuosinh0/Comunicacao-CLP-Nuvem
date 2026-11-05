package com.clpnuvem.api.mappers.reactor;

import com.clpnuvem.api.dtos.production.ReactorResponseDto;
import com.clpnuvem.api.dtos.telemetry.ReactorTelemetryRequestDto;
import com.clpnuvem.api.models.reactor.ReactorState;
import org.springframework.stereotype.Component;

import java.time.Instant;

/** Conversões DTO ↔ modelo do tanque. Só copia/calcula: nenhuma regra de aceitar/rejeitar aqui. */
@Component
public class ReactorMapper {

    /** Leitura recebida do DataManager → estado do tanque. plcId e readAt vêm do envio (individual ou lote). */
    public ReactorState toState(String plcId, ReactorTelemetryRequestDto dto, Instant readAt, Instant receivedAt) {
        return new ReactorState(
                plcId,
                dto.reactorId().trim(),
                dto.opCode().trim(),
                dto.productCode(),
                dto.status(),
                dto.requestedWeightKg(),
                dto.producedWeightKg(),
                dto.currentStep(),
                dto.startTime(),
                dto.endTime(),
                readAt,
                receivedAt);
    }

    /** Estado do tanque → item da tela de Produção. */
    public ReactorResponseDto toDto(ReactorState s, boolean stale) {
        // Sem arredondar nem cortar: negativo = produziu acima do pedido. Sem um dos pesos → null (nunca 0).
        Double remaining = s.requestedWeightKg() == null || s.producedWeightKg() == null
                ? null
                : s.requestedWeightKg() - s.producedWeightKg();

        return new ReactorResponseDto(
                s.plcId(), s.reactorId(), s.opCode(), s.productCode(), s.status(),
                s.requestedWeightKg(), s.producedWeightKg(), remaining, s.currentStep(),
                s.startTime(), s.endTime(), s.readAt(), stale);
    }
}
