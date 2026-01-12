#!/bin/sh
set -e

# Default PORT to 80 if not provided (Railway injects PORT)
export PORT="${PORT:-80}"

# Default API URL if not provided
export API_URL="${API_URL:-http://localhost:8080}"

# Extract API_HOST from API_URL (remove protocol prefix and any trailing path)
# Example: https://api.example.com/path -> api.example.com
API_HOST=$(echo "$API_URL" | sed -E 's|^https?://||' | sed -E 's|/.*||')
export API_HOST

echo "Configuring nginx with PORT: $PORT, API_URL: $API_URL, API_HOST: $API_HOST"

# Generate nginx config from template with environment variable substitution
envsubst '${PORT} ${API_URL} ${API_HOST}' < /etc/nginx/nginx.conf.template > /etc/nginx/conf.d/default.conf

# Test nginx configuration
echo "Testing nginx configuration..."
nginx -t

# Start nginx
echo "Starting nginx..."
exec nginx -g 'daemon off;'
