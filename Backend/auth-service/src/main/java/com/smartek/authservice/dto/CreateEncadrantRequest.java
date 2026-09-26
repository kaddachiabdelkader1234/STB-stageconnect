package com.smartek.authservice.dto;

import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class CreateEncadrantRequest {

    @NotBlank(message = "Le prénom est obligatoire")
    @Size(max = 50)
    private String firstName;

    /** Nom de famille — optional so legacy callers keep working. */
    @Size(max = 50)
    private String lastName;

    @NotBlank(message = "L'email est obligatoire")
    @Email(message = "Format d'email invalide")
    private String email;

    /** Téléphone professionnel de l'encadrant — informational, optional. */
    @Size(max = 20)
    private String phone;

    @NotBlank(message = "Le département est obligatoire")
    private String departement;
}
