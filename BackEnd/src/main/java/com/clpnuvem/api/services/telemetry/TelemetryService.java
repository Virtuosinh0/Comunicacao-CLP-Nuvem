package com.clpnuvem.api.service;

import com.clpnuvem.api.config.TelemetryProperties;
import com.clpnuvem.api.dto.telemetry.ReactorBatchTelemetry;
import com.clpnuvem.api.dto.telemetry.ReactorTelemetry;
import com.clpnuvem.api.model.ReactorState;
import com.clpnuvem.api.repository.ReactorStateRepository;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

import java.time.Clock;
import java.time.Instant;
import java.util.HashSet;
import java.util.Set;

@Service
public class TelemetryService {

    private static final Logger log = LoggerFactory.getLogger(TelemetryService.class);

    private final ReactorStateRepository repository;
    private final TelemetryProperties properties;
    private final Clock clock;

    public TelemetryService(ReactorStateRepository repository, TelemetryProperties properties, Clock clock) {
        this.repository = repository;
        this.properties = properties;
        this.clock = clock;
    }

    /** Uma OP por requisição. plc_id e timestamp são obrigatórios (não inventamos origem nem hora). */
    public void receiveReactor(ReactorTelemetry t) {
        if (t.plcId() == null || t.plcId().isBlank()) {
            throw new IllegalArgumentException("plc_id é obrigatório no envio individual");
        }
        if (t.timestamp() == null) {
            throw new IllegalArgumentException("timestamp é obrigatório no envio individual");
        }

        Instant now = clock.instant();
        checkNotInFuture(t.timestamp(), now);
        save(toState(t.plcId().trim(), t, t.timestamp(), now));
    }

    /**
     * Todas as OPs de um CLP. Tanque que não veio no lote NÃO é apagado:
     * ele fica com a última leitura e aparece como desatualizado (stale) no GET.
     */
    public void receiveBatch(ReactorBatchTelemetry batch) {
        Instant now = clock.instant();
        checkNotInFuture(batch.timestamp(), now);

        Set<String> seen = new HashSet<>();
        for (ReactorTelemetry t : batch.reactors()) {
            if (!seen.add(t.reactorId().trim())) {
                throw new IllegalArgumentException(
                        "reactor_id '" + t.reactorId() + "' repetido no lote do " + batch.plcId());
            }
        }

        String plcId = batch.plcId().trim();
        batch.reactors().forEach(t -> save(toState(plcId, t, batch.timestamp(), now)));
    }

    private void save(ReactorState state) {
        if (!repository.saveIfNewer(state)) {
            log.warn("Leitura descartada: {}/{} de {} é mais antiga que a já registrada",
                    state.plcId(), state.reactorId(), state.readAt());
        }
    }

    private void checkNotInFuture(Instant readAt, Instant now) {
        if (readAt.isAfter(now.plus(properties.maxClockSkew()))) {
            throw new IllegalArgumentException(
                    "timestamp " + readAt + " está no futuro (agora: " + now + "). Verifique o relógio do gateway.");
        }
    }

    private static ReactorState toState(String plcId, ReactorTelemetry t, Instant readAt, Instant receivedAt) {
        return new ReactorState(
                plcId,
                t.reactorId().trim(),
                t.opCode().trim(),
                t.productCode(),
                t.status(),
                t.requestedWeightKg(),
                t.producedWeightKg(),
                t.currentStep(),
                t.startTime(),
                t.endTime(),
                readAt,
                receivedAt);
    }
}
