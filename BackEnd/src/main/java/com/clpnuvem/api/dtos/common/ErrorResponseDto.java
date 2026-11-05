package com.clpnuvem.api.dtos.common;

/** Corpo de toda resposta de erro da API (400, 403...). */
public record ErrorResponseDto(
        String message
) {
}
