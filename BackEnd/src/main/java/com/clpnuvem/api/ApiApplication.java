package com.clpnuvem.api;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.autoconfigure.security.servlet.UserDetailsServiceAutoConfiguration;

// Sem usuários/senhas: a segurança é por IP (infra/security). Exclui o usuário "user" com senha gerada do Spring.
@SpringBootApplication(exclude = UserDetailsServiceAutoConfiguration.class)
public class ApiApplication {

    public static void main(String[] args) {
        SpringApplication.run(ApiApplication.class, args);
    }
}
