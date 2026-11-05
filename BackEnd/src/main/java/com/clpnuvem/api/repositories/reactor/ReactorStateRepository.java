package com.clpnuvem.api.repositories.reactor;

import com.clpnuvem.api.models.reactor.ReactorState;
import org.springframework.stereotype.Repository;

import java.util.List;
import java.util.Map;
import java.util.Objects;
import java.util.concurrent.ConcurrentHashMap;

/**
 * Estado atual dos tanques em memória (perdido ao reiniciar; o DataManager reenvia a cada ciclo).
 * Chave = CLP + tanque, pois o mesmo nome de tanque pode existir em CLPs diferentes (ex.: R280).
 */
@Repository
public class ReactorStateRepository {

    private final Map<String, ReactorState> states = new ConcurrentHashMap<>();

    /**
     * Grava a leitura, a menos que já exista uma leitura MAIS NOVA do mesmo tanque
     * (envio atrasado/fora de ordem não pode sobrescrever dado mais recente).
     *
     * @return false se a leitura foi descartada por ser mais antiga
     */
    public boolean saveIfNewer(ReactorState state) {
        ReactorState result = states.merge(
                key(state.plcId(), state.reactorId()),
                state,
                (current, incoming) -> incoming.readAt().isBefore(current.readAt()) ? current : incoming);
        return result == state;
    }

    public List<ReactorState> findAll() {
        return List.copyOf(states.values());
    }

    private static String key(String plcId, String reactorId) {
        return Objects.requireNonNull(plcId) + "/" + Objects.requireNonNull(reactorId);
    }
}
