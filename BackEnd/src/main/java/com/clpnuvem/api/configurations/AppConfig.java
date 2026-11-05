package com.clpnuvem.api.configurations;

import com.clpnuvem.api.infra.security.AllowedIpsProperties;
import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.time.Clock;

@Configuration
@EnableConfigurationProperties({TelemetryProperties.class, AllowedIpsProperties.class})
public class AppConfig {

    /** Relógio da API (injetável para os testes controlarem o tempo). */
    @Bean
    public Clock clock() {
        return Clock.systemUTC();
    }
}
