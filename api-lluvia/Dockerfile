# Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app
COPY . .
RUN dotnet publish -c Release -o out

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Dependencias nativas del sistema operativo para SkiaSharp
RUN apt-get update && apt-get install -y \
    fontconfig \
    libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "api-lluvia.dll"]