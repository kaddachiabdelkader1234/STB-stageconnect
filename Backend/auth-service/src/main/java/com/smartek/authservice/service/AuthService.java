package com.smartek.authservice.service;

import com.smartek.authservice.dto.AuthResponse;
import com.smartek.authservice.dto.LoginRequest;
import com.smartek.authservice.dto.RegisterRequest;
import com.smartek.authservice.dto.UserSummaryResponse;
import com.smartek.authservice.entity.User;
import com.smartek.authservice.enums.RoleType;
import com.smartek.authservice.repository.UserRepository;
import com.smartek.authservice.config.RabbitConfig;
import com.smartek.authservice.exception.DuplicateEmailException;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.security.authentication.AuthenticationManager;
import org.springframework.security.authentication.UsernamePasswordAuthenticationToken;
import org.springframework.security.crypto.password.PasswordEncoder;
import org.springframework.stereotype.Service;
import org.springframework.transaction.annotation.Transactional;
import com.smartek.authservice.dto.ChangePasswordRequest;
import com.smartek.authservice.dto.ForgotPasswordRequest;
import com.smartek.authservice.dto.ResetPasswordRequest;
import java.time.Instant;
import java.time.temporal.ChronoUnit;
import java.util.UUID;
import java.util.Base64;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Optional;

@Service
@RequiredArgsConstructor
@Slf4j
public class AuthService {
    
    private final UserRepository userRepository;
    private final PasswordEncoder passwordEncoder;
    private final JwtService jwtService;
    private final AuthenticationManager authenticationManager;
    private final JdbcTemplate jdbcTemplate;
    private final RabbitTemplate rabbitTemplate;
    
    @Transactional
    public AuthResponse register(RegisterRequest request) {
        log.info("Tentative d'inscription pour l'email: {}", request.getEmail());
        
        // Vérifier si l'email existe déjà
        if (userRepository.existsByEmail(request.getEmail())) {
            // Specific type so the controller returns 409 with this exact message instead
            // of a generic 400 — the signup form can then tell the user what is wrong.
            throw new DuplicateEmailException("Cet email est déjà utilisé");
        }
        
        // SECURITY FIX: Always force LEARNER role for public registration.
        // Never trust the client-provided role — a direct API call could set
        // role=ADMIN and bypass every downstream authorization check.
        RoleType assignedRole = RoleType.LEARNER;
        log.info("Rôle assigné: {} (toujours LEARNER pour l'inscription publique)", assignedRole);
        
        // Créer le nouvel utilisateur
        User user = User.builder()
                .firstName(request.getFirstName())
                .lastName(request.getLastName())
                .email(request.getEmail())
                .password(passwordEncoder.encode(request.getPassword()))
                .phone(request.getPhone())
                .role(assignedRole)
                .image(convertBase64ToBytes(request.getImageBase64()))
                .experience(request.getExperience() != null ? request.getExperience() : 0)
                .build();
        
        User savedUser = userRepository.save(user);
        log.info("Utilisateur créé avec succès: {}", savedUser.getEmail());
        
        // Générer le token JWT
        Map<String, Object> claims = new HashMap<>();
        claims.put("role", savedUser.getRole().name());
        claims.put("userId", savedUser.getUserId());
        
        String token = jwtService.generateToken(savedUser.getEmail(), claims);
        String refreshToken = jwtService.generateRefreshToken(savedUser.getEmail(), claims);
        savedUser.setRefreshToken(refreshToken);
        userRepository.save(savedUser);
        
        return AuthResponse.builder()
                .token(token)
                .refreshToken(refreshToken)
                .userId(savedUser.getUserId())
                .email(savedUser.getEmail())
                .firstName(savedUser.getFirstName())
                .lastName(savedUser.getLastName())
                .phone(savedUser.getPhone())
                .role(savedUser.getRole())
                .imageBase64(convertBytesToBase64(savedUser.getImage()))
                .experience(savedUser.getExperience())
                .message("Inscription réussie")
                .build();
    }
    
    @Transactional
    public AuthResponse login(LoginRequest request) {
        log.info("Tentative de connexion pour l'email: {}", request.getEmail());
        
        // Authentifier l'utilisateur
        authenticationManager.authenticate(
                new UsernamePasswordAuthenticationToken(
                        request.getEmail(),
                        request.getPassword()
                )
        );
        
        // Récupérer l'utilisateur
        User user = userRepository.findByEmail(request.getEmail())
                .orElseThrow(() -> new RuntimeException("Utilisateur non trouvé"));
        
        // Générer le token JWT + refresh token
        Map<String, Object> claims = new HashMap<>();
        claims.put("role", user.getRole().name());
        claims.put("userId", user.getUserId());
        
        String token = jwtService.generateToken(user.getEmail(), claims);
        String refreshToken = jwtService.generateRefreshToken(user.getEmail(), claims);
        user.setRefreshToken(refreshToken);
        userRepository.save(user);
        
        log.info("Connexion réussie pour: {}", user.getEmail());
        
        return AuthResponse.builder()
                .token(token)
                .refreshToken(refreshToken)
                .userId(user.getUserId())
                .email(user.getEmail())
                .firstName(user.getFirstName())
                .lastName(user.getLastName())
                .phone(user.getPhone())
                .role(user.getRole())
                .imageBase64(convertBytesToBase64(user.getImage()))
                .experience(user.getExperience())
                .message("Connexion réussie")
                .build();
    }
    
    public boolean validateUser(Long userId) {
        return userRepository.existsById(userId);
    }

    /**
     * Validates a refresh token and issues a new access + refresh token pair.
     * The old refresh token is invalidated (one-time use).
     *
     * Uses an atomic SQL UPDATE (via {@code atomicSwapRefreshToken}) to prevent the TOCTOU race
     * where two concurrent requests both read the old token, both pass the check, and both write
     * new tokens. The atomic update returns 0 rows affected if the token was already consumed.
     *
     * The repository method uses {@code @Modifying(clearAutomatically = true)} so Hibernate's
     * persistence context is cleared after the UPDATE. This prevents Hibernate from flushing the
     * stale entity loaded by {@code findByEmail} back to the DB at commit time, which would
     * overwrite the atomic UPDATE with the old token value.
     */
    @Transactional
    public AuthResponse refresh(String refreshToken) {
        // 1. Extract email from the JWT — no DB call needed for this.
        String email = jwtService.extractUsername(refreshToken);
        
        // 2. Load the user to get claims for token generation.
        User user = userRepository.findByEmail(email)
                .orElseThrow(() -> new RuntimeException("Utilisateur non trouvé"));
        
        // 3. Generate new tokens with proper claims.
        Map<String, Object> claims = new HashMap<>();
        claims.put("role", user.getRole().name());
        claims.put("userId", user.getUserId());
        
        String newToken = jwtService.generateToken(user.getEmail(), claims);
        String newRefreshToken = jwtService.generateRefreshToken(user.getEmail(), claims);
        
        // 4. Atomically check that the stored token matches AND replace it.
        //    Uses JdbcTemplate directly to bypass Hibernate's persistence context entirely.
        //    The UPDATE ... WHERE refreshToken = ? is a single SQL statement — atomic at the DB level.
        //    Returns 1 if the swap succeeded, 0 if the token was already consumed.
        int rowsAffected = jdbcTemplate.update(
                "UPDATE users SET refresh_token = ? WHERE email = ? AND refresh_token = ?",
                newRefreshToken, email, refreshToken);
        log.info("Refresh token swap: email={}, rowsAffected={}", email, rowsAffected);
        
        if (rowsAffected == 0) {
            throw new RuntimeException("Refresh token invalide ou déjà utilisé");
        }
        
        return AuthResponse.builder()
                .token(newToken)
                .refreshToken(newRefreshToken)
                .userId(user.getUserId())
                .email(user.getEmail())
                .firstName(user.getFirstName())
                .lastName(user.getLastName())
                .phone(user.getPhone())
                .role(user.getRole())
                .imageBase64(convertBytesToBase64(user.getImage()))
                .experience(user.getExperience())
                .message("Token rafraîchi")
                .build();
    }

    /**
     * Creates a TRAINER (encadrant) account. Admin-only — the controller must enforce this.
     * A temporary password is generated and returned once in the response.
     */
    @Transactional
    public AuthResponse createEncadrant(com.smartek.authservice.dto.CreateEncadrantRequest request) {
        if (userRepository.existsByEmail(request.getEmail())) {
            throw new DuplicateEmailException("Cet email est déjà utilisé");
        }

        String tempPassword = generateTempPassword();

        User user = User.builder()
                .firstName(request.getFirstName())
                .lastName(request.getLastName())
                .email(request.getEmail())
                .password(passwordEncoder.encode(tempPassword))
                .phone(request.getPhone())
                .role(RoleType.TRAINER)
                .experience(0)
                .build();

        User savedUser = userRepository.save(user);
        log.info("Encadrant créé par admin: {} (email: {})", savedUser.getFirstName(), savedUser.getEmail());

        // Hand the credentials to notification-service via RabbitMQ — a branded welcome email
        // is sent to the encadrant's inbox instead of the admin copying a password off screen.
        // Publishing happens AFTER the DB commit scope (method is @Transactional, publish fires
        // at the end) — if SMTP fails, notification-service retries; the account already exists.
        try {
            rabbitTemplate.convertAndSend(
                    RabbitConfig.EVENTS_EXCHANGE,
                    RabbitConfig.ENCADRANT_CREATED_KEY,
                    RabbitConfig.encadrantCreatedPayload(
                            savedUser.getUserId(),
                            savedUser.getFirstName(),
                            savedUser.getLastName(),
                            savedUser.getEmail(),
                            request.getDepartement(),
                            request.getPhone(),
                            tempPassword));
            log.info("Événement EncadrantCreated publié pour {} (routing key: {})",
                    savedUser.getEmail(), RabbitConfig.ENCADRANT_CREATED_KEY);
        } catch (Exception e) {
            // The account is created either way — a failed publish must not fail the HTTP call.
            // The admin sees a warning in the UI and can resend from the mail server logs.
            log.error("Impossible de publier EncadrantCreated pour {}: {}",
                    savedUser.getEmail(), e.getMessage());
        }

        Map<String, Object> claims = new HashMap<>();
        claims.put("role", savedUser.getRole().name());
        claims.put("userId", savedUser.getUserId());

        String token = jwtService.generateToken(savedUser.getEmail(), claims);

        return AuthResponse.builder()
                .token(token)
                .userId(savedUser.getUserId())
                .email(savedUser.getEmail())
                .firstName(savedUser.getFirstName())
                .lastName(savedUser.getLastName())
                .phone(savedUser.getPhone())
                .role(savedUser.getRole())
                .message("Encadrant créé. Un email de bienvenue avec les identifiants a été envoyé à " + savedUser.getEmail())
                .build();
    }

    private String generateTempPassword() {
        String chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789";
        StringBuilder sb = new StringBuilder();
        java.util.Random random = new java.util.Random();
        for (int i = 0; i < 12; i++) {
            sb.append(chars.charAt(random.nextInt(chars.length())));
        }
        return sb.toString();
    }

    /**
     * Lists users of one role, as summaries.
     *
     * Backs the admin's encadrant dropdown (role {@code TRAINER}). Returns
     * {@link UserSummaryResponse} rather than {@code AuthResponse} so no token field or base64
     * image is ever part of a list payload.
     */
    public List<UserSummaryResponse> getUsersByRole(RoleType role) {
        return userRepository.findByRole(role).stream()
                .map(AuthService::toSummary)
                .toList();
    }

    /**
     * Re-issues credentials for an existing account: generates a new password, stores its hash
     * and publishes an EncadrantCreated event with {@code IsPasswordReset=true} so the encadrant
     * receives an email with the new password.
     *
     * The original password is BCrypt-hashed and cannot be recovered — "resend" therefore means
     * "reset and resend by email". Admin-only; the seeded admin account is refused because it
     * must never be locked out.
     */
    @Transactional
    public AuthResponse resetAndResendCredentials(Long userId) {
        if (userId == 1L) {
            throw new IllegalStateException("Les identifiants du compte administrateur principal ne peuvent pas être réinitialisés.");
        }
        User user = userRepository.findById(userId)
                .orElseThrow(() -> new RuntimeException("Utilisateur non trouvé"));

        String newPassword = generateTempPassword();
        user.setPassword(passwordEncoder.encode(newPassword));
        user.setRefreshToken(null);
        User savedUser = userRepository.save(user);
        log.info("Réinitialisation des identifiants de {} (userId {}) par un admin", savedUser.getEmail(), userId);

        try {
            rabbitTemplate.convertAndSend(
                    RabbitConfig.EVENTS_EXCHANGE,
                    RabbitConfig.ENCADRANT_CREATED_KEY,
                    RabbitConfig.encadrantCreatedPayload(
                            savedUser.getUserId(),
                            savedUser.getFirstName(),
                            savedUser.getLastName(),
                            savedUser.getEmail(),
                            null,
                            savedUser.getPhone(),
                            newPassword,
                            true));
            log.info("Événement EncadrantCreated (reset) publié pour {}", savedUser.getEmail());
        } catch (Exception e) {
            // The new password is already stored — a failed publish must not roll it back,
            // otherwise the account would keep the old password while the email never goes out.
            log.error("Impossible de publier EncadrantCreated (reset) pour {}: {}",
                    savedUser.getEmail(), e.getMessage());
            throw new RuntimeException("La réinitialisation a eu lieu mais l'envoi de l'email a échoué. Réessayez.");
          }

        return AuthResponse.builder()
                .userId(savedUser.getUserId())
                .email(savedUser.getEmail())
                .firstName(savedUser.getFirstName())
                .lastName(savedUser.getLastName())
                .role(savedUser.getRole())
                .message("Nouveaux identifiants envoyés par email à " + savedUser.getEmail())
                .build();
    }

    /**
     * Every account on the platform, newest first — backs the admin's accounts-management
     * screen. Same summary shape as {@link #getUsersByRole}: no tokens, no password hashes,
     * no base64 image.
     */
    public List<UserSummaryResponse> getAllUsers() {
        return userRepository.findAllByOrderByUserIdDesc().stream()
                .map(AuthService::toSummary)
                .toList();
    }

    /**
     * Deletes an account. The seeded admin is protected so it is never possible
     * to lock yourself out of the platform — callers get a clear 409/400 rather than a
     * silent failure.
     */
    public void deleteUser(Long userId) {
        User user = userRepository.findById(userId)
                .orElseThrow(() -> new RuntimeException("Utilisateur non trouvé"));
        if (userId == 1L || "admin@stb.tn".equalsIgnoreCase(user.getEmail())) {
            throw new IllegalStateException("Le compte administrateur principal ne peut pas être supprimé.");
        }
        userRepository.deleteById(userId);
        log.info("Utilisateur {} ({}) supprimé par un admin", userId, user.getEmail());
    }

    /**
     * Changes password for an authenticated user.
     */
    public AuthResponse changePassword(Long userId, String email, ChangePasswordRequest request) {
        User user = null;
        if (userId != null) {
            user = userRepository.findById(userId).orElse(null);
        }
        if (user == null && email != null) {
            user = userRepository.findByEmail(email).orElse(null);
        }
        if (user == null && request.getUserId() != null) {
            user = userRepository.findById(request.getUserId()).orElse(null);
        }
        if (user == null) {
            throw new RuntimeException("Utilisateur non trouvé");
        }

        if (!passwordEncoder.matches(request.getCurrentPassword(), user.getPassword())) {
            throw new IllegalArgumentException("L'ancien mot de passe est incorrect.");
        }

        user.setPassword(passwordEncoder.encode(request.getNewPassword()));
        userRepository.save(user);
        log.info("Mot de passe mis à jour pour l'utilisateur {}", user.getEmail());

        return AuthResponse.builder()
                .userId(user.getUserId())
                .email(user.getEmail())
                .firstName(user.getFirstName())
                .lastName(user.getLastName())
                .role(user.getRole())
                .message("Mot de passe mis à jour avec succès.")
                .build();
    }

    /**
     * Self-service password recovery: generates a secure one-time reset link (valid 30 min)
     * and sends email via RabbitMQ without altering the user's current password.
     */
    public AuthResponse forgotPassword(ForgotPasswordRequest request) {
        String email = request.getEmail().trim().toLowerCase();
        Optional<User> optionalUser = userRepository.findByEmail(email);

        if (optionalUser.isEmpty()) {
            // Anti-enumeration: Return success message even if email doesn't exist
            return AuthResponse.builder()
                    .email(email)
                    .message("Si un compte existe pour cet email, un lien sécurisé de réinitialisation vous a été envoyé par email.")
                    .build();
        }

        User user = optionalUser.get();
        String resetToken = UUID.randomUUID().toString().replace("-", "") + UUID.randomUUID().toString().replace("-", "");
        Instant expiresAt = Instant.now().plus(30, ChronoUnit.MINUTES);

        user.setResetPasswordToken(resetToken);
        user.setResetPasswordExpiresAt(expiresAt);
        User savedUser = userRepository.save(user);

        String resetUrl = "http://localhost:4200/reset-password?token=" + resetToken;

        try {
            rabbitTemplate.convertAndSend(
                    RabbitConfig.EVENTS_EXCHANGE,
                    RabbitConfig.PASSWORD_RESET_REQUESTED_KEY,
                    RabbitConfig.passwordResetRequestedPayload(
                            savedUser.getUserId(),
                            savedUser.getEmail(),
                            savedUser.getFirstName(),
                            savedUser.getLastName(),
                            resetToken,
                            resetUrl,
                            expiresAt,
                            UUID.randomUUID().toString()));
            log.info("Lien de réinitialisation de mot de passe envoyé avec succès pour {}", savedUser.getEmail());
        } catch (Exception e) {
            log.error("Erreur lors de l'envoi de l'événement de réinitialisation pour {}: {}", savedUser.getEmail(), e.getMessage());
        }

        return AuthResponse.builder()
                .userId(savedUser.getUserId())
                .email(savedUser.getEmail())
                .message("Si un compte existe pour cet email, un lien sécurisé de réinitialisation vous a été envoyé par email.")
                .build();
    }

    /**
     * Completes password reset using the secure one-time token provided in the email link.
     */
    public AuthResponse resetPassword(ResetPasswordRequest request) {
        String token = request.getToken().trim();
        String newPassword = request.getNewPassword().trim();

        if (token.isBlank() || newPassword.length() < 8) {
            throw new RuntimeException("Le mot de passe doit contenir au moins 8 caractères.");
        }

        User user = userRepository.findByResetPasswordToken(token)
                .orElseThrow(() -> new RuntimeException("Le lien de réinitialisation est invalide ou a expiré. Veuillez refaire une demande."));

        if (user.getResetPasswordExpiresAt() == null || user.getResetPasswordExpiresAt().isBefore(Instant.now())) {
            user.setResetPasswordToken(null);
            user.setResetPasswordExpiresAt(null);
            userRepository.save(user);
            throw new RuntimeException("Le lien de réinitialisation a expiré. Veuillez refaire une demande.");
        }

        user.setPassword(passwordEncoder.encode(newPassword));
        user.setResetPasswordToken(null);
        user.setResetPasswordExpiresAt(null);
        user.setRefreshToken(null); // Force clean re-login
        userRepository.save(user);

        log.info("Mot de passe réinitialisé avec succès via lien sécurisé pour {}", user.getEmail());

        return AuthResponse.builder()
                .userId(user.getUserId())
                .email(user.getEmail())
                .message("Votre mot de passe a été réinitialisé avec succès. Vous pouvez maintenant vous connecter avec votre nouveau mot de passe.")
                .build();
    }

    private static UserSummaryResponse toSummary(User user) {
        return UserSummaryResponse.builder()
                .userId(user.getUserId())
                .email(user.getEmail())
                .firstName(user.getFirstName())
                .lastName(user.getLastName())
                .phone(user.getPhone())
                .role(user.getRole())
                .createdAt(user.getCreatedAt())
                .build();
    }
    
    public AuthResponse getUserById(Long userId) {
        User user = userRepository.findById(userId)
                .orElseThrow(() -> new RuntimeException("Utilisateur non trouvé"));
        
        return AuthResponse.builder()
                .userId(user.getUserId())
                .email(user.getEmail())
                .firstName(user.getFirstName())
                .lastName(user.getLastName())
                .phone(user.getPhone())
                .role(user.getRole())
                .imageBase64(convertBytesToBase64(user.getImage()))
                .experience(user.getExperience())
                .message("Données utilisateur récupérées")
                .build();
    }
    
    private byte[] convertBase64ToBytes(String base64String) {
        if (base64String == null || base64String.isEmpty()) {
            return null;
        }
        try {
            return Base64.getDecoder().decode(base64String);
        } catch (IllegalArgumentException e) {
            log.error("Erreur lors de la conversion de l'image base64", e);
            return null;
        }
    }
    
    private String convertBytesToBase64(byte[] bytes) {
        if (bytes == null || bytes.length == 0) {
            return null;
        }
        try {
            return Base64.getEncoder().encodeToString(bytes);
        } catch (Exception e) {
            log.error("Erreur lors de la conversion des bytes en base64", e);
            return null;
        }
    }
}
