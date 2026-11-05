package com.clpnuvem.api.controllers.telemetry;

import com.clpnuvem.api.dtos.telemetry.ReactorBatchTelemetryRequestDto;
import com.clpnuvem.api.dtos.telemetry.ReactorTelemetryRequestDto;
import com.clpnuvem.api.services.telemetry.TelemetryService;
import jakarta.validation.Valid;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

/** Ingestão de dados enviados pelo DataManager (gateway local). Só IPs de security.allowed-ips.gateway. */
@RestController
@RequestMapping("/api/v1/telemetry/reactors")
public class TelemetryController {

    private final TelemetryService service;

    public TelemetryController(TelemetryService service) {
        this.service = service;
    }

    /** Uma OP por requisição. */
    @PostMapping
    public ResponseEntity<Void> receiveReactor(@Valid @RequestBody ReactorTelemetryRequestDto dto) {
        service.receiveReactor(dto);
        return ResponseEntity.noContent().build();
    }

    /** Todas as OPs de um CLP em uma requisição. */
    @PostMapping("/batch")
    public ResponseEntity<Void> receiveBatch(@Valid @RequestBody ReactorBatchTelemetryRequestDto dto) {
        service.receiveBatch(dto);
        return ResponseEntity.noContent().build();
    }
}
