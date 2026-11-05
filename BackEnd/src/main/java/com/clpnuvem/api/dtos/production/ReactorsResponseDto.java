package com.clpnuvem.api.dtos.production;

import java.time.Instant;
import java.util.List;

/** GET /api/v1/production/reactors */
public record ReactorsResponseDto(int totalActiveOps, Instant lastUpdate, List<ReactorResponseDto> reactors) {
}
