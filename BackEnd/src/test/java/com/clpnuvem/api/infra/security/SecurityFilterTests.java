package com.clpnuvem.api.infra.security;

import com.fasterxml.jackson.databind.ObjectMapper;
import org.junit.jupiter.api.Test;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.boot.test.autoconfigure.web.servlet.AutoConfigureMockMvc;
import org.springframework.boot.test.context.SpringBootTest;
import org.springframework.http.MediaType;
import org.springframework.test.web.servlet.MockMvc;
import org.springframework.test.web.servlet.request.MockHttpServletRequestBuilder;
import org.springframework.test.web.servlet.request.RequestPostProcessor;

import java.time.Instant;
import java.util.List;

import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.get;
import static org.springframework.test.web.servlet.request.MockMvcRequestBuilders.post;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.jsonPath;
import static org.springframework.test.web.servlet.result.MockMvcResultMatchers.status;

/** Lista de IPs: quem envia (gateway) e quem vê (viewers) são separados; o resto é 403. */
@SpringBootTest(properties = {
        "security.allowed-ips.gateway=127.0.0.1,10.0.0.5",
        "security.allowed-ips.viewers=127.0.0.1,192.168.10.0/24"
})
@AutoConfigureMockMvc
class SecurityFilterTests {

    static final String GATEWAY_IP = "10.0.0.5";
    static final String VIEWER_IP = "192.168.10.20";
    static final String UNKNOWN_IP = "8.8.8.8";

    @Autowired
    private MockMvc mvc;

    @Test
    void viewerCanReadButCannotSendReadings() throws Exception {
        mvc.perform(get("/api/v1/production/reactors").with(from(VIEWER_IP)))
                .andExpect(status().isOk());

        // Um PC da lista "viewers" não pode injetar leitura falsa.
        mvc.perform(batch().with(from(VIEWER_IP)))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.message").value("Acesso negado"));
    }

    @Test
    void gatewayCanSendButCannotRead() throws Exception {
        mvc.perform(batch().with(from(GATEWAY_IP)))
                .andExpect(status().isNoContent());

        mvc.perform(get("/api/v1/production/reactors").with(from(GATEWAY_IP)))
                .andExpect(status().isForbidden());
    }

    @Test
    void unknownIpIsBlockedEverywhere() throws Exception {
        mvc.perform(get("/api/v1/production/reactors").with(from(UNKNOWN_IP)))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.message").value("Acesso negado"));
        mvc.perform(get("/").with(from(UNKNOWN_IP)))
                .andExpect(status().isForbidden());
        mvc.perform(batch().with(from(UNKNOWN_IP)))
                .andExpect(status().isForbidden());
    }

    @Test
    void forwardedForHeaderCannotFakeTheOrigin() throws Exception {
        mvc.perform(get("/api/v1/production/reactors").with(from(UNKNOWN_IP)).header("X-Forwarded-For", "127.0.0.1"))
                .andExpect(status().isForbidden());
    }

    @Test
    void allowedIpOnUnlistedRouteIsBlocked() throws Exception {
        mvc.perform(get("/api/v1/qualquer-coisa").with(from(VIEWER_IP)))
                .andExpect(status().isForbidden())
                .andExpect(jsonPath("$.message").value("Acesso negado"));
        // Método não liberado na rota de leitura.
        mvc.perform(post("/api/v1/production/reactors").with(from(VIEWER_IP)))
                .andExpect(status().isForbidden());
    }

    @Test
    void invalidAllowedIpEntryFailsAtStartup() {
        AllowedIpsProperties properties = new AllowedIpsProperties(List.of("127.0.0.1", "192.168.0.300"), List.of());
        assertThrows(IllegalStateException.class, () -> new SecurityFilter(properties, new ObjectMapper()));
    }

    private static MockHttpServletRequestBuilder batch() {
        return post("/api/v1/telemetry/reactors/batch")
                .contentType(MediaType.APPLICATION_JSON)
                .content("""
                        {"plc_id":"CLP2","timestamp":"%s","reactors":[{"reactor_id":"R210","op_code":""}]}"""
                        .formatted(Instant.now()));
    }

    private static RequestPostProcessor from(String ip) {
        return request -> {
            request.setRemoteAddr(ip);
            return request;
        };
    }
}
