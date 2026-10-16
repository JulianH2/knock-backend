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
- `POST /api/v1/auth/firebase`: valida el ID token de Firebase y responde `{ accessToken, refreshToken }`.
- JWT propio (HS256, 15 min) con `sub` determinista derivado del `uid` de Firebase; `GET /api/v1/me` protegido con `[Authorize]` que responde `{ id }`.
- Pruebas de integración y de seguridad negativa contra el emulador oficial de Firebase Auth, y el canje real contra el proyecto `knock-0` como evidencia manual (caso 15).
- Nota del spike en `docs/evidencia/KNOCK-21-spike-firebase.md` con qué funcionó, qué falta para HU-001 y riesgos.

**Queda fuera:** ver §3 Out. Diferencias con la ficha, por decisión del revisor: no hay persistencia de usuarios ni `esNuevo` (pasan a HU-001) y la respuesta es `{ accessToken, refreshToken }`; la ficha es un documento generado y no se edita.

## 2. Historia y casos de prueba
**TT-API-02** — Tarea técnica (no es historia de usuario). Los casos 01 a 14 se verifican con pruebas de código; el 15, el 16 y el 17 con comandos (evidencia en el PR). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_API_02_KK`. Los casos 01, 02, 03, 05, 06 y 08 cubren V1, V2 y V3 de la ficha; el resto sale de los pasos de la ficha, de las decisiones del revisor y de `security-core`.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-02-01 | Dado un ID token válido de un usuario de prueba creado en el emulador de Firebase Auth (V1), cuando se hace `POST /api/v1/auth/firebase` con `{ firebaseIdToken }`, entonces responde 200 con `accessToken` y `refreshToken` (no vacíos y distintos entre sí). | `IntegrationTest/Identity/FirebaseSessionTests.ExchangeValidIdToken_Returns200WithSession_TC_TT_API_02_01` |
| TC-TT-API-02-02 | Dado un ID token de Firebase alterado o malformado, cuando se canjea, entonces responde 401 `application/problem+json` sin emitir tokens (V2). | `SecurityTest/Identity/FirebaseTokenRejectionTests.TamperedIdToken_Returns401_TC_TT_API_02_02` (alcance con el emulador: duda 1) |
| TC-TT-API-02-03 | Dado un ID token de Firebase vencido, cuando se canjea, entonces responde 401 `application/problem+json` sin emitir tokens (V2). | `SecurityTest/Identity/FirebaseTokenRejectionTests.ExpiredIdToken_Returns401_TC_TT_API_02_03` |
| TC-TT-API-02-04 | Dado un ID token que Firebase valida pero no trae `phone_number`, o que es de otro proyecto, cuando se canjea, entonces responde 401 con el mismo mensaje y cuerpo que un token inválido (no revela el motivo). | `SecurityTest/Identity/FirebaseTokenRejectionTests.IdTokenWithoutPhoneOrWrongProject_Returns401SameAsInvalid_TC_TT_API_02_04` |
| TC-TT-API-02-05 | Dado `GET /api/v1/me` sin encabezado `Authorization`, cuando se llama, entonces responde 401 (V3). | `IntegrationTest/Identity/MeEndpointTests.Me_WithoutToken_Returns401_TC_TT_API_02_05` |
| TC-TT-API-02-06 | Dado el `accessToken` del canje, cuando se llama a `GET /api/v1/me` con él, entonces responde 200 con `{ id }` igual al `sub`, y el controller de Identity que lo atiende es `internal` y se descubre de verdad (la ruta responde 200 con el `ControllerFeatureProvider` de `Hosting/`, y la prueba afirma por reflexión que el tipo no es público) (V3). | `IntegrationTest/Identity/MeEndpointTests.Me_WithIssuedToken_Returns200AndInternalControllerIsDiscovered_TC_TT_API_02_06` |
| TC-TT-API-02-07 | Dado el JWT emitido, cuando se decodifica, entonces usa HS256, vence a los 15 min, trae `sub` (el GUID derivado, no el `uid` de Firebase), `jti`, `iss` y `aud` (de configuración), y no contiene teléfono ni `role`. | `UnitTest/Identity/SessionTokenIssuerTests.IssuedJwt_HasHs256_15Min_AndExpectedClaims_TC_TT_API_02_07` |
| TC-TT-API-02-08 | Dado un JWT propio alterado, vencido (con `ClockSkew` cero, un segundo después del vencimiento), con `iss` o `aud` incorrectos, con algoritmo `none` o firmado con otra clave, cuando se llama a `/me`, entonces responde 401 (V2). | `SecurityTest/Identity/SessionJwtValidationTests.InvalidJwt_Returns401_TC_TT_API_02_08` |
| TC-TT-API-02-09 | Dado el mismo usuario de prueba (mismo `uid`) canjeado dos veces, cuando se repite el canje, entonces el `sub` es el mismo en los dos JWT (sin persistencia). | `IntegrationTest/Identity/FirebaseSessionTests.SameUidTwice_ReturnsSameSub_TC_TT_API_02_09` |
| TC-TT-API-02-10 | Dado un `uid` conocido y el namespace fijo, cuando se deriva el `sub` (GUID v5, RFC 9562 §5.5; .NET no trae v5 incorporado), entonces coincide con un valor esperado calculado con una implementación independiente (por ejemplo `uuid.uuid5` de Python), y dos `uid` distintos dan GUID distintos. | `UnitTest/Identity/UserIdDerivationTests.KnownVector_MatchesGuidV5_TC_TT_API_02_10` (valor del namespace: duda 2) |
| TC-TT-API-02-11 | Dado un cuerpo vacío, sin `firebaseIdToken`, con valor vacío o de más de 4096 caracteres, o un cuerpo mayor al límite de tamaño del endpoint, cuando se canjea, entonces responde 400 (cuerpo inválido) o 413 (cuerpo demasiado grande) con `application/problem+json` y no llama a Firebase. | `SecurityTest/Identity/FirebaseRequestValidationTests.InvalidOrOversizedBody_IsRejected_TC_TT_API_02_11` (límite en bytes: duda 3) |
| TC-TT-API-02-12 | Dados `appsettings*.json` y el repositorio, entonces no contienen la clave del JWT ni la cuenta de servicio de Firebase; en Development y pruebas se usa la clave de user-secrets si existe y si no una aleatoria generada al arrancar; fuera de Development la API no arranca sin clave configurada de al menos 32 bytes (ni con una más corta). | `SecurityTest/Secrets/JwtKeyConfigurationTests.KeyNotInRepo_EphemeralOnlyInDevelopment_MinLengthOutside_TC_TT_API_02_12` (complementa `AppSettingsSecretsTests`) |
| TC-TT-API-02-13 | Dado un canje correcto y uno rechazado, cuando se revisan los logs y el cuerpo de la respuesta, entonces no aparecen el ID token, el teléfono, el access token ni el refresh token. | `SecurityTest/Identity/SensitiveDataLeakTests.ExchangeAndRejection_DoNotLeakSensitiveData_TC_TT_API_02_13` |
| TC-TT-API-02-14 | Dado un entorno distinto de Development y la variable del emulador de Firebase Auth presente, cuando se arranca la API, entonces no se aceptan tokens del emulador (sin firma). | `SecurityTest/Identity/FirebaseEmulatorGuardTests.NonDevelopment_EmulatorTokensRejected_TC_TT_API_02_14` |
| TC-TT-API-02-15 | Dado el proyecto de Firebase `knock-0` y un número de prueba de TT-FB-01 (sin SMS real), cuando una persona obtiene su ID token con la REST API de Identity Toolkit y lo canjea contra la API local, entonces recibe sesión y `/me` responde 200 (cierra el caso 01 de TT-FB-01). | Comandos `curl` contra Identity Toolkit y la API local (comando); la persona los ejecuta y la evidencia va en el PR, sin códigos, llaves ni tokens |
| TC-TT-API-02-16 | Dada la tarea terminada, entonces existe `docs/evidencia/KNOCK-21-spike-firebase.md` con qué funcionó, qué falta para HU-001 y riesgos; dice que el refresh token todavía no funciona y deja la condición de no desplegar sin rate limit; y no contiene secretos. | Comandos de existencia y de contenido del archivo y `gitleaks` (comando); salida en el PR |
| TC-TT-API-02-17 | Dada la solución `Knock.slnx`, cuando se compila en Release y se verifica el formato, entonces hay 0 errores y 0 warnings y `dotnet format` no produce cambios. | `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes` (comando); salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Módulo `Identity` (`src/Knock.Api/Modules/Identity/`): validación del ID token con el Firebase Admin SDK (`VerifyIdTokenAsync`), derivación del `sub` (GUID v5 con namespace fijo sobre el `uid`), emisión del JWT y del refresh token (valor aleatorio de 256 bits), `POST /api/v1/auth/firebase` y `GET /api/v1/me`. Controllers `internal`.
  - En `Hosting/`: autenticación JwtBearer (`ClockSkew` cero, HS256, `iss` y `aud` validados desde configuración), `UseAuthentication`/`UseAuthorization` y un `ControllerFeatureProvider` que también descubre los controllers `internal` del ensamblado. No se modifica `ModuleSurfaceTests` (TC-TT-API-01-09).
  - Límite de tamaño del cuerpo en el endpoint de canje y `firebaseIdToken` de 4096 caracteres como máximo.
  - Configuración por user-secrets o variables de entorno: clave del JWT, issuer, audience y cuenta de servicio de Firebase (solo para el caso 15); nada en el repositorio.
  - Emulador oficial de Firebase Auth con `npx firebase-tools` y un proyecto `demo-` (sin credenciales ni Docker) para las pruebas de integración y de seguridad; la herramienta se fija por versión.
  - Pruebas de los casos 01 a 14; nota del spike (caso 16).
  - Paquetes: `FirebaseAdmin` y `Microsoft.AspNetCore.Authentication.JwtBearer`, y la herramienta `firebase-tools`. **No se agregan hasta evaluarlos** (necesidad, licencia, mantenimiento, vulnerabilidades) y registrarlos en `Memoria.md`.
- **Out:**
  - Persistencia de usuarios, `esNuevo`, «perfil incompleto» y tratamiento del teléfono (no se guarda; en HU-001, cifrado de campo más HMAC para búsqueda).
  - Rotación, revocación y reuso del refresh token, `blacklist:{jti}` en Redis, cierre de sesión y límites de peticiones (HU-001 y HU-003, Sprint 1). El refresh token solo se entrega; no se guarda su hash ni se puede canjear.
  - Contrato vivo (`docs/openapi.v1.json`, `docs/endpoints.md`, `scripts/export_openapi.py`, `tests/ContractTest`) y su publicación a knock-frontend: pospuesto a HU-001.
  - Pipeline de seguridad de la constitución §4 (SecurityHeaders, ForwardedHeaders, HSTS, HttpsRedirection, CORS, RateLimiter): otra tarea con su spec.
  - `role` y permisos; consentimiento, nombre, zona y foto; cambio de número.
  - Pantallas y cliente de la app (TT-PRU-04 y TT-MOB-*).
  - Proyecto de Firebase, números de prueba y llaves (TT-FB-01, KNOCK-28).
  - Redis, MassTransit, outbox, base de datos y cualquier otro endpoint o evento.
  - Operar Firebase, Azure o Docker, y editar los workflows de CI (`.github/` es protegido): lo hace una persona.

## 4. Contratos API y eventos
| Método y ruta | Auth | Petición | Respuesta | Notas |
|---|---|---|---|---|
| `POST /api/v1/auth/firebase` | pública | `{ firebaseIdToken }` (máx. 4096 caracteres) | 200 `{ accessToken, refreshToken }` · 400 · 401 · 413 | `application/problem+json` en errores; el 401 es el mismo para token inválido, vencido, sin teléfono o de otro proyecto. |
| `GET /api/v1/me` | JWT propio (`[Authorize]`) | — | 200 `{ id }` · 401 | `id` es el `sub` del JWT. |

- **JWT propio:** HS256, 15 min, `ClockSkew` cero, `ValidAlgorithms = [HS256]`, claims `sub` y `jti`; `iss` y `aud` validados con valores de configuración (por ejemplo `knock-api` y `knock-app`); sin `role`. Clave de al menos 32 bytes: user-secrets o variable de entorno; en Development y pruebas, si falta, una aleatoria al arrancar; fuera de Development la API no arranca sin ella.
- **`sub`:** GUID v5 del `uid` de Firebase con un namespace fijo (determinista; mismo `uid`, mismo `sub`).
- **Refresh token:** valor aleatorio de 256 bits; sin hash guardado ni canje (todavía no funciona).
- **Contrato vivo:** no se publica en esta tarea (pospuesto a HU-001); el contrato de este spike vive en esta spec.
- **Eventos:** ninguno.

## 5. Modelo de datos
— Sin persistencia: no hay tablas, `DbContext` ni migración. El teléfono no se guarda ni se registra; el `sub` se calcula a partir del `uid`.

## 6. Seguridad, privacidad y amenazas
STRIDE breve. Esta tarea toca autenticación, tokens y sesión (`security-core`, checks 2, 3, 6, 7, 11, 13 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Suplantación: ID token falsificado o alterado | Validación con el Admin SDK (`VerifyIdTokenAsync`); `uid` y teléfono salen solo del token. La firma real solo se comprueba contra `knock-0` (el emulador no firma) | TC-TT-API-02-02, TC-TT-API-02-15 |
| Suplantación: token vencido, sin teléfono o de otro proyecto | Rechazo con el mismo 401 que un token inválido (sin revelar el motivo); JWT propio con `ClockSkew` cero | TC-TT-API-02-03, TC-TT-API-02-04, TC-TT-API-02-08 |
| Suplantación: tokens del emulador (sin firma) aceptados en un entorno real | El emulador solo en Development y pruebas; fuera, los tokens sin firma se rechazan | TC-TT-API-02-14 |
| Alteración: JWT propio manipulado, `alg: none`, otra clave, `iss` o `aud` ajenos | `ValidAlgorithms = [HS256]`, firma, vida, `ClockSkew` cero, `iss` y `aud` validados | TC-TT-API-02-08 |
| Alteración: el cliente elige su id o su teléfono | El cuerpo solo trae el ID token; `sub` se deriva del `uid` verificado | TC-TT-API-02-07, TC-TT-API-02-09, TC-TT-API-02-10 |
| Fuga/predicción: `sub` determinista derivable del `uid` | El `uid` no es público y el `sub` por sí solo no autoriza nada (hace falta el JWT firmado); el namespace fijo no es un secreto | TC-TT-API-02-10 (derivación estable); riesgo bajo aceptado |
| Repudio: sin rastro del acceso | Registro con TraceId y sin datos personales; la auditoría de sesiones es de HU-001 | TC-TT-API-02-13 |
| Fuga: ID token, teléfono, JWT o refresh token en logs o errores | Serilog y ProblemDetails sin esos valores; 401 uniforme | TC-TT-API-02-13, TC-TT-API-02-04 |
| Fuga: clave del JWT o cuenta de servicio en el repositorio, logs o prompts de IA | user-secrets o variables; gitleaks en CI; la clave aleatoria solo en Development y pruebas; regla de proceso (ficha §8) | TC-TT-API-02-12; `security-reviewer` |
| Fuga: números reales o códigos de prueba en el repositorio o la spec | Solo números de prueba ficticios; los códigos fuera del repositorio (KNOCK-28) | TC-TT-API-02-15 (evidencia sin códigos); `security-reviewer` |
| Denegación: endpoint público de canje con cuerpo enorme o ráfagas | `firebaseIdToken` ≤ 4096 caracteres y límite de tamaño del cuerpo. **Sin rate limit** (pipeline de seguridad y HU-001): se acepta solo mientras la API corra en local; la nota del spike lo deja como condición para no desplegar | TC-TT-API-02-11 (tamaño); condición en TC-TT-API-02-16 |
| Elevación: acceso a `/me` sin sesión o con la de otro | `[Authorize]`; el id sale del `sub` del JWT, no de un parámetro | TC-TT-API-02-05, TC-TT-API-02-06 |
| Elevación/operación: refresh token emitido que parece usable | Es solo un valor aleatorio, sin hash guardado ni canje; la nota dice que todavía no funciona | TC-TT-API-02-16 |
| Costo: SMS reales o números reales | Solo números de prueba de Firebase y el emulador; ningún SMS real (ficha §8) | TC-TT-API-02-15 |

No aplican aquí: autorización por recurso más allá de `/me`, cifrado de campo (no se guarda el teléfono), biométricos, archivos, IA y SSRF (el backend no descarga URLs del usuario).

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas** (Sí/No con motivo; meta de mutación):

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Emisión del JWT y derivación del `sub` (casos 07 y 10), `UnitTest/Identity/`. |
| Arquitectura | Sí, existentes | `ModuleBoundaryTests` y `ModuleSurfaceTests` de TT-API-01 siguen en verde sin cambios (los controllers son `internal`). |
| Integración | Sí | `WebApplicationFactory` contra el emulador de Firebase Auth (casos 01, 05, 06, 09). Sin Testcontainers: no hay base de datos. |
| Seguridad negativa | Sí | Casos 02, 03, 04, 08, 11, 12, 13 y 14 en `tests/SecurityTest`. |
| Contrato | No | Pospuesto a HU-001 (`tests/ContractTest`, `docs/openapi.v1.json`). |
| Base de datos | No | Sin persistencia. |
| Mutación | No en esta tarea | Meta del módulo Identity ≥ 80 % (nightly) cuando exista su lógica real en HU-001. |
| Rendimiento, fuzzing, caos, E2E, UI | No | Spike sin límites ni UI. |
| Smoke manual | Sí | Caso 15 contra `knock-0`. |

Cobertura mínima 60 % líneas / 50 % ramas; no se bajan umbrales.

**Gates:** `AGENTS.md §5`: `build -c Release` 0/0, `dotnet format` sin cambios, Unit, Integration y Security en verde, `sdd_lint.py --trace TT-API-02`, `codemap.py --check`, `check_exceptions.py`, `sync_agents.py --check`, Semgrep, `security-reviewer` LIBERA, `traceability-clerk` sin drift, `Memoria.md` al día. Evidencia en el PR: salida de `dotnet test`, la del caso 15 y las de los casos 16 y 17.

**Decisiones previas (de `Memoria.md` y del revisor):**
1. «JWT con clave efímera» se trasladó de TT-API-01 a esta tarea.
2. El pipeline de seguridad de la constitución §4 no va aquí: es otra tarea con su spec.
3. El proyecto de Firebase y los números de prueba (KNOCK-28) los configuró una persona; evidencia en `docs/evidencia/KNOCK-28-firebase.md`. Los códigos y llaves no van al repositorio ni a la spec.
4. Docker y los servicios externos los opera una persona; los paquetes nuevos requieren evaluación y registro.

**Dudas resueltas por el revisor (revisión del PR #26):**
1. **Origen del token — Resuelto:** emulador oficial de Firebase Auth para integración y CI; el caso 15 queda como evidencia manual contra `knock-0`. Ruta `POST /api/v1/auth/firebase`, campo `firebaseIdToken`. El emulador corre con `npx firebase-tools` y un proyecto `demo-` (sin credenciales ni Docker); se agrega un paso al CI, la herramienta se fija por versión y se evalúa y registra en `Memoria.md`.
2. **Sin persistencia — Resuelto:** el `sub` se deriva de forma determinista del `uid` (GUID v5, namespace fijo); `esNuevo` sale del spike y pasa a HU-001. El caso 09 es «mismo `sub` en los dos canjes».
3. **Clave efímera — Resuelto:** en Development y pruebas, la clave de user-secrets si existe y si no una aleatoria al arrancar; fuera de Development la API no arranca sin una clave de al menos 32 bytes (caso 12).
4. **Caso 04 — Resuelto:** 401 con el mismo mensaje que un token inválido.
5. **Límites — Resuelto:** `firebaseIdToken` máx. 4096 caracteres y límite de tamaño del cuerpo; sin rate limit solo mientras la API corra en local, anotado en la nota del spike como condición para no desplegar.
6. **`GET /me` — Resuelto:** responde `{ id }`; `esNuevo` no aplica sin persistencia y «perfil incompleto» es de HU-001.
7. **Claims — Resuelto:** `sub` y `jti`, más `iss` y `aud` validados con valores de configuración (por ejemplo `knock-api` y `knock-app`); sin `role`.
8. **Refresh token — Resuelto:** solo el valor aleatorio de 256 bits, sin guardar su hash ni canjearlo; la nota debe decir que todavía no funciona.
9. **Contrato vivo — Resuelto:** pospuesto a HU-001; aquí no se publica el contrato a knock-frontend.
10. **Nota del spike — Resuelto:** `docs/evidencia/KNOCK-21-spike-firebase.md`.
11. **Casos de comando — Resuelto:** los casos 15, 16 y 17 llevan «(comando)» y su evidencia va en el PR.
12. **Controller en Identity — Resuelto:** controllers `internal` en el módulo y un `ControllerFeatureProvider` en `Hosting/` que también descubre los controllers `internal` del ensamblado; no se toca `ModuleSurfaceTests`. El caso 06 comprueba que se descubre de verdad.
13. **Teléfono — Resuelto:** no se guarda y no aplica en el spike; para HU-001, cifrado de campo más HMAC para búsqueda.

**Contingencia y tope de tiempo (emulador):** si montar el emulador con `npx firebase-tools` se complica y pasa de medio día de trabajo, se cambia a un validador falso detrás de una interfaz (opción C), el caso 15 sigue como evidencia contra `knock-0`, y se avisa al revisor para ajustar los casos 01 a 04 y quitar el 14.

**Límites y dudas abiertas (nuevas, para el revisor; no resueltas, no inventadas):**
1. **Alcance de los casos 02 a 04 con el emulador.** El Admin SDK en modo emulador acepta tokens sin firma, así que «token alterado» solo puede probarse como token malformado o truncado; la firma real solo se comprueba en el caso 15. Tampoco está confirmado que el modo emulador valide vencimiento y proyecto igual que producción: se confirma al implementar y, si no, el caso afectado pasa al caso 15 o a la contingencia.
2. **Valor del namespace fijo del GUID v5 (caso 10).** Pido que el revisor lo defina (o acepte uno generado una vez y documentado). El valor esperado del vector se calcula con una implementación independiente, no con el código bajo prueba.
3. **Límite de tamaño del cuerpo en bytes (caso 11).** La decisión pide un límite pero no su valor; propongo uno acorde con 4096 caracteres más el JSON (por ejemplo 8 KiB), a confirmar.
4. **Emulador en el CI y en local.** El paso del CI toca `.github/` (protegido): lo agrega una persona. Pendiente de definir: versión fijada de `firebase-tools`, requisitos del runner (Node y, si hicieran falta, otros), y si las pruebas deben fallar (propuesta) o saltarse cuando el emulador no esté disponible; se propone que fallen con un mensaje claro.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
