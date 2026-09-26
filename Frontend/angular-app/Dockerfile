# Multi-stage build for the Angular STB Frontend
# Stage 1: Build the application with Node.js 20
# Stage 2: Serve with nginx Alpine (tiny, secure, fast)

# ========== BUILD ==========
FROM node:20-alpine AS builder

WORKDIR /app

# Install dependencies first (layer caching)
COPY package.json package-lock.json ./
RUN npm ci --legacy-peer-deps

# Copy source files
COPY . .

# Build for production
RUN npm run build -- --configuration production

# ========== PRODUCTION ==========
FROM nginx:alpine AS production

# Install curl for health checks
RUN apk add --no-cache curl

# Remove default nginx config
RUN rm -rf /etc/nginx/conf.d/default.conf

# Copy custom nginx config (full config: worker_processes/events/http blocks live at
# /etc/nginx/nginx.conf — /etc/nginx/conf.d/ only accepts server{} directives)
COPY nginx.conf /etc/nginx/nginx.conf

# Copy built assets from build stage. The Angular "application" builder (v17+) emits the
# deployable app into <outputPath>/browser/ — copying the outputPath itself would leave the
# image serving nginx's default index.html instead of the SPA.
COPY --from=builder /app/dist/angular-app/browser /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Health check
# 127.0.0.1, not localhost: nginx listens on IPv4 only, but localhost resolves to ::1 here.
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://127.0.0.1/ || exit 1

# Start nginx
CMD ["nginx", "-g", "daemon off;"]
