#!/bin/sh
set -e

# Default PORT to 80 if not provided (Railway injects PORT)
export PORT="${PORT:-80}"

# Default API URL if not provided
export API_URL="${API_URL:-http://localhost:8080}"

echo "Configuring nginx with PORT: $PORT, API_URL: $API_URL"

# Generate nginx config from template with environment variable substitution
envsubst '${PORT} ${API_URL}' < /etc/nginx/nginx.conf.template > /etc/nginx/conf.d/default.conf

# Test nginx configuration
echo "Testing nginx configuration..."
nginx -t

# Start nginx
echo "Starting nginx..."
exec nginx -g 'daemon off;'
