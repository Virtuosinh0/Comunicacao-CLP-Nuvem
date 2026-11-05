package com.clpnuvem.api.dto.production;

import java.time.Instant;
import java.util.List;

/** GET /api/v1/production/reactors */
public record ReactorsResponse(int totalActiveOps, Instant lastUpdate, List<ReactorView> reactors) {
}
