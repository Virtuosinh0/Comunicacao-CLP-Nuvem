package com.clpnuvem.api.controllers.production;

import com.clpnuvem.api.dtos.production.ReactorsResponseDto;
import com.clpnuvem.api.services.production.ProductionService;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Consultas usadas pelo Web App. Só IPs de security.allowed-ips.viewers. */
@RestController
@RequestMapping("/api/v1/production")
public class ProductionController {

    private final ProductionService service;

    public ProductionController(ProductionService service) {
        this.service = service;
    }

    @GetMapping("/reactors")
    public ReactorsResponseDto reactors() {
        return service.activeReactors();
    }
}
