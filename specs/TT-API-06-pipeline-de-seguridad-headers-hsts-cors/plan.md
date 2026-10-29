# plan.md — TT-API-06 — Pipeline de seguridad: SecurityHeaders, ForwardedHeaders, HSTS, HTTPS, CORS y rate limiter

> Borrador. Nada de esto se implementa hasta que la spec esté Aprobada y las dudas de spec §8 estén resueltas por el líder.

## 1. Mapeo de archivos
- **Reutilizar (buscado con `python scripts/codemap.py --query` por «response headers middleware OnStarting», «cors», «hsts https redirect», «kestrel server header» y «endpoint data source public endpoints»; sin coincidencias para CORS, HSTS ni encabezados de seguridad):**
  - `src/Knock.Api/Hosting/CorrelationIdMiddleware.cs`: molde del middleware que fija encabezados con `Response.OnStarting` (el manejador de excepciones limpia la respuesta).
  - `src/Knock.Api/Hosting/RateLimitingExtensions.cs`: molde de opciones validadas con `ValidateOnStart`, sin valores por defecto en código; no se cambia.
  - `src/Knock.Api/Hosting/ForwardedHeadersExtensions.cs`: se ajusta solo para procesar también `X-Forwarded-Proto` con proxies de confianza (duda 4).
  - `src/Knock.Api/Hosting/KnockApplicationExtensions.cs` (`AddKnockApi`/`UseKnockApi`): aquí se registra y se ordena todo.
  - `src/Knock.Api/Hosting/OpenApiExtensions.cs`: `/openapi` y `/scalar` solo en Development (referencia para la duda 9).
  - `docs/MIDDLEWARES.md` y `docs/rate-limit-matrix.md`: se actualizan.
  - `tests/IntegrationTest/Support/` (`KnockApiFactory`, `TestHttp`, `TestController`), `tests/SecurityTest/RateLimiting/ForwardedHeadersEvasionTests.cs` (molde de proxies de confianza), `tests/IntegrationTest/Hosting/PipelineOrderTests.cs` (molde del orden; no se edita), `tests/SecurityTest/OpenApi/ScalarExposureTests.cs` y `tests/SecurityTest/Secrets/AppSettingsSecretsTests.cs` (moldes).
- **Crear (nombres por confirmar al implementar):**
  - `src/Knock.Api/Hosting/SecurityHeadersMiddleware.cs` (+ extensión de registro): encabezados fijos y `Cache-Control` si falta.
  - `src/Knock.Api/Hosting/TransportSecurityExtensions.cs`: opciones `Security:Hsts` y `Security:Https`, `UseHsts` (no Development) y `UseHttpsRedirection` opcional.
  - `src/Knock.Api/Hosting/CorsExtensions.cs`: política única por configuración, validación de orígenes, falla cerrada.
  - Apagar `AddServerHeader` de Kestrel (en `Program.cs` o en una extensión de `Hosting/`, la que menos cambie el orden de arranque).
  - `docs/security-headers.md`.
  - Pruebas: `tests/UnitTest/Hosting/`, `tests/IntegrationTest/{Hosting,RateLimiting,OpenApi}/`, `tests/SecurityTest/Hosting/`; un endpoint de prueba con `Cache-Control` propio en `tests/IntegrationTest/Support/TestController.cs`.
- **Modificar:** `KnockApplicationExtensions.cs` (orden y registro), `ForwardedHeadersExtensions.cs`, `src/Knock.Api/appsettings.json` (`Security:Hsts`, `Security:Cors:AllowedOrigins` vacío, `Security:Cors:PreflightMaxAge`; sin `RedirectPort`), `appsettings.Development.json` solo si se resuelve la duda 7 (revisarlo antes: no se pudo leer al redactar la spec), `docs/MIDDLEWARES.md`, `docs/rate-limit-matrix.md`, `KnockApiFactory`/`TestHttp` si hace falta para el cliente HTTPS. Sin cambios en `*.csproj` ni `packages.lock.json`.
- **No tocar:** `.github/`, `scripts/`, `.agents/`, configuración de analizadores, `constitution.md` (si el orden de §4 necesitara cambiar, lo traslada el revisor), `RateLimitingExtensions.cs` salvo que el líder resuelva la duda 8 con cambio de política, pruebas de TT-API-01, 02 y 03 (siguen en verde sin editar), `docs/openapi.v1.json` a mano, `Dockerfile`, `deploy/`, specs ajenas, módulos de negocio.

## 2. Riesgos y efectos secundarios
- **HSTS inexistente detrás del ingress:** si `ForwardedHeaders` no procesa `X-Forwarded-Proto`, la API siempre ve HTTP y no emite HSTS. Mitigación: ajuste de `ForwardedHeaders` (duda 4) y casos 05 y 07.
- **Bucle de redirecciones:** `UseHttpsRedirection` con puerto configurado y esquema mal leído redirige para siempre (el ingress vuelve a entregar HTTP). Mitigación: opt-in por `RedirectPort`, ausente en Container Apps, y caso 08.
- **Confianza en proxies:** `X-Forwarded-Proto` solo de proxies o redes de confianza; sin ellos, `None`. Un CIDR demasiado amplio permitiría falsear el esquema y la IP (mismo riesgo de `docs/rate-limit-matrix.md`).
- **El manejador de excepciones y CORS:** puede borrar los encabezados CORS del 500 (el manejador está antes que CORS en el orden fijo). Si el caso 12 lo demuestra, se aplica CORS en `OnStarting`, sin cambiar el orden.
- **Preflight y enrutamiento:** un `OPTIONS` a una ruta que solo acepta `POST` podría recibir 405 del enrutador antes que CORS. El caso 10 lo comprueba; si ocurre, se corrige con la política por defecto aplicada a los endpoints, sin comodines.
- **CSP vs Scalar en Development:** `default-src 'none'` puede romper `/scalar` (duda 9, caso 18).
- **`Cache-Control: no-store` por defecto:** endpoints futuros con `ETag` deben declarar el suyo (duda 2); se documenta en `docs/security-headers.md`.
- **Orden del pipeline:** `PipelineOrderTests` (TT-API-03) compara `docs/MIDDLEWARES.md` con `UseKnockApi` leyendo el código con una expresión regular: los nombres nuevos deben ser llamadas `app.UseX(` o `app.UseMiddleware<T>(` y estar en el documento en el mismo orden; `UseHsts` se llama siempre dentro de `UseKnockApi` (la condición «no Development» va dentro de la extensión o en un `if` que la expresión regular siga viendo).
- **Pruebas existentes:** ninguna debe recibir 429 ni cambiar de resultado; `KnockApiFactory` ya sube los límites y fija la IP con `X-Test-Remote-Ip`. `HSTS` excluye `localhost` por defecto: los casos 05 y 08 usan `https://api.example.test`.
- **Prueba del caso 16:** debe excluir los controllers de `tests/IntegrationTest/Support/` y cubrir los endpoints que no son controllers (health, OpenAPI, Scalar). Una ruta con parámetros se sustituye por valores de ejemplo.
- **Gateway:** esta tarea no lo crea ni lo configura; lo que cambia allí queda anotado en spec §3 (ADR-001, TT-ARQ-02).
- Docker, Azure e ingress los opera una persona; esta tarea no los necesita. La prueba real contra el ingress es de TT-AZ-02.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-API-06-01 | `IntegrationTest/Hosting/SecurityHeadersTests` | integración |
| TC-TT-API-06-02 | `IntegrationTest/Hosting/SecurityHeadersTests` | integración |
| TC-TT-API-06-03 | `IntegrationTest/Hosting/SecurityHeadersTests` | integración |
| TC-TT-API-06-04 | `SecurityTest/Hosting/ServerFingerprintTests` | seguridad negativa |
| TC-TT-API-06-05 | `IntegrationTest/Hosting/HstsTests` | integración |
| TC-TT-API-06-06 | `IntegrationTest/Hosting/HstsTests` | integración |
| TC-TT-API-06-07 | `SecurityTest/Hosting/ForwardedProtoTests` | seguridad negativa |
| TC-TT-API-06-08 | `IntegrationTest/Hosting/HttpsRedirectionTests` | integración |
| TC-TT-API-06-09 | `IntegrationTest/Hosting/CorsTests` | integración |
| TC-TT-API-06-10 | `IntegrationTest/Hosting/CorsTests` | integración |
| TC-TT-API-06-11 | `SecurityTest/Hosting/CorsPolicyTests` | seguridad negativa |
| TC-TT-API-06-12 | `IntegrationTest/Hosting/CorsTests` | integración |
| TC-TT-API-06-13 | `UnitTest/Hosting/SecuritySettingsValidationTests` | unitaria |
| TC-TT-API-06-14 | `SecurityTest/Hosting/SecurityConfigurationTests` | seguridad negativa |
| TC-TT-API-06-15 | `SecurityTest/Hosting/SecurityConfigurationTests` | seguridad negativa |
| TC-TT-API-06-16 | `IntegrationTest/RateLimiting/PublicEndpointsRateLimitTests` | integración |
| TC-TT-API-06-17 | `IntegrationTest/Hosting/SecurityPipelineOrderTests` | integración |
| TC-TT-API-06-18 | `IntegrationTest/OpenApi/DevelopmentDocsTests` | integración |
| TC-TT-API-06-19 | `docs/MIDDLEWARES.md`, `docs/security-headers.md`, `docs/rate-limit-matrix.md` (existencia y contenido), `git diff` de `*.csproj` y `packages.lock.json`, `gitleaks` | comando (evidencia en el PR) |
| TC-TT-API-06-20 | `dotnet build` + `dotnet format --verify-no-changes` | comando |

Regresión sin editar: TC-TT-API-03-03, 04, 05, 06, 07, 12 y 16; TC-TT-API-02-02..08; TC-TT-API-01-12 y 14.

## 4. Secuencia
1. Aprobación de la spec y respuesta del líder a las dudas (en especial 1, 4, 5, 6 y 8).
2. Contrato (spec §4) y pruebas que fallan primero (encabezados, HSTS, CORS, endpoints públicos).
3. Ajuste de `ForwardedHeaders` con `X-Forwarded-Proto` y caso 07 (regresión de TC-TT-API-03-06 en verde).
4. `SecurityHeadersMiddleware`, `Cache-Control` y Kestrel sin `Server` (casos 01 a 04).
5. HSTS y redirección opcional (casos 05, 06 y 08).
6. CORS con falla cerrada, validación y arranque (casos 09 a 15).
7. Prueba de endpoints públicos (caso 16) y política propia por IP de `/health/ready`, 60 por minuto configurable (duda 8, resuelta por el líder el 2026-10-28).
8. Orden del pipeline y documentos (`docs/MIDDLEWARES.md`, `docs/security-headers.md`, nota en `docs/rate-limit-matrix.md`; casos 17 a 19), prueba de Scalar (caso 18).
9. Gates, `Memoria.md`, informe y evidencia en el PR (`curl -I` del caso 04).

## 5. Verificación (comandos exactos)
```bash
dotnet restore Knock.slnx && dotnet tool restore
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest -c Release --no-build
dotnet list Knock.slnx package --vulnerable --include-transitive
git diff --stat -- '*.csproj' '*packages.lock.json'
python scripts/sdd_lint.py && python scripts/sdd_lint.py --trace TT-API-06
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
```
