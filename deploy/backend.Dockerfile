# syntax=docker/dockerfile:1
# ---------------------------------------------------------------------------
# Backend (Vss.Api) container image — TEMPLATE / scaffolding.
# Build context is the REPO ROOT, e.g.:
#   docker build -f deploy/backend.Dockerfile -t REGISTRY/vss-backend:TAG .
# ---------------------------------------------------------------------------

# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the backend solution and restore first (better layer caching).
COPY backend/ ./backend/
WORKDIR /src/backend
RUN dotnet restore Vss.Api/Vss.Api.csproj
RUN dotnet publish Vss.Api/Vss.Api.csproj -c Release -o /app/publish --no-restore

# ---- runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

# The API listens on 8080 by convention in containers.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Vss.Api.dll"]
