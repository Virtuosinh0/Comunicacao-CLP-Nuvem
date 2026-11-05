package com.clpnuvem.api.controller;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.boot.test.context.TestConfiguration;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Primary;
import org.springframework.http.MediaType;
import org.springframework.test.annotation.DirtiesContext;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.ResultActions;

import java.time.Clock;
import java.time.Duration;
import java.time.Instant;
import java.time.ZoneId;
import java.time.ZoneOffset;

import static org.hamcrest.Matchers.closeTo;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
@DirtiesContext(classMode = DirtiesContext.ClassMode.AFTER_EACH_TEST_METHOD)
class TelemetryControllerTests {

    /** "Agora" da API nos testes; avançado manualmente com {@link MutableClock#advance}. */
    static final Instant NOW = Instant.parse("2026-11-05T14:50:00Z");

    @TestConfiguration
    static class ClockConfig {
        @Bean
        @Primary
        MutableClock testClock() {
            return new MutableClock(NOW);
        }
    }

    @Autowired
    private MockMvc mvc;

    @Autowired
    private MutableClock clock;

    @Test
    void batchUpdatesStateAndIdleTankLeavesList() throws Exception {
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:07Z","reactors":[
                  {"reactor_id":"R210","op_code":"AEMEKL01001","product_code":"P62475","status":2,
                   "requested_weight_kg":6500,"produced_weight_kg":3767.4,"current_step":3,
                   "start_time":"2026-11-05T14:14:47.5815014Z"},
                  {"reactor_id":"R211","op_code":""},
                  {"reactor_id":"R371","op_code":"AEMEMC01001","requested_weight_kg":5000,"produced_weight_kg":4661}
                ]}""").andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.total_active_ops").value(2))
                .andExpect(jsonPath("$.last_update").value("2026-11-05T14:49:07Z"))
                .andExpect(jsonPath("$.reactors[0].reactor_id").value("R210"))
                .andExpect(jsonPath("$.reactors[0].plc_id").value("CLP2"))
                .andExpect(jsonPath("$.reactors[0].remaining_weight_kg").value(closeTo(2732.6, 0.001)))
                .andExpect(jsonPath("$.reactors[0].stale").value(false))
                .andExpect(jsonPath("$.reactors[1].reactor_id").value("R371"));

        // R371 terminou (op_code vazio): sai da lista.
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:12Z","reactors":[
                  {"reactor_id":"R210","op_code":"AEMEKL01001","requested_weight_kg":6500,"produced_weight_kg":3782.5},
                  {"reactor_id":"R211","op_code":""},
                  {"reactor_id":"R371","op_code":""}
                ]}""").andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.total_active_ops").value(1))
                .andExpect(jsonPath("$.reactors[0].produced_weight_kg").value(3782.5));
    }

    @Test
    void tankMissingFromBatchIsKeptAndBecomesStale() throws Exception {
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:00Z","reactors":[
                  {"reactor_id":"R210","op_code":"OP1"},{"reactor_id":"R371","op_code":"OP2"}]}""")
                .andExpect(status().isNoContent());

        // Próximo lote sem o R371 (ex.: erro ao converter): ele NÃO some, só deixa de ser atualizado.
        clock.advance(Duration.ofSeconds(40));
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:40Z","reactors":[{"reactor_id":"R210","op_code":"OP1"}]}""")
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.total_active_ops").value(2))
                .andExpect(jsonPath("$.reactors[0].reactor_id").value("R210"))
                .andExpect(jsonPath("$.reactors[0].stale").value(false))
                .andExpect(jsonPath("$.reactors[1].reactor_id").value("R371"))
                .andExpect(jsonPath("$.reactors[1].stale").value(true));
    }

    @Test
    void everythingBecomesStaleWhenDataManagerStopsSending() throws Exception {
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:00Z","reactors":[{"reactor_id":"R210","op_code":"OP1"}]}""")
                .andExpect(status().isNoContent());

        clock.advance(Duration.ofSeconds(29));
        mvc.perform(get("/api/v1/production/reactors")).andExpect(jsonPath("$.reactors[0].stale").value(false));

        clock.advance(Duration.ofSeconds(2));
        mvc.perform(get("/api/v1/production/reactors")).andExpect(jsonPath("$.reactors[0].stale").value(true));
    }

    @Test
    void olderReadingDoesNotOverwriteNewer() throws Exception {
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:10Z","reactors":[
                  {"reactor_id":"R210","op_code":"OP1","produced_weight_kg":200}]}""")
                .andExpect(status().isNoContent());

        // Chega atrasado um envio lido ANTES: é aceito (204) mas descartado.
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:05Z","reactors":[
                  {"reactor_id":"R210","op_code":"OP1","produced_weight_kg":100}]}""")
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.reactors[0].produced_weight_kg").value(200.0))
                .andExpect(jsonPath("$.last_update").value("2026-11-05T14:49:10Z"));
    }

    @Test
    void remainingIsNotClampedWhenProducedAboveRequested() throws Exception {
        postSingle("""
                {"plc_id":"CLP2","reactor_id":"R371","op_code":"OP1","requested_weight_kg":5000,
                 "produced_weight_kg":5120,"timestamp":"2026-11-05T14:49:00Z"}""")
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.reactors[0].remaining_weight_kg").value(-120.0));
    }

    @Test
    void singleUpdatesOneReactor() throws Exception {
        postSingle("""
                {"plc_id":"CLP2","reactor_id":"R371","op_code":"AEMEMC01001","requested_weight_kg":5000,
                 "produced_weight_kg":3301,"current_step":1,"timestamp":"2026-11-05T14:49:44Z"}""")
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.total_active_ops").value(1))
                .andExpect(jsonPath("$.reactors[0].op_code").value("AEMEMC01001"))
                .andExpect(jsonPath("$.reactors[0].remaining_weight_kg").value(1699.0));
    }

    @Test
    void sameReactorOnDifferentPlcsDoesNotCollide() throws Exception {
        for (String plc : new String[]{"CLP1", "CLP2"}) {
            postBatch("""
                    {"plc_id":"%s","timestamp":"2026-11-05T14:49:00Z","reactors":[{"reactor_id":"R280","op_code":"OP-%s"}]}"""
                    .formatted(plc, plc))
                    .andExpect(status().isNoContent());
        }

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.total_active_ops").value(2));
    }

    @Test
    void invalidPayloadsReturn400() throws Exception {
        // Sem reactor_id.
        postSingle("""
                {"plc_id":"CLP2","op_code":"X","timestamp":"2026-11-05T14:49:00Z"}""")
                .andExpect(status().isBadRequest());

        // Envio individual sem timestamp: a API não inventa a hora da leitura.
        postSingle("""
                {"plc_id":"CLP2","reactor_id":"R210","op_code":"X"}""")
                .andExpect(status().isBadRequest());

        // Envio individual sem plc_id.
        postSingle("""
                {"reactor_id":"R210","op_code":"X","timestamp":"2026-11-05T14:49:00Z"}""")
                .andExpect(status().isBadRequest());

        // Tanque repetido no lote.
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:00Z","reactors":[
                  {"reactor_id":"R210","op_code":""},{"reactor_id":"R210","op_code":""}]}""")
                .andExpect(status().isBadRequest());

        // Relógio do gateway 1 h adiantado.
        postBatch("""
                {"plc_id":"CLP2","timestamp":"2026-11-05T15:50:00Z","reactors":[{"reactor_id":"R210","op_code":"X"}]}""")
                .andExpect(status().isBadRequest());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.total_active_ops").value(0));
    }

    private ResultActions postBatch(String json) throws Exception {
        return mvc.perform(post("/api/v1/telemetry/reactors/batch").contentType(MediaType.APPLICATION_JSON).content(json));
    }

    private ResultActions postSingle(String json) throws Exception {
        return mvc.perform(post("/api/v1/telemetry/reactors").contentType(MediaType.APPLICATION_JSON).content(json));
    }

    /** Relógio controlado pelo teste. */
    static final class MutableClock extends Clock {
        private volatile Instant now;

        MutableClock(Instant now) {
            this.now = now;
        }

        void advance(Duration duration) {
            now = now.plus(duration);
        }

        @Override
        public Instant instant() {
            return now;
        }

        @Override
        public ZoneId getZone() {
            return ZoneOffset.UTC;
        }

        @Override
        public Clock withZone(ZoneId zone) {
            return this;
        }
    }
}
