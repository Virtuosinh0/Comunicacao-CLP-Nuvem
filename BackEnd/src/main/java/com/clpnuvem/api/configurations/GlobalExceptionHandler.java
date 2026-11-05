package com.clpnuvem.api.configurations;

import com.clpnuvem.api.dtos.common.ErrorResponseDto;
import org.springframework.http.HttpStatus;
import org.springframework.web.bind.MethodArgumentNotValidException;
import org.springframework.web.bind.annotation.ExceptionHandler;
import org.springframework.web.bind.annotation.ResponseStatus;
import org.springframework.web.bind.annotation.RestControllerAdvice;

@RestControllerAdvice
public class GlobalExceptionHandler {

    /** Payload com regra de negócio inválida (ex.: reactor_id repetido no lote) → 400. */
    @ExceptionHandler(IllegalArgumentException.class)
    @ResponseStatus(HttpStatus.BAD_REQUEST)
    public ErrorResponseDto badRequest(IllegalArgumentException ex) {
        return new ErrorResponseDto(ex.getMessage());
    }

    /** Campo obrigatório ausente/inválido no JSON (Bean Validation) → 400, dizendo qual campo. */
    @ExceptionHandler(MethodArgumentNotValidException.class)
    @ResponseStatus(HttpStatus.BAD_REQUEST)
    public ErrorResponseDto validationError(MethodArgumentNotValidException ex) {
        String message = ex.getBindingResult().getFieldErrors().stream()
                .map(error -> error.getField() + ": " + error.getDefaultMessage())
                .findFirst()
                .orElse("Payload inválido");
        return new ErrorResponseDto(message);
    }
}
