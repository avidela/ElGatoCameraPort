# --- Build Frontend ---
FROM node:22-slim AS build-web
WORKDIR /app
COPY ElgatoControl.Web/package*.json ./
RUN npm install
COPY ElgatoControl.Web/ ./
RUN npm run build

# --- Build Backend ---
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build-api
WORKDIR /app
COPY ElgatoControl.Api/ElgatoControl.Api.csproj ./
RUN dotnet restore
COPY ElgatoControl.Api/ ./
RUN dotnet publish -c Release -o out

# --- Final Image ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine
WORKDIR /app

# Install ffmpeg and v4l-utils (the "good stuff" we need)
RUN apk add --no-cache ffmpeg v4l-utils

# Copy builds
COPY --from=build-api /app/out ./
COPY --from=build-web /app/dist ./wwwroot

# The app listens on port 5000
EXPOSE 5000

# Start the application
ENTRYPOINT ["dotnet", "ElgatoControl.Api.dll"]
