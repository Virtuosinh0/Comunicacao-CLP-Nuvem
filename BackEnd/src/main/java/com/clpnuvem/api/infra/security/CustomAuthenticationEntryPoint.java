package com.clpnuvem.api.infra.security;

import com.clpnuvem.api.dtos.common.ErrorResponseDto;
import com.fasterxml.jackson.databind.ObjectMapper;
import jakarta.servlet.http.HttpServletRequest;
import jakarta.servlet.http.HttpServletResponse;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.http.HttpStatus;
import org.springframework.http.MediaType;
import org.springframework.security.core.AuthenticationException;
import org.springframework.security.web.AuthenticationEntryPoint;
import org.springframework.stereotype.Component;

import java.io.IOException;

/**
 * IP autorizado, mas rota que não existe/não é liberada (anyRequest().denyAll()).
 * Responde 403 (e não 401): a API não tem login, então não há "credencial" a pedir.
 */
@Component
public class CustomAuthenticationEntryPoint implements AuthenticationEntryPoint {

    private static final Logger logger = LoggerFactory.getLogger(CustomAuthenticationEntryPoint.class);

    private final ObjectMapper objectMapper;

    public CustomAuthenticationEntryPoint(ObjectMapper objectMapper) {
        this.objectMapper = objectMapper;
    }

    @Override
    public void commence(HttpServletRequest request, HttpServletResponse response,
                         AuthenticationException authException) throws IOException {
        logger.warn("ACESSO BLOQUEADO: rota não liberada. IP: {}, Método: {}, URL: {}",
                request.getRemoteAddr(), request.getMethod(), request.getRequestURI());

        response.resetBuffer();
        response.setStatus(HttpStatus.FORBIDDEN.value());
        response.setContentType(MediaType.APPLICATION_JSON_VALUE);
        objectMapper.writeValue(response.getWriter(), new ErrorResponseDto("Acesso negado"));
        response.flushBuffer();
    }
}
