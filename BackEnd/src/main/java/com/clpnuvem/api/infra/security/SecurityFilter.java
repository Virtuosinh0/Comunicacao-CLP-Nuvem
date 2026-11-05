package com.clpnuvem.api.infra.security;

import com.clpnuvem.api.dtos.common.ErrorResponseDto;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.servlet.FilterChain;
import jakarta.servlet.ServletException;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.security.web.util.matcher.IpAddressMatcher;
import org.springframework.stereotype.Component;
import org.springframework.web.filter.OncePerRequestFilter;

import java.io.IOException;
import java.util.List;

/**
 * Primeira barreira: só passa requisição vinda de IP autorizado (security.allowed-ips).
 *  - /api/v1/telemetry/** (envio de leituras) → lista "gateway"
 *  - todo o resto (tela, consultas)          → lista "viewers"
 *
 * O IP é o da conexão TCP (request.getRemoteAddr()). O cabeçalho X-Forwarded-For NÃO é usado:
 * qualquer um pode escrevê-lo e se passar por um IP autorizado.
 */
@Component
public class SecurityFilter extends OncePerRequestFilter {

    private static final Logger logger = LoggerFactory.getLogger(SecurityFilter.class);
    private static final String TELEMETRY_PATH = "/api/v1/telemetry/";

    private final List<IpAddressMatcher> gateway;
    private final List<IpAddressMatcher> viewers;
    private final ObjectMapper objectMapper;

    public SecurityFilter(AllowedIpsProperties properties, ObjectMapper objectMapper) {
        // IP/faixa mal escrita no application.properties derruba a API na inicialização (falha rápido, não libera nada).
        this.gateway = toMatchers("gateway", properties.gateway());
        this.viewers = toMatchers("viewers", properties.viewers());
        this.objectMapper = objectMapper;
    }

    @Override
    protected void doFilterInternal(HttpServletRequest request, HttpServletResponse response, FilterChain filterChain)
            throws ServletException, IOException {

        String path = request.getRequestURI().substring(request.getContextPath().length());
        boolean isTelemetry = path.startsWith(TELEMETRY_PATH);
        String ipAddress = request.getRemoteAddr();

        if (!isAllowed(ipAddress, isTelemetry ? gateway : viewers)) {
            logger.warn("ACESSO BLOQUEADO: IP fora da lista \"{}\". IP: {}, Método: {}, URL: {}",
                    isTelemetry ? "gateway" : "viewers", ipAddress, request.getMethod(), path);

            response.setStatus(HttpStatus.FORBIDDEN.value());
            response.setContentType(MediaType.APPLICATION_JSON_VALUE);
            objectMapper.writeValue(response.getWriter(), new ErrorResponseDto("Acesso negado"));
            return;
        }

        filterChain.doFilter(request, response);
    }

    private static boolean isAllowed(String ipAddress, List<IpAddressMatcher> allowed) {
        try {
            return allowed.stream().anyMatch(matcher -> matcher.matches(ipAddress));
        } catch (IllegalArgumentException ex) {
            return false; // endereço de origem ilegível: nega
        }
    }

    private static List<IpAddressMatcher> toMatchers(String listName, List<String> entries) {
        if (entries.isEmpty()) {
            logger.warn("security.allowed-ips.{} está vazia: ninguém terá acesso a essa parte da API.", listName);
        }
        return entries.stream()
                .map(String::trim)
                .map(entry -> {
                    try {
                        return new IpAddressMatcher(entry);
                    } catch (IllegalArgumentException ex) {
                        throw new IllegalStateException(
                                "security.allowed-ips." + listName + ": '" + entry + "' não é um IP/faixa válido", ex);
                    }
                })
                .toList();
    }
}
