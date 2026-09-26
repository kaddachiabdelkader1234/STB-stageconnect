package com.smartek.authservice.entity;

import com.smartek.authservice.enums.RoleType;
import jakarta.persistence.*;
import jakarta.validation.constraints.Email;
import jakarta.validation.constraints.NotBlank;
import jakarta.validation.constraints.Size;
import lombok.AllArgsConstructor;
import lombok.Builder;
import lombok.Data;
import lombok.NoArgsConstructor;

/**
 * Entité User pour la plateforme SMARTEK
 */
@Entity
@Table(name = "users", uniqueConstraints = {
    @UniqueConstraint(columnNames = "email")
})
@Data
@NoArgsConstructor
@AllArgsConstructor
@Builder
public class User {
    
    @Id
    @GeneratedValue(strategy = GenerationType.IDENTITY)
    private Long userId;
    
    @Lob
    @Column(name = "image", columnDefinition = "BLOB")
    private byte[] image;
    
    @NotBlank(message = "Le prénom est obligatoire")
    @Size(max = 50)
    @Column(nullable = false, length = 50)
    private String firstName;

    /**
     * Nom de famille. Nullable in the database: accounts created before this field existed
     * (and the seeded admin) have no last name until the profile is completed.
     */
    @Size(max = 50)
    @Column(name = "last_name", length = 50)
    private String lastName;
    
    @NotBlank(message = "L'email est obligatoire")
    @Email(message = "Format d'email invalide")
    @Size(max = 100)
    @Column(nullable = false, unique = true, length = 100)
    private String email;
    
    @NotBlank(message = "Le mot de passe est obligatoire")
    @Size(min = 8, message = "Le mot de passe doit contenir au moins 8 caractères")
    @Column(nullable = false)
    private String password;
    
    @Column(length = 20)
    private String phone;
    
    @Column(columnDefinition = "INT DEFAULT 0")
    private Integer experience = 0;
    
    @Enumerated(EnumType.STRING)
    @Column(nullable = false, length = 30, columnDefinition = "VARCHAR(30)")
    private RoleType role;

    /**
     * Refresh token for the silent re-login flow. Set on login/register, replaced on each
     * refresh. Nullable — a user without one must re-authenticate from scratch.
     */
    @Column(length = 512)
    private String refreshToken;

    /**
     * Account creation timestamp. Nullable column: pre-existing rows are backfilled to the
     * migration time by DDL default rather than being given a fake historical date.
     */
    @Column(name = "created_at", updatable = false)
    private java.time.Instant createdAt;

    @Column(name = "reset_password_token", length = 255)
    private String resetPasswordToken;

    @Column(name = "reset_password_expires_at")
    private java.time.Instant resetPasswordExpiresAt;

    @PrePersist
    void onCreate() {
        if (createdAt == null) {
            createdAt = java.time.Instant.now();
        }
    }
}
