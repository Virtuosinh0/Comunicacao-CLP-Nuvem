package com.clpnuvem.api.services.production;

import com.clpnuvem.api.configurations.TelemetryProperties;
import com.clpnuvem.api.dtos.production.ReactorResponseDto;
import com.clpnuvem.api.dtos.production.ReactorsResponseDto;
import com.clpnuvem.api.mappers.reactor.ReactorMapper;
import com.clpnuvem.api.models.reactor.ReactorState;
import com.clpnuvem.api.repositories.reactor.ReactorStateRepository;
import org.springframework.stereotype.Service;

import java.time.Clock;
import java.time.Instant;
import java.util.Comparator;
import java.util.List;

@Service
public class ProductionService {

    private final ReactorStateRepository repository;
    private final ReactorMapper mapper;
    private final TelemetryProperties properties;
    private final Clock clock;

    public ProductionService(ReactorStateRepository repository, ReactorMapper mapper,
                             TelemetryProperties properties, Clock clock) {
        this.repository = repository;
        this.mapper = mapper;
        this.properties = properties;
        this.clock = clock;
    }

    /** Somente tanques com OP ativa (op_code preenchido), cada um indicando se o dado está desatualizado. */
    public ReactorsResponseDto activeReactors() {
        List<ReactorState> all = repository.findAll();
        Instant staleBefore = clock.instant().minus(properties.staleAfter());

        List<ReactorResponseDto> active = all.stream()
                .filter(ReactorState::hasOp)
                .sorted(Comparator.comparing(ReactorState::plcId).thenComparing(ReactorState::reactorId))
                .map(s -> mapper.toDto(s, s.receivedAt().isBefore(staleBefore)))
                .toList();

        Instant lastUpdate = all.stream()
                .map(ReactorState::readAt)
                .max(Comparator.naturalOrder())
                .orElse(null);

        return new ReactorsResponseDto(active.size(), lastUpdate, active);
    }
}
