package com.clpnuvem.api.infra.security;

import org.springframework.boot.web.servlet.FilterRegistrationBean;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.HttpMethod;
import org.springframework.security.config.annotation.web.builders.HttpSecurity;
import org.springframework.security.config.annotation.web.configuration.EnableWebSecurity;
import org.springframework.security.config.http.SessionCreationPolicy;
import org.springframework.security.web.SecurityFilterChain;
import org.springframework.security.web.context.SecurityContextHolderFilter;

/**
 * Regras de acesso. Duas camadas:
 *  1. SecurityFilter: o IP de origem precisa estar em security.allowed-ips (gateway ou viewers).
 *  2. Abaixo: só as rotas listadas existem; qualquer outra → 403 (anyRequest().denyAll()).
 *
 * Sem login, sessão ou cookie: CSRF desligado (não há sessão a ser sequestrada) e CORS fechado
 * (a tela é servida pela própria API ou, em desenvolvimento, pelo proxy do Vite — mesma origem).
 */
@Configuration
@EnableWebSecurity
public class SecurityConfigurations {

    private final SecurityFilter securityFilter;
    private final CustomAuthenticationEntryPoint customAuthenticationEntryPoint;
    private final CustomAccessDeniedHandler customAccessDeniedHandler;

    public SecurityConfigurations(SecurityFilter securityFilter,
                                  CustomAuthenticationEntryPoint customAuthenticationEntryPoint,
                                  CustomAccessDeniedHandler customAccessDeniedHandler) {
        this.securityFilter = securityFilter;
        this.customAuthenticationEntryPoint = customAuthenticationEntryPoint;
        this.customAccessDeniedHandler = customAccessDeniedHandler;
    }

    @Bean
    public SecurityFilterChain securityFilterChain(HttpSecurity httpSecurity) throws Exception {
        return httpSecurity
                .csrf(csrf -> csrf.disable())
                .httpBasic(basic -> basic.disable())
                .formLogin(form -> form.disable())
                .logout(logout -> logout.disable())
                .requestCache(cache -> cache.disable())
                .sessionManagement(session -> session.sessionCreationPolicy(SessionCreationPolicy.STATELESS))
                .exceptionHandling(ex -> ex
                        .authenticationEntryPoint(customAuthenticationEntryPoint)
                        .accessDeniedHandler(customAccessDeniedHandler)
                )
                .authorizeHttpRequests(authorize -> authorize

                        // DataManager → API (IP da lista "gateway", conferido no SecurityFilter)
                        .requestMatchers(HttpMethod.POST, "/api/v1/telemetry/reactors").permitAll()
                        .requestMatchers(HttpMethod.POST, "/api/v1/telemetry/reactors/batch").permitAll()

                        // Tela → API (IP da lista "viewers")
                        .requestMatchers(HttpMethod.GET, "/api/v1/production/reactors").permitAll()

                        // Tela compilada (FrontEnd/dist) servida pela própria API no Serviço Windows
                        .requestMatchers(HttpMethod.GET, "/", "/index.html", "/assets/**",
                                "/favicon.ico", "/favicon.svg", "/apple-touch-icon.png").permitAll()

                        // Página de erro interna do Spring (ex.: JSON malformado → 400)
                        .requestMatchers("/error").permitAll()

                        .anyRequest().denyAll()

                ).addFilterBefore(securityFilter, SecurityContextHolderFilter.class).build();
    }

    /**
     * O SecurityFilter é um @Component: sem isto o Spring Boot o registraria uma segunda vez,
     * fora da cadeia de segurança. Ele deve rodar só dentro dela (acima).
     */
    @Bean
    public FilterRegistrationBean<SecurityFilter> securityFilterRegistration(SecurityFilter filter) {
        FilterRegistrationBean<SecurityFilter> registration = new FilterRegistrationBean<>(filter);
        registration.setEnabled(false);
        return registration;
    }
}
