package com.clpnuvem.api.services.telemetry;

import com.clpnuvem.api.configurations.TelemetryProperties;
import com.clpnuvem.api.dtos.telemetry.ReactorBatchTelemetryRequestDto;
import com.clpnuvem.api.dtos.telemetry.ReactorTelemetryRequestDto;
import com.clpnuvem.api.mappers.reactor.ReactorMapper;
import com.clpnuvem.api.models.reactor.ReactorState;
import com.clpnuvem.api.repositories.reactor.ReactorStateRepository;
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
    private final ReactorMapper mapper;
    private final TelemetryProperties properties;
    private final Clock clock;

    public TelemetryService(ReactorStateRepository repository, ReactorMapper mapper,
                            TelemetryProperties properties, Clock clock) {
        this.repository = repository;
        this.mapper = mapper;
        this.properties = properties;
        this.clock = clock;
    }

    /** Uma OP por requisição. plc_id e timestamp são obrigatórios (não inventamos origem nem hora). */
    public void receiveReactor(ReactorTelemetryRequestDto dto) {
        if (dto.plcId() == null || dto.plcId().isBlank()) {
            throw new IllegalArgumentException("plc_id é obrigatório no envio individual");
        }
        if (dto.timestamp() == null) {
            throw new IllegalArgumentException("timestamp é obrigatório no envio individual");
        }

        Instant now = clock.instant();
        checkNotInFuture(dto.timestamp(), now);
        save(mapper.toState(dto.plcId().trim(), dto, dto.timestamp(), now));
    }

    /**
     * Todas as OPs de um CLP. Tanque que não veio no lote NÃO é apagado:
     * ele fica com a última leitura e aparece como desatualizado (stale) no GET.
     */
    public void receiveBatch(ReactorBatchTelemetryRequestDto batch) {
        Instant now = clock.instant();
        checkNotInFuture(batch.timestamp(), now);

        Set<String> seen = new HashSet<>();
        for (ReactorTelemetryRequestDto dto : batch.reactors()) {
            if (!seen.add(dto.reactorId().trim())) {
                throw new IllegalArgumentException(
                        "reactor_id '" + dto.reactorId() + "' repetido no lote do " + batch.plcId());
            }
        }

        String plcId = batch.plcId().trim();
        batch.reactors().forEach(dto -> save(mapper.toState(plcId, dto, batch.timestamp(), now)));
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
}
