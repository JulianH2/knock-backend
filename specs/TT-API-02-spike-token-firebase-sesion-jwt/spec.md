# TT-API-02 — Spike: validar el token de Firebase de un número de prueba y emitir la sesión propia (JWT)

> **Jira:** KNOCK-21 · **Área:** API · Backend base · **Épica:** EP-13 · **Responsable:** José Guillermo Rangel Hernández · **Sprint:** S0 · **Puntos:** 2 · **Prioridad:** Alta
> **Repositorio:** knock-backend · **Ficha:** TT-API-02 (`docs/backlog/TT-API-02.md`) · **Rama:** `chore/KNOCK-21-spike-firebase-jwt` → PR a `sprint-00`
> **Depende de:** TT-API-01, TT-FB-01
> **Fuentes:** DECISIONES.md #2 y #21 · DECISION_TECNICA.md §8
> **Skills:** auth-firebase-jwt, security-core, vertical-slice-api, testing-strategy-backend, story-workflow
> **Estado:** Borrador

## 1. Objetivo
Comprobar al inicio la parte más riesgosa del acceso: que el servidor confíe en el código SMS validado por Firebase y entregue su propia sesión (JWT). Es un **spike**: lo que se entrega es un canje que funciona de punta a punta y lo aprendido, no la solución completa de HU-001.

**Terminada cuando** (ficha): hay una prueba de integración que canjea un token de prueba de Firebase por la sesión propia y llama a `/me` con ella.

**Se entrega:**
- `POST /api/v1/auth/firebase`: valida el ID token de Firebase, lee el teléfono, crea el usuario si no existe y responde `{ accessToken, refreshToken, esNuevo }`.
- JWT propio (HS256, 15 min) con `sub` = id interno; `GET /api/v1/me` protegido con `[Authorize]`.
- Pruebas de integración y de seguridad negativa de los casos de §2.
- Nota con qué funcionó, qué falta para HU-001 y riesgos (la pide la ficha; ruta en la duda 10).

**Queda fuera:** ver §3 Out (rotación y revocación del refresh, logout, límites, pipeline de seguridad, pantallas).

## 2. Historia y casos de prueba
**TT-API-02** — Tarea técnica (no es historia de usuario). Los casos 01 a 13 se verifican con pruebas de código; el 14 con evidencia manual de una persona; el 15 y el 16 por revisión y comandos. Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_02_KK`. Los casos 01, 02, 03 y 05 a 08 cubren V1, V2 y V3 de la ficha; el resto sale de los pasos 2, 3 y 6 de la ficha y de `security-core`.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-02-01 | Dado un ID token válido de un número de prueba de Firebase (V1), cuando se hace `POST /api/v1/auth/firebase`, entonces responde 200 con `accessToken`, `refreshToken` (no vacío, distinto del access token) y `esNuevo` (`true` la primera vez). | `IntegrationTest/Identity/FirebaseSessionTests.ExchangeValidIdToken_Returns200WithSession_TC_TT_API_02_01` (origen del token: duda 1) |
| TC-TT-API-02-02 | Dado un ID token de Firebase alterado (firma modificada), cuando se canjea, entonces responde 401 `application/problem+json` sin emitir tokens (V2). | `SecurityTest/Identity/FirebaseTokenRejectionTests.TamperedIdToken_Returns401_TC_TT_API_02_02` |
| TC-TT-API-02-03 | Dado un ID token de Firebase vencido, cuando se canjea, entonces responde 401 `application/problem+json` sin emitir tokens (V2). | `SecurityTest/Identity/FirebaseTokenRejectionTests.ExpiredIdToken_Returns401_TC_TT_API_02_03` |
| TC-TT-API-02-04 | Dado un ID token que Firebase valida pero no trae `phone_number` (o es de otro proyecto o audiencia), cuando se canjea, entonces se rechaza con ProblemDetails y no se crea usuario (código HTTP: duda 4). | `SecurityTest/Identity/FirebaseTokenRejectionTests.IdTokenWithoutPhoneOrWrongProject_IsRejected_TC_TT_API_02_04` |
| TC-TT-API-02-05 | Dado `GET /api/v1/me` sin encabezado `Authorization`, cuando se llama, entonces responde 401 (V3). | `IntegrationTest/Identity/MeEndpointTests.Me_WithoutToken_Returns401_TC_TT_API_02_05` |
| TC-TT-API-02-06 | Dado el `accessToken` obtenido en el canje, cuando se llama a `GET /api/v1/me` con él, entonces responde 200 con el id interno del usuario y si es nuevo (V3; forma exacta: duda 6). | `IntegrationTest/Identity/MeEndpointTests.Me_WithIssuedToken_Returns200_TC_TT_API_02_06` |
| TC-TT-API-02-07 | Dado el JWT emitido, cuando se decodifica, entonces usa HS256, vence a los 15 min, su `sub` es el id interno (no el `uid` de Firebase) y no contiene el teléfono. | `UnitTest/Identity/SessionTokenIssuerTests.IssuedJwt_HasHs256_15MinutesAndInternalSub_TC_TT_API_02_07` |
| TC-TT-API-02-08 | Dado un JWT propio alterado, vencido (con `ClockSkew` cero, un segundo después del vencimiento), con algoritmo `none` o firmado con otra clave, cuando se llama a `/me`, entonces responde 401 (V2). | `SecurityTest/Identity/SessionJwtValidationTests.InvalidJwt_Returns401_TC_TT_API_02_08` |
| TC-TT-API-02-09 | Dado el mismo número de prueba canjeado dos veces, cuando se repite el canje, entonces el `sub` es el mismo y `esNuevo` es `false` la segunda vez (alta idempotente; depende de la duda 2). | `IntegrationTest/Identity/FirebaseSessionTests.SameNumberTwice_ReturnsSameUserAndNotNew_TC_TT_API_02_09` |
| TC-TT-API-02-10 | Dado un cuerpo vacío, sin `firebaseIdToken` o con un valor vacío o demasiado largo, cuando se canjea, entonces responde 400 `application/problem+json` y no llama a Firebase (límite: duda 5). | `SecurityTest/Identity/FirebaseRequestValidationTests.InvalidBody_Returns400_TC_TT_API_02_10` |
| TC-TT-API-02-11 | Dados `appsettings*.json` y el repositorio, entonces no contienen la clave del JWT ni la cuenta de servicio de Firebase; y dada una clave de menos de 32 bytes, entonces la API no la acepta (clave efímera solo donde decida la duda 3). | `SecurityTest/Secrets/JwtKeyConfigurationTests.KeyNotInRepoAndMinLength_TC_TT_API_02_11` (complementa `AppSettingsSecretsTests`) |
| TC-TT-API-02-12 | Dado un canje correcto y uno rechazado, cuando se revisan los logs y el cuerpo de la respuesta, entonces no aparecen el ID token, el teléfono, el access token ni el refresh token. | `SecurityTest/Identity/SensitiveDataLeakTests.ExchangeAndRejection_DoNotLeakSensitiveData_TC_TT_API_02_12` |
| TC-TT-API-02-13 | Dado un entorno distinto de Development, cuando se arranca la API con la variable del emulador de Firebase Auth, entonces no se aceptan tokens del emulador (sin firma). Solo aplica si se elige el emulador (duda 1). | `SecurityTest/Identity/FirebaseEmulatorGuardTests.NonDevelopment_EmulatorTokensRejected_TC_TT_API_02_13` |
| TC-TT-API-02-14 | Dado el proyecto de Firebase `knock-0` y un número de prueba de TT-FB-01 (sin SMS real), cuando una persona obtiene su ID token con la REST API de Identity Toolkit y lo canjea contra la API local, entonces recibe sesión y `/me` responde 200 (cierra el caso 01 de TT-FB-01). | Evidencia manual en el PR (sin códigos, llaves ni tokens); la ejecuta una persona |
| TC-TT-API-02-15 | Dada la tarea terminada, entonces existe la nota del spike con qué funcionó, qué falta para HU-001 y riesgos, sin secretos. | Revisión del PR (duda 10) |
| TC-TT-API-02-16 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios. | `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes`; salida en el PR (duda 11) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Módulo `Identity` (`src/Knock.Api/Modules/Identity/`): validación del ID token con el Firebase Admin SDK (`VerifyIdTokenAsync`), alta del usuario si no existe, emisión del JWT (HS256, 15 min, `sub` = id interno) y del refresh token, `POST /api/v1/auth/firebase` y `GET /api/v1/me`.
  - Autenticación JWT Bearer en `Hosting/` (`ClockSkew` cero, algoritmos fijados a HS256) y `UseAuthentication`/`UseAuthorization` en el pipeline.
  - Configuración por user-secrets o variables de entorno: clave del JWT y ruta de la cuenta de servicio de Firebase; nada en el repositorio.
  - Pruebas de los casos 01 a 13 en `tests/UnitTest`, `tests/IntegrationTest` y `tests/SecurityTest`.
  - Nota del spike con lo aprendido (ficha, paso 6).
  - Paquetes que pide la ficha: `FirebaseAdmin` y `Microsoft.AspNetCore.Authentication.JwtBearer`. **No se agregan hasta evaluarlos** (necesidad, licencia, mantenimiento, vulnerabilidades) y registrarlos en `Memoria.md`; versiones al implementar.
- **Out:**
  - Rotación, revocación y reuso del refresh token, `blacklist:{jti}` en Redis, cierre de sesión y límites de peticiones (HU-001 y HU-003, Sprint 1). Aquí el refresh token solo se entrega; no hay endpoint que lo canjee.
  - Pipeline de seguridad de la constitución §4 (SecurityHeaders, ForwardedHeaders, HSTS, HttpsRedirection, CORS, RateLimiter): otra tarea con su spec, propuesta junto con esta.
  - Consentimiento del aviso de privacidad y términos, nombre, zona y foto (HU-001, HU-002, HU-009); cambio de número (HU-008); `role` y permisos del panel.
  - Pantallas y cliente de la app (TT-PRU-04 y TT-MOB-*).
  - Proyecto de Firebase, números de prueba y llaves (TT-FB-01, KNOCK-28: los configuró una persona).
  - Redis, MassTransit, outbox y cualquier otro endpoint de negocio o evento.
  - Operar Firebase, Azure o Docker: lo hace una persona.

## 4. Contratos API y eventos
| Método y ruta | Auth | Petición | Respuesta | Notas |
|---|---|---|---|---|
| `POST /api/v1/auth/firebase` | pública | `{ firebaseIdToken }` (nombre del campo: duda 1) | 200 `{ accessToken, refreshToken, esNuevo }` · 400 · 401 | `application/problem+json` en errores; mensaje uniforme que no distingue el motivo del rechazo de Firebase. |
| `GET /api/v1/me` | JWT propio (`[Authorize]`) | — | 200 `{ id, esNuevo }` · 401 | Forma exacta y significado de `esNuevo` en `/me`: duda 6. |

- **JWT propio:** HS256, 15 min, `ClockSkew` cero, `ValidAlgorithms = [HS256]`, claims `sub` (id interno) y `jti`; clave de al menos 32 bytes desde user-secrets o variable de entorno. Issuer, audience y `role`: duda 7.
- **Refresh token:** valor aleatorio de 256 bits (según `auth-firebase-jwt`); en este spike solo se devuelve, sin canje (duda 8).
- **Ruta:** la ficha, HU-001 y TT-PRU-04 usan `/auth/firebase`; la skill `auth-firebase-jwt` dice `/auth/session` (duda 1).
- **Contrato vivo:** esta tarea agrega los primeros endpoints de negocio; habría que regenerar `docs/openapi.v1.json` y actualizar `docs/endpoints.md` (hoy no existen). Ver duda 9.
- **Eventos:** ninguno.

## 5. Modelo de datos
Depende de la duda 2: la API aún no tiene `DbContext` (TT-BD-01 solo dejó el Compose de PostgreSQL/PostGIS; EF Core, migraciones y `DbContext` quedaron fuera de TT-API-01). Datos mínimos del usuario del spike: id interno (GUID), `uid` de Firebase y teléfono (dato personal; tratamiento si se persiste: duda 13). Sin tablas nuevas mientras la duda 2 no se resuelva.

## 6. Seguridad, privacidad y amenazas
STRIDE breve. Esta tarea toca autenticación, tokens y sesión (`security-core`, checks 2, 3, 6, 7, 11, 13 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Suplantación: ID token falsificado o alterado para entrar como otro teléfono | Validación con el Admin SDK (`VerifyIdTokenAsync`: firma, proyecto, vencimiento); teléfono y `uid` se toman solo del token, nunca del cuerpo | TC-TT-API-02-02, TC-TT-API-02-04 |
| Suplantación: reutilizar un token vencido | Firebase rechaza el vencido; JWT propio con `ClockSkew` cero | TC-TT-API-02-03, TC-TT-API-02-08 |
| Suplantación: tokens del emulador (sin firma) aceptados en un entorno real | El emulador solo en Development y pruebas (control propuesto; duda 1) | TC-TT-API-02-13 |
| Alteración: JWT propio manipulado, `alg: none` o firmado con otra clave | `ValidAlgorithms = [HS256]`, validación de firma, vida y `ClockSkew` cero | TC-TT-API-02-08 |
| Alteración: el cliente elige su id o su teléfono en el cuerpo | El cuerpo solo trae el ID token; `sub` es el id interno generado por el servidor | TC-TT-API-02-07, TC-TT-API-02-09 |
| Repudio: sin rastro del acceso | Registro con TraceId y sin datos personales (la auditoría completa de sesiones es de HU-001) | TC-TT-API-02-12 (sin PII en el registro); auditoría de eventos sensibles: Out |
| Fuga: ID token, teléfono, JWT o refresh token en logs o errores | Serilog y ProblemDetails sin esos valores; mensaje uniforme que no revela el motivo del rechazo | TC-TT-API-02-12 |
| Fuga: la clave del JWT o la cuenta de servicio de Firebase en el repositorio, en logs o en prompts de IA | user-secrets o variables; gitleaks en CI; regla de proceso (nada de secretos a la IA, ficha §8) | TC-TT-API-02-11; `security-reviewer` |
| Fuga: números reales o códigos de prueba en el repositorio o la spec | Solo números de prueba ficticios y sin SMS; los códigos se guardan fuera del repositorio (KNOCK-28) | TC-TT-API-02-14 (evidencia sin códigos); `security-reviewer` |
| Denegación: endpoint público de canje sin límite de peticiones ni de tamaño del cuerpo | Validación de tamaño del campo (caso 10). **Rate limit: Out** (pipeline de seguridad y HU-001); riesgo aceptado solo mientras la API corra en local; no desplegarla antes (duda 5) | TC-TT-API-02-10 (solo tamaño) |
| Elevación: acceso a `/me` sin sesión o con la sesión de otro | `[Authorize]`; el id sale del `sub` del JWT, no de un parámetro | TC-TT-API-02-05, TC-TT-API-02-06 |
| Costo: SMS reales o números reales | Solo números de prueba de Firebase; ningún SMS real (ficha §8) | TC-TT-API-02-14 |

No aplican aquí: autorización por recurso más allá de `/me`, cifrado de campo (si no se persiste el teléfono), biométricos, archivos, IA y SSRF (el backend no descarga URLs del usuario).

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Emisión del JWT (caso 07), `UnitTest/Identity/`. |
| Arquitectura | Sí, existentes | Deben seguir en verde `ModuleBoundaryTests` y `ModuleSurfaceTests` de TT-API-01 (duda 12 sobre el controller público). |
| Integración | Sí | `WebApplicationFactory` (casos 01, 05, 06, 09); el origen del ID token depende de la duda 1. Sin Testcontainers mientras no haya base de datos (duda 2). |
| Seguridad negativa | Sí | Casos 02, 03, 04, 08, 10, 11, 12 y 13 en `tests/SecurityTest`. |
| Contrato | Duda 9 | Hay endpoints nuevos pero aún no existen `docs/openapi.v1.json` ni `tests/ContractTest`. |
| Base de datos | No (provisional) | Sin tablas hasta resolver la duda 2. |
| Mutación | No en esta tarea | Meta del módulo Identity ≥ 80 % (nightly) cuando exista su lógica real en HU-001; el spike es de 2 puntos. |
| Rendimiento, fuzzing, caos, E2E, UI | No | Spike sin límites ni UI; el fuzzing del contrato llega con HU-001. |
| Smoke manual | Sí | Caso 14 contra el proyecto real de Firebase. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit, Integration y Security en verde, `sdd_lint.py --trace TT-API-02`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Evidencia en el PR: salida de `dotnet test` y la del caso 14.

**Decisiones ya tomadas (de `Memoria.md` y del revisor):**
1. «JWT con clave efímera» se trasladó de TT-API-01 a esta tarea.
2. El pipeline de seguridad de la constitución §4 no va aquí: es otra tarea con su spec.
3. El proyecto de Firebase y los números de prueba (KNOCK-28) los configuró una persona; evidencia en `docs/evidencia/KNOCK-28-firebase.md`. Los códigos y llaves no van al repositorio ni a la spec.
4. Docker y los servicios externos los opera una persona; los paquetes nuevos requieren evaluación y registro.

**Límites y dudas abiertas (para el revisor; no resueltas, no inventadas):**
1. **Cómo obtiene el ID token la prueba de integración, y ruta y campo de la petición.** La ficha (V1) pide un token real de un número de prueba, obtenido con la REST API de Identity Toolkit, lo que necesita red, la llave web del proyecto y el código del número, y por tanto no corre en el CI sin secretos. Opciones: (A) emulador oficial de Firebase Auth (`FIREBASE_AUTH_EMULATOR_HOST`, como dice la skill; AGENTS §2 lo menciona pero `deploy/docker-compose.local.yml` hoy solo tiene PostgreSQL, y Docker lo inicia una persona; agrega el caso 13); (B) prueba contra el proyecto real, opcional y omitida si faltan las variables, más el caso 14 como evidencia; (C) un validador falso detrás de una interfaz (no prueba a Firebase). Propuesta del redactor: A para el CI y el caso 14 como evidencia manual; decide el revisor. Aparte: la ruta `/auth/firebase` (ficha, HU-001, TT-PRU-04) difiere de `/auth/session` (skill); se propone la de la ficha. El nombre del campo (`firebaseIdToken` en la skill) tampoco lo define la ficha.
2. **Dónde se guarda el usuario («crea el usuario si no existe», `esNuevo`).** No hay `DbContext`. Opciones: (a) almacén en memoria solo para el spike, descartable (contradice «no estado de negocio en memoria del proceso», `AGENTS.md §4`, y exige excepción del revisor; el caso 09 y `esNuevo` solo valen mientras viva el proceso); (b) tabla mínima con migración, que mete capa [BD] y EF Core/Npgsql (paquetes) en una tarea de 2 puntos; (c) sin persistencia: el id se deriva del `uid` de Firebase y `esNuevo` no se prueba. Hasta decidir, el caso 09 y §5 quedan provisionales.
3. **«JWT con clave efímera».** La ficha pone la clave en user-secrets; la decisión trasladada habla de clave efímera. ¿Significa generar una clave aleatoria al arrancar solo en Development y en las pruebas (las sesiones mueren al reiniciar y no sirve con varias instancias), y que fuera de Development la API no arranque sin clave configurada? ¿O solo la efímera, sin user-secrets? Afecta al caso 11.
4. **Código HTTP** cuando Firebase valida el token pero falta `phone_number` o es de otro proyecto (caso 04): ¿401, igual que token inválido, o 400/403? Se propone 401.
5. **Límites del endpoint:** tamaño máximo del campo del ID token (caso 10) y si se acepta no tener rate limit hasta la tarea del pipeline de seguridad y HU-001, siempre que la API solo corra en local.
6. **`GET /me`:** ¿`esNuevo` significa «el usuario se creó en este canje» (obligaría a llevarlo como claim del JWT) o «aún no completó su perfil»? La ficha dice solo «el id y si es nuevo». Y la forma del JSON (`id`, `esNuevo`).
7. **Claims e identificadores del JWT:** valores de issuer y audience (la skill los pide; la ficha no los define) y si se incluye `role` (la skill lo lista; aún no hay roles). Se propone `sub` y `jti` solamente.
8. **Refresh token del spike:** se entrega un valor aleatorio de 256 bits, sin persistirse ni canjearse (todo eso es Out). ¿Se acepta que sea solo el valor, sin guardar su SHA-256?
9. **Contrato vivo:** `docs/endpoints.md` y `docs/openapi.v1.json` no existen en el repositorio y `scripts/export_openapi.py` no está en `scripts/`. ¿Se crean en esta tarea o con la primera HU? Si no, el contrato de §4 vive solo en esta spec y no hay `tests/ContractTest`. Además, esta tarea cambiaría el contrato: ¿se dispara `contract-publish.yml` hacia knock-frontend?
10. **Ruta de la nota del spike:** la ficha dice `docs/specs/spike-firebase.md`, pero `docs/specs/` no existe (las specs viven en `specs/`) y `docs/` tiene `evidencia/`, `architecture/`, etc. ¿Se confirma esa ruta o se usa `docs/evidencia/KNOCK-21-spike-firebase.md`, como la nota de KNOCK-28? Los casos 14 (evidencia manual) y 15 (revisión del PR) no tienen prueba de código: confirmar cómo se marcan.
11. **Caso 16 (build y formato):** se verifica por comando; el revisor decide si lleva la marca «(comando)» tras aprobar.
12. **Controller público en el módulo Identity.** MVC solo descubre controllers públicos, pero la prueba aprobada `ModuleSurfaceTests` (TC-TT-API-01-09) exige exactamente dos tipos públicos por módulo (`AddXxxModule` y `IXxxApi`); TT-API-01 lo resolvió con un controller de prueba fuera de `src`. Opciones: (a) ajustar esa prueba para permitir controllers públicos (cambia una spec aprobada de otra tarea); (b) minimal API o `MapPost` internos registrados desde `AddIdentityModule` (se aparta de «controllers delgados», `AGENTS.md §6`); (c) controllers en `Hosting/` o `Common/`.
13. **Tratamiento del teléfono si se persiste:** ¿en claro, cifrado de campo o solo HMAC? (`security-core` pide cifrar celular y dirección; CURP solo HMAC). Depende de la duda 2.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
