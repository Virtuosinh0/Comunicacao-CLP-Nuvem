package com.clpnuvem.api.controller;

import com.clpnuvem.api.dto.production.ReactorsResponse;
import com.clpnuvem.api.service.ProductionService;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Consultas usadas pelo Web App. */
@RestController
@RequestMapping("/api/v1/production")
public class ProductionController {

    private final ProductionService service;

    public ProductionController(ProductionService service) {
        this.service = service;
    }

    @GetMapping("/reactors")
    public ReactorsResponse reactors() {
        return service.activeReactors();
    }
}
