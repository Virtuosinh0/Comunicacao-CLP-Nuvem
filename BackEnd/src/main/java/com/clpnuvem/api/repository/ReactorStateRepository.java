package com.clpnuvem.api.repository;

import com.clpnuvem.api.model.ReactorState;
import org.springframework.stereotype.Repository;

import java.util.Collection;
import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.Set;
import java.util.concurrent.ConcurrentHashMap;
import java.util.stream.Collectors;

/**
 * Estado atual dos tanques em memória (perdido ao reiniciar; o DataManager reenvia a cada ciclo).
 * Chave = CLP + tanque, pois o mesmo nome de tanque pode existir em CLPs diferentes (ex.: R280).
 */
@Repository
public class ReactorStateRepository {

    private final Map<String, ReactorState> states = new ConcurrentHashMap<>();

    public void save(ReactorState state) {
        states.put(key(state.plcId(), state.reactorId()), state);
    }

    /** Substitui todos os tanques de um CLP: os que não vieram no lote são removidos. */
    public synchronized void replacePlc(String plcId, Collection<ReactorState> plcStates) {
        Set<String> keep = plcStates.stream()
                .map(s -> key(s.plcId(), s.reactorId()))
                .collect(Collectors.toSet());

        plcStates.forEach(this::save);
        states.entrySet().removeIf(e -> Objects.equals(e.getValue().plcId(), plcId) && !keep.contains(e.getKey()));
    }

    public List<ReactorState> findAll() {
        return List.copyOf(states.values());
    }

    private static String key(String plcId, String reactorId) {
        return Objects.toString(plcId, "") + "/" + reactorId;
    }
}
