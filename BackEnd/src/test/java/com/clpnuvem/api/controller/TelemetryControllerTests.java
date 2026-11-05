package com.clpnuvem.api.controller;

import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.annotation.DirtiesContext;
import org.springframework.test.web.servlet.MockMvc;

import static org.hamcrest.Matchers.closeTo;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

@SpringBootTest
@AutoConfigureMockMvc
@DirtiesContext(classMode = DirtiesContext.ClassMode.AFTER_EACH_TEST_METHOD)
class TelemetryControllerTests {

    @Autowired
    private MockMvc mvc;

    @Test
    void batchReplacesPlcState() throws Exception {
        String first = """
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:07Z","reactors":[
                  {"reactor_id":"R210","op_code":"AEMEKL01001","product_code":"P62475","status":2,"priority":999,
                   "requested_weight_kg":6500,"produced_weight_kg":3767.4,"current_step":3,
                   "start_time":"2026-11-05T14:14:47.5815014Z","final_destination":"R210"},
                  {"reactor_id":"R211","op_code":""},
                  {"reactor_id":"R371","op_code":"AEMEMC01001","requested_weight_kg":5000,"produced_weight_kg":4661}
                ]}""";

        mvc.perform(post("/api/v1/telemetry/reactors/batch").contentType(MediaType.APPLICATION_JSON).content(first))
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(status().isOk())
                .andExpect(jsonPath("$.total_active_ops").value(2))
                .andExpect(jsonPath("$.last_update").value("2026-11-05T14:49:07Z"))
                .andExpect(jsonPath("$.reactors[0].reactor_id").value("R210"))
                .andExpect(jsonPath("$.reactors[0].plc_id").value("CLP2"))
                .andExpect(jsonPath("$.reactors[0].remaining_weight_kg").value(closeTo(2732.6, 0.001)))
                .andExpect(jsonPath("$.reactors[1].reactor_id").value("R371"));

        // R371 terminou (op_code vazio): deve sair da lista.
        String second = """
                {"plc_id":"CLP2","timestamp":"2026-11-05T14:49:12Z","reactors":[
                  {"reactor_id":"R210","op_code":"AEMEKL01001","requested_weight_kg":6500,"produced_weight_kg":3782.5},
                  {"reactor_id":"R211","op_code":""},
                  {"reactor_id":"R371","op_code":""}
                ]}""";

        mvc.perform(post("/api/v1/telemetry/reactors/batch").contentType(MediaType.APPLICATION_JSON).content(second))
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.total_active_ops").value(1))
                .andExpect(jsonPath("$.reactors[0].produced_weight_kg").value(3782.5));
    }

    @Test
    void singleUpdatesOneReactor() throws Exception {
        String op = """
                {"plc_id":"CLP2","reactor_id":"R371","op_code":"AEMEMC01001","requested_weight_kg":5000,
                 "produced_weight_kg":3301,"current_step":1,"timestamp":"2026-11-05T14:02:44Z"}""";

        mvc.perform(post("/api/v1/telemetry/reactors").contentType(MediaType.APPLICATION_JSON).content(op))
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.total_active_ops").value(1))
                .andExpect(jsonPath("$.reactors[0].op_code").value("AEMEMC01001"))
                .andExpect(jsonPath("$.reactors[0].remaining_weight_kg").value(1699.0));
    }

    @Test
    void sameReactorOnDifferentPlcsDoesNotCollide() throws Exception {
        for (String plc : new String[]{"CLP1", "CLP2"}) {
            String batch = """
                    {"plc_id":"%s","timestamp":"2026-11-05T14:00:00Z","reactors":[{"reactor_id":"R280","op_code":"OP-%s"}]}"""
                    .formatted(plc, plc);
            mvc.perform(post("/api/v1/telemetry/reactors/batch").contentType(MediaType.APPLICATION_JSON).content(batch))
                    .andExpect(status().isNoContent());
        }

        mvc.perform(get("/api/v1/production/reactors"))
                .andExpect(jsonPath("$.total_active_ops").value(2));
    }

    @Test
    void invalidPayloadsReturn400() throws Exception {
        mvc.perform(post("/api/v1/telemetry/reactors").contentType(MediaType.APPLICATION_JSON)
                        .content("{\"op_code\":\"X\"}"))
                .andExpect(status().isBadRequest());

        mvc.perform(post("/api/v1/telemetry/reactors/batch").contentType(MediaType.APPLICATION_JSON)
                        .content("""
                                {"plc_id":"CLP2","timestamp":"2026-11-05T14:00:00Z","reactors":[
                                  {"reactor_id":"R210","op_code":""},{"reactor_id":"R210","op_code":""}]}"""))
                .andExpect(status().isBadRequest());
    }
}
