package com.clpnuvem.api.configurations;

import org.springframework.boot.context.properties.ConfigurationProperties;

import java.time.Duration;

/**
 * Seção "telemetry" do application.properties.
 *
 * @param staleAfter   sem leitura nova há mais que isso → tanque marcado como desatualizado (stale)
 * @param maxClockSkew timestamp de leitura mais no futuro que isso é rejeitado
 */
@ConfigurationProperties(prefix = "telemetry")
public record TelemetryProperties(Duration staleAfter, Duration maxClockSkew) {

    public TelemetryProperties {
        if (staleAfter == null) staleAfter = Duration.ofSeconds(30);
        if (maxClockSkew == null) maxClockSkew = Duration.ofMinutes(5);
    }
}
