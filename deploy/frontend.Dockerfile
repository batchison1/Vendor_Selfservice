# syntax=docker/dockerfile:1
# ---------------------------------------------------------------------------
# Frontend (Vite/React) container image — TEMPLATE / scaffolding.
# Build context is the REPO ROOT, e.g.:
#   docker build -f deploy/frontend.Dockerfile -t REGISTRY/vss-frontend:TAG .
#
# NOTE: build-time env. Vite inlines REACT_APP_* at build time (import.meta.env).
# TODO: pass the real values as --build-arg and forward them into `npm run build`
#       (e.g. REACT_APP_VSS_API_DOMAIN, REACT_APP_AUTH_MODE=entra,
#        REACT_APP_ENTRA_*, REACT_APP_UNITY_*). The dev stub for the private
#        @univerus/... package lets `npm ci` succeed without registry auth.
# ---------------------------------------------------------------------------

# ---- build stage ----
FROM node:20 AS build
WORKDIR /app

COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci

COPY frontend/ ./
RUN npm run build

# ---- runtime stage ----
FROM nginx:1.27-alpine AS runtime
COPY deploy/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
