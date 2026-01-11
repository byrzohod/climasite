# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source files
COPY . .

# Build the Angular application
RUN npm run build -- --configuration=production

# Runtime stage
FROM nginx:alpine AS final

# Copy built application
COPY --from=build /app/dist/clima-site.web/browser /usr/share/nginx/html

# Copy nginx configuration template (will be processed by entrypoint)
COPY nginx.conf.template /etc/nginx/nginx.conf.template

# Copy entrypoint script
COPY docker-entrypoint.sh /docker-entrypoint.sh
RUN chmod +x /docker-entrypoint.sh

# Expose port
EXPOSE 80

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:80/ || exit 1

# Use entrypoint script to generate runtime config
ENTRYPOINT ["/docker-entrypoint.sh"]
