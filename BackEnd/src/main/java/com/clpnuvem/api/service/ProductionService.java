package com.clpnuvem.api.service;

import com.clpnuvem.api.dto.production.ReactorView;
import com.clpnuvem.api.dto.production.ReactorsResponse;
import com.clpnuvem.api.model.ReactorState;
import com.clpnuvem.api.repository.ReactorStateRepository;
import org.springframework.stereotype.Service;

import java.time.Instant;
import java.util.Comparator;
import java.util.List;

@Service
public class ProductionService {

    private final ReactorStateRepository repository;

    public ProductionService(ReactorStateRepository repository) {
        this.repository = repository;
    }

    /** Somente tanques com OP ativa (op_code preenchido). */
    public ReactorsResponse activeReactors() {
        List<ReactorState> all = repository.findAll();

        List<ReactorView> active = all.stream()
                .filter(ReactorState::hasOp)
                .sorted(Comparator.comparing(ReactorState::plcId, Comparator.nullsFirst(Comparator.naturalOrder()))
                        .thenComparing(ReactorState::reactorId))
                .map(ProductionService::toView)
                .toList();

        Instant lastUpdate = all.stream()
                .map(ReactorState::lastUpdate)
                .max(Comparator.naturalOrder())
                .orElse(null);

        return new ReactorsResponse(active.size(), lastUpdate, active);
    }

    private static ReactorView toView(ReactorState s) {
        Double remaining = s.requestedWeightKg() == null || s.producedWeightKg() == null
                ? null
                : Math.max(0, s.requestedWeightKg() - s.producedWeightKg());

        return new ReactorView(
                s.plcId(), s.reactorId(), s.opCode(), s.productCode(), s.status(), s.priority(),
                s.requestedWeightKg(), s.producedWeightKg(), remaining, s.currentStep(),
                s.startTime(), s.endTime(), s.finalDestination(), s.lastUpdate());
    }
}
