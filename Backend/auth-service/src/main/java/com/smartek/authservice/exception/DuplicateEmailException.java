package com.smartek.authservice.exception;

/**
 * Thrown when a register or encadrant-creation attempt uses an email that already exists.
 *
 * Maps to HTTP 409 CONFLICT at the controller layer so the frontend shows the real reason
 * ("Cet email est déjà utilisé") instead of the generic 400 "Création impossible" — which
 * made a duplicate email look like a validation failure and sent the admin hunting for a
 * form mistake that did not exist.
 */
public class DuplicateEmailException extends RuntimeException {
    public DuplicateEmailException(String message) {
        super(message);
    }
}
