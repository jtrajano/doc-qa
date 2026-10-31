# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies first so this layer is cached
COPY package.json package-lock.json ./
RUN npm ci

# Copy the source and build the static files
COPY . .
RUN npm run build

# ---- Runtime stage ----
FROM nginx:alpine AS final

COPY nginx/default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html

# Default backend for standalone runs; compose will override it
ENV API_URL=http://host.docker.internal:8080

EXPOSE 80   