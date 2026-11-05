package com.clpnuvem.api.config;

import org.springframework.boot.context.properties.EnableConfigurationProperties;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

import java.time.Clock;

@Configuration
@EnableConfigurationProperties(TelemetryProperties.class)
public class AppConfig {

    /** Relógio da API (injetável para os testes controlarem o tempo). */
    @Bean
    public Clock clock() {
        return Clock.systemUTC();
    }
}
