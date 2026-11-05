package com.clpnuvem.api.service;

import com.clpnuvem.api.config.TelemetryProperties;
import com.clpnuvem.api.dto.production.ReactorView;
import com.clpnuvem.api.dto.production.ReactorsResponse;
import com.clpnuvem.api.model.ReactorState;
import com.clpnuvem.api.repository.ReactorStateRepository;
import org.springframework.stereotype.Service;

import java.time.Clock;
import java.time.Instant;
import java.util.Comparator;
import java.util.List;

@Service
public class ProductionService {

    private final ReactorStateRepository repository;
    private final TelemetryProperties properties;
    private final Clock clock;

    public ProductionService(ReactorStateRepository repository, TelemetryProperties properties, Clock clock) {
        this.repository = repository;
        this.properties = properties;
        this.clock = clock;
    }

    /** Somente tanques com OP ativa (op_code preenchido), cada um indicando se o dado está desatualizado. */
    public ReactorsResponse activeReactors() {
        List<ReactorState> all = repository.findAll();
        Instant staleBefore = clock.instant().minus(properties.staleAfter());

        List<ReactorView> active = all.stream()
                .filter(ReactorState::hasOp)
                .sorted(Comparator.comparing(ReactorState::plcId).thenComparing(ReactorState::reactorId))
                .map(s -> toView(s, s.receivedAt().isBefore(staleBefore)))
                .toList();

        Instant lastUpdate = all.stream()
                .map(ReactorState::readAt)
                .max(Comparator.naturalOrder())
                .orElse(null);

        return new ReactorsResponse(active.size(), lastUpdate, active);
    }

    private static ReactorView toView(ReactorState s, boolean stale) {
        Double remaining = s.requestedWeightKg() == null || s.producedWeightKg() == null
                ? null
                : s.requestedWeightKg() - s.producedWeightKg();

        return new ReactorView(
                s.plcId(), s.reactorId(), s.opCode(), s.productCode(), s.status(), s.priority(),
                s.requestedWeightKg(), s.producedWeightKg(), remaining, s.currentStep(),
                s.startTime(), s.endTime(), s.finalDestination(), s.readAt(), stale);
    }
}
