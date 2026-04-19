# Frontend build stage
FROM node:20-alpine AS frontend-build
WORKDIR /frontend

COPY frontend/package*.json ./
RUN npm ci

COPY frontend/. ./
ARG VITE_API_BASE_URL=/api
ARG VITE_HUB_URL=/hubs/execution
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
ENV VITE_HUB_URL=$VITE_HUB_URL
RUN npm run build

# Backend build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY backend/TraceAI.Api.csproj backend/
RUN dotnet restore "backend/TraceAI.Api.csproj"

# Copy source and build
COPY backend/. backend/
RUN dotnet build "backend/TraceAI.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "backend/TraceAI.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false
COPY --from=frontend-build /frontend/dist /app/publish/wwwroot

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish ./

EXPOSE 8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080

ENTRYPOINT ["dotnet", "TraceAI.Api.dll"]
