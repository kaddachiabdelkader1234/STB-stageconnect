package com.smartek.authservice.config;

import org.springframework.amqp.core.TopicExchange;
import org.springframework.amqp.rabbit.core.RabbitTemplate;
import org.springframework.amqp.support.converter.Jackson2JsonMessageConverter;
import org.springframework.amqp.support.converter.MessageConverter;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.amqp.rabbit.connection.ConnectionFactory;

import java.util.Map;

/**
 * RabbitMQ publishing for auth-service.
 *
 * The only event auth-service emits today is {@code EncadrantCreated}, published to the
 * {@code smartek.events} topic exchange. Notification.Service (.NET / MassTransit) binds a
 * queue to it with routing key {@code encadrant.created} and deserializes the JSON payload
 * directly into its {@code EncadrantCreated} contract record.
 *
 * MassTransit names its own exchanges {@code Namespace:TypeName}, so we deliberately do NOT
 * publish through a MassTransit-shaped envelope: a raw JSON message on our own exchange keeps
 * the two stacks decoupled and avoids depending on MassTransit envelope internals from Java.
 *
 * RabbitTemplate is created manually (not via auto-configuration properties) so the host
 * resolves at runtime from SPRING_RABBITMQ_HOST with the same defaults docker-compose sets.
 */
@Configuration
public class RabbitConfig {

    public static final String EVENTS_EXCHANGE = "smartek.events";
    public static final String ENCADRANT_CREATED_KEY = "encadrant.created";
    public static final String PASSWORD_RESET_REQUESTED_KEY = "password.reset.requested";

    @Bean
    public TopicExchange eventsExchange() {
        return new TopicExchange(EVENTS_EXCHANGE, true, false);
    }

    @Bean
    public MessageConverter jacksonMessageConverter() {
        // JSON instead of Java serialization — Notification.Service reads it with System.Text.Json.
        return new Jackson2JsonMessageConverter();
    }

    @Bean
    public RabbitTemplate rabbitTemplate(ConnectionFactory connectionFactory,
                                         MessageConverter jacksonMessageConverter) {
        RabbitTemplate template = new RabbitTemplate(connectionFactory);
        template.setMessageConverter(jacksonMessageConverter);
        template.setExchange(EVENTS_EXCHANGE);
        return template;
    }

    /**
     * Builds the EncadrantCreated payload as a plain map — Jackson serializes it to
     * {"UserId":…,"FirstName":…,"LastName":…,"Email":…,"Departement":…,"TemporaryPassword":…},
     * exactly the PascalCase shape System.Text.Json deserializes by default.
     */
    public static Map<String, Object> encadrantCreatedPayload(long userId,
                                                              String firstName,
                                                              String lastName,
                                                              String email,
                                                              String departement,
                                                              String temporaryPassword) {
        var payload = new java.util.HashMap<String, Object>();
        payload.put("UserId", userId);
        payload.put("FirstName", firstName);
        if (lastName != null && !lastName.isBlank()) {
            payload.put("LastName", lastName);
        }
        payload.put("Email", email);
        if (departement != null && !departement.isBlank()) {
            payload.put("Departement", departement);
        }
        payload.put("TemporaryPassword", temporaryPassword);
        return payload;
    }

    /**
     * Builds the EncadrantCreated payload for the "resend credentials" flow: the same event
     * powers the welcome email and the reset email — {@code IsPasswordReset} flips the email
     * copy from "welcome" to "your password was reset" on the .NET side.
     */
    public static Map<String, Object> encadrantCreatedPayload(long userId,
                                                              String firstName,
                                                              String lastName,
                                                              String email,
                                                              String departement,
                                                              String phone,
                                                              String temporaryPassword,
                                                              boolean isPasswordReset) {
        var payload = new java.util.HashMap<String, Object>(encadrantCreatedPayload(
                userId, firstName, lastName, email, departement, phone, temporaryPassword));
        payload.put("IsPasswordReset", isPasswordReset);
        return payload;
    }

    /**
     * Builds the EncadrantCreated payload with the encadrant's phone number included.
     * Overload kept separate so the existing call sites read cleanly.
     */
    public static Map<String, Object> encadrantCreatedPayload(long userId,
                                                              String firstName,
                                                              String lastName,
                                                              String email,
                                                              String departement,
                                                              String phone,
                                                              String temporaryPassword) {
        var payload = new java.util.HashMap<String, Object>(encadrantCreatedPayload(
                userId, firstName, lastName, email, departement, temporaryPassword));
        if (phone != null && !phone.isBlank()) {
            payload.put("Phone", phone);
        }
        return payload;
    }

    public static Map<String, Object> passwordResetRequestedPayload(long userId,
                                                                   String email,
                                                                   String firstName,
                                                                   String lastName,
                                                                   String resetToken,
                                                                   String resetUrl,
                                                                   java.time.Instant expiresAt,
                                                                   String traceId) {
        var payload = new java.util.HashMap<String, Object>();
        payload.put("UserId", userId);
        payload.put("Email", email);
        payload.put("FirstName", firstName != null ? firstName : "");
        payload.put("LastName", lastName != null ? lastName : "");
        payload.put("ResetToken", resetToken);
        payload.put("ResetUrl", resetUrl);
        payload.put("ExpiresAt", expiresAt.toString());
        payload.put("TraceId", traceId != null ? traceId : java.util.UUID.randomUUID().toString());
        return payload;
    }
}
