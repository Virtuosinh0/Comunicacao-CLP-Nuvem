package com.clpnuvem.api.infra.security;

import org.springframework.boot.context.properties.ConfigurationProperties;

import java.util.List;

/**
 * Seção "security.allowed-ips" do application.properties: quem pode acessar a API.
 * Cada item é um IP (192.168.0.10) ou uma faixa CIDR (192.168.0.0/24), IPv4 ou IPv6.
 *
 * @param gateway IPs que podem ENVIAR leituras (POST /api/v1/telemetry/**): só o(s) DataManager(s)
 * @param viewers IPs que podem VER a tela e consultar (GET /api/v1/production/**, página web)
 */
@ConfigurationProperties(prefix = "security.allowed-ips")
public record AllowedIpsProperties(List<String> gateway, List<String> viewers) {

    public AllowedIpsProperties {
        gateway = gateway == null ? List.of() : List.copyOf(gateway);
        viewers = viewers == null ? List.of() : List.copyOf(viewers);
    }
}
