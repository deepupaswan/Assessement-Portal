# Build stage
FROM node:22-alpine AS build
WORKDIR /app

# Copy package files
COPY frontend/package*.json ./

# Install dependencies
RUN npm ci

# Copy source code
COPY frontend/ .

# Build Angular application in production mode
RUN npm run build -- --configuration production

# Runtime stage - Nginx
FROM nginx:alpine
EXPOSE 80

# Copy Nginx configuration
COPY frontend/nginx.conf /etc/nginx/nginx.conf
COPY frontend/nginx-default.conf /etc/nginx/conf.d/default.conf

# Copy built Angular app from build stage
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=10s --retries=3 \
    CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1

CMD ["nginx", "-g", "daemon off;"]
