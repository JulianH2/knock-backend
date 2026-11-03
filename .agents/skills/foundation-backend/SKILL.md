---
name: foundation-backend
description: Estructura base de knock-backend (solución .NET 10, módulos vacíos, pipeline de seguridad fijo, health, docker-compose con documentdb-local, Redis y Azurite, CI). Úsala en TT-API-01, TT-API-03, TT-BD-01, TT-QA-01 y cuando toques Program.cs, Hosting/ o la configuración base.
---

# foundation-backend

## Estructura
```
Knock.slnx · Directory.Build.props · global.json · .config/dotnet-tools.json
src/Knock.Api/
  Program.cs                      # corto: todo se registra en Hosting/*Extensions.cs
  Hosting/ Middleware/ Common/
  Modules/{Identity,Catalog,Requests,Messaging,Reputation,Trust,Notifications,Assistant,_Template}/
tests/
  UnitTest/ (incluye Architecture/ con NetArchTest)   IntegrationTest/   SecurityTest/
  DatabaseTest/   ContractTest/   perf/ (k6)   fuzz/ (RESTler)   chaos/
db/  seeds/  sepomex/  scripts/  backup/          # scripts de base de datos (ver skill database-scripts)
deploy/  docker-compose.local.yml  docker-compose.ci.yml  terraform/
```

## Pipeline fijo (constitución §4)
`SecurityHeaders → ExceptionHandling → ForwardedHeaders → HSTS (no Dev) → HttpsRedirection → CORS → RateLimiter → Authentication → Authorization → MapControllers / MapHubs`. Una prueba de arquitectura verifica el orden.

## Base
- Versión de .NET: destino **.NET 11**; hoy el código está en **net10**. Las skills y la constitución cambian de versión en el mismo PR que cambie el código.
- `Directory.Build.props`: `TreatWarningsAsErrors`, `AnalysisMode=All`, `EnforceCodeStyleInBuild`, `NuGetAudit` (all), SonarAnalyzer.CSharp.
- Health: `/health` (liveness) y `/health/ready` (MongoDB, Redis, Blob).
- Versionado `/api/v1`, ProblemDetails, Scalar solo en Dev, Serilog JSON con CorrelationId y filtro de secretos.
- Herramientas locales en `.config/dotnet-tools.json` (crear con `python scripts/bootstrap_dotnet_tools.py`): dotnet-stryker, reportgenerator, CycloneDX, dotnet-sonarscanner.
- `deploy/docker-compose.local.yml` (con TT-BD-07; hoy el archivo sigue con PostGIS): `documentdb-local` fijado por digest (el mismo que `DocumentDbFixture.Image`), `redis:7-alpine`, Azurite y el emulador de Firebase Auth.
- Secretos: `dotnet user-secrets` en local, Key Vault con identidad administrada en Azure.

## Verificación
`dotnet build Knock.slnx -c Release` 0/0 · `curl /health/ready` 200 con dependencias y 503 sin ellas.
