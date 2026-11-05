package com.clpnuvem.api.service;

import com.clpnuvem.api.dto.telemetry.ReactorBatchTelemetry;
import com.clpnuvem.api.dto.telemetry.ReactorTelemetry;
import com.clpnuvem.api.model.ReactorState;
import com.clpnuvem.api.repository.ReactorStateRepository;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.HashSet;
import java.util.List;
import java.util.Set;

@Service
public class TelemetryService {

    private final ReactorStateRepository repository;

    public TelemetryService(ReactorStateRepository repository) {
        this.repository = repository;
    }

    /** Uma OP por requisição. */
    public void receiveReactor(ReactorTelemetry t) {
        Instant timestamp = t.timestamp() != null ? t.timestamp() : Instant.now();
        repository.save(toState(t.plcId(), t, timestamp));
    }

    /** Todas as OPs de um CLP: substitui o estado do CLP inteiro de uma vez. */
    public void receiveBatch(ReactorBatchTelemetry batch) {
        Set<String> seen = new HashSet<>();
        for (ReactorTelemetry t : batch.reactors()) {
            if (!seen.add(t.reactorId().trim())) {
                throw new IllegalArgumentException(
                        "reactor_id '" + t.reactorId() + "' repetido no lote do " + batch.plcId());
            }
        }

        List<ReactorState> states = batch.reactors().stream()
                .map(t -> toState(batch.plcId(), t, batch.timestamp()))
                .toList();

        repository.replacePlc(batch.plcId(), states);
    }

    private static ReactorState toState(String plcId, ReactorTelemetry t, Instant timestamp) {
        return new ReactorState(
                plcId,
                t.reactorId().trim(),
                t.opCode().trim(),
                t.productCode(),
                t.status(),
                t.priority(),
                t.requestedWeightKg(),
                t.producedWeightKg(),
                t.currentStep(),
                t.startTime(),
                t.endTime(),
                t.finalDestination(),
                timestamp);
    }
}
