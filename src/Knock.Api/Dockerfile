# Imagen de la API (multi-etapa). Contexto de compilación: raíz del repositorio.
#   docker build -f src/Knock.Api/Dockerfile -t knock-api:local .
# Sin credenciales ni valores reales: la configuración llega por variables de entorno en tiempo de ejecución.

# ---- Compilación ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props .editorconfig ./
COPY src/Knock.Api/Knock.Api.csproj src/Knock.Api/packages.lock.json src/Knock.Api/
RUN dotnet restore src/Knock.Api/Knock.Api.csproj --locked-mode
COPY src/Knock.Api/ src/Knock.Api/
RUN dotnet publish src/Knock.Api/Knock.Api.csproj -c Release --no-restore -o /app/publish /p:UseAppHost=false

# ---- Ejecución ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
COPY --from=build /app/publish .
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Knock.Api.dll"]
