# Plan 035 — Redacción de registros con parámetros de consulta y encabezados reenviados

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
Dos frentes que tocan los mismos `Hosting/` y pueden avanzar a la vez si se coordinan por archivo.

**Frente A — Registros (KNOCK-186)**
1. Prueba primero: arrancar cada host con todo en `Verbose`, receptor en memoria y peticiones con consulta (debe fallar hoy).
2. Filtro de fuentes en `LoggingExtensions` de los cuatro proyectos, antes de leer la configuración.
3. Regla de redacción de consultas en `TelemetryRedactor` con sus pruebas unitarias y de tiempo.
4. `instance` de ProblemDetails sin consulta; regla de Semgrep contra `UseHttpLogging`.
5. Prueba de regresión de trazas sin consulta.

**Frente B — Encabezados reenviados (KNOCK-180)**
1. Validación al arrancar de redes y proxies (vacío fuera de `Development`, redes privadas, prefijo mínimo) en los cuatro proyectos.
2. Transformación de YARP en el gateway (reemplazar `X-Forwarded-For`, quitar los demás).
3. Mensajería con encabezados reenviados.
4. Terraform: variable validada y `ForwardedHeaders__KnownNetworks__0` para los cuatro; `terraform test`.
5. Pruebas de dos clientes y de encabezados falsos; humo en el entorno de evaluación.

## Archivos que se crean o cambian
- `src/Knock.Gateway/Hosting/{LoggingExtensions,TelemetryRedactor,CorsExtensions}.cs`, `src/Knock.Gateway/appsettings.json` (transformación de YARP).
- `src/Knock.{Identity,Market,Messaging}.Api/Hosting/{LoggingExtensions,TelemetryRedactor,ForwardedHeadersExtensions}.cs`; `src/Knock.Messaging.Api/Program.cs`.
- `appsettings*.json` de los cuatro proyectos (anulaciones sin efecto).
- `deploy/apps/main.tf`, `deploy/apps/variables.tf`, `deploy/tests/encabezados_reenviados.tftest.hcl`.
- Regla de Semgrep del repositorio (archivo protegido: la agrega una persona).
- `tests/UnitTest`, `tests/IntegrationTest`, `tests/SecurityTest` (clases `Spec035*`).
- `docs/OBSERVABILIDAD.md`, `docs/rate-limit-matrix.md`, `Memoria.md`.

## Dependencias
- Specs 005, 006, 009 y 011 implementadas (ya lo están en lo esencial).
- La subred del entorno de Container Apps conocida por Terraform (ya es variable).
- Debe estar desplegada antes del humo de dos réplicas de la spec 034.

## Riesgos del plan
- Los dos frentes editan los mismos archivos: se integran en ramas cortas y en este orden (A, luego B) para evitar conflictos.
- La duplicación de `Hosting/` obliga a aplicar el mismo cambio cuatro veces; la prueba que recorre los cuatro proyectos es la red de seguridad.

## Decisiones de la spec que este plan sigue
- Excluir fuentes en código; redactar toda la consulta; el gateway reemplaza los encabezados reenviados; fallar al arrancar sin redes de confianza.

## Verificación
- `dotnet build Knock.slnx -c Release`, `dotnet test Knock.slnx -c Release`, `dotnet format Knock.slnx --verify-no-changes`.
- `terraform validate` y `terraform test`; Semgrep con la regla nueva.
- `python scripts/sdd_lint.py --trace 035`.
