package com.clpnuvem.api.controller;

import com.clpnuvem.api.dto.telemetry.ReactorBatchTelemetry;
import com.clpnuvem.api.dto.telemetry.ReactorTelemetry;
import com.clpnuvem.api.service.TelemetryService;
import jakarta.validation.Valid;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Ingestão de dados enviados pelo DataManager (gateway local). */
@RestController
@RequestMapping("/api/v1/telemetry/reactors")
public class TelemetryController {

    private final TelemetryService service;

    public TelemetryController(TelemetryService service) {
        this.service = service;
    }

    /** Uma OP por requisição. */
    @PostMapping
    public ResponseEntity<Void> receiveReactor(@Valid @RequestBody ReactorTelemetry payload) {
        service.receiveReactor(payload);
        return ResponseEntity.noContent().build();
    }

    /** Todas as OPs de um CLP em uma requisição. */
    @PostMapping("/batch")
    public ResponseEntity<Void> receiveBatch(@Valid @RequestBody ReactorBatchTelemetry payload) {
        service.receiveBatch(payload);
        return ResponseEntity.noContent().build();
    }
}
