# HU-003 — Mantener mi sesión segura (renovación del token)

> **Jira:** KNOCK-45 · **Épica:** EP-01 Acceso y cuenta · **Servicio:** Identidad y confianza (`Knock.Identity.Api`, ADR-001; hoy módulo Identity en `src/Knock.Api/`) · **Responsable:** José Guillermo Rangel Hernández (como en la ficha) · **Sprint:** demo de Google Play y evaluación de la segunda unidad (la ficha dice S1) · **Puntos:** 3 · **Capas:** BD y Back (este repositorio) y Mobile (knock-frontend: interceptor de `dio`)
> **Repositorio:** knock-backend (capas [BD] y [Back]) · **Otra parte en:** knock-frontend (capa [Mobile]) · **Ficha:** `docs/backlog/HU-003.md` (generada; no se edita a mano)
> **Depende de:** **HU-001 (KNOCK-43)**: crea `refresh_tokens`, la huella y el JWT. **Su código está en la rama `feature/KNOCK-43-phone-login-code`; el PR #75 se cerró sin fusionar y hay que reabrirlo y fusionarlo antes de implementar esta spec** · TT-API-03 (KNOCK-37, ProblemDetails, 422, `X-Correlation-Id` y límite por IP; fusionado) · TT-BD-06 (KNOCK-156, cliente de Mongo `AddKnockMongo`; fusionado)
> **Fuentes:** `docs/backlog/HU-003.md` (CA1 a CA6) · spec HU-001 (§4 contrato del canje, §5 `refresh_tokens`, decisiones 3 y 9) · constitución 2.2.x (principios 2, 3, 13, 15; §3 «Acceso»; §6; §12) · ADR-001 (regla 3) · ADR-002 (regla 4) · `docs/DECISION_TECNICA.md` §8 · código de la rama de HU-001 (`RefreshTokenFingerprint`, `SessionTokenIssuer`, `IdentityStore`)
> **Skills:** story-workflow, spec-first-writing, auth-firebase-jwt, database-scripts, vertical-slice-api, security-core, api-contract-openapi, testing-strategy-backend, reuse-before-create
> **Estado:** Borrador

## 1. Objetivo
Que la sesión siga viva sin pedir otro código SMS: la app cambia, en segundo plano, su refresh token (30 días) por un par nuevo, y **cada uso entrega un refresh nuevo y deja inservible el anterior** (rotación). Si alguien presenta un refresh que ya fue reemplazado, se **revoca toda su familia** y hay que entrar de nuevo. Este repositorio entrega `POST /api/v1/auth/refresh`, la rotación atómica y la detección de reuso; el interceptor de la app es de knock-frontend.

**Terminada cuando** (ficha, CA1 a CA5): un refresh válido devuelve un access token y un refresh nuevos y el anterior deja de servir; el reuso de uno reemplazado revoca la familia; uno vencido, desconocido o ajeno al dispositivo responde 401 sin crear nada; una cuenta suspendida no recibe tokens; y ningún token, huella ni `deviceId` sale en logs ni respuestas. **CA6 (revocar un access token antes de vencer) pasa a HU-004** y es opcional si hay Redis (§8, duda 5).

**Qué ya existe y qué es nuevo** (verificado en la rama de HU-001):

| Pieza | En la rama de HU-001 | En esta historia |
|---|---|---|
| Refresh token | 32 bytes aleatorios en Base64Url; en la base solo la huella SHA-256 (`RefreshTokenFingerprint`); `refresh_tokens` con `familiaId`, `dispositivoId`, `creadoEn`, `venceEn` | **Se usa:** se busca por huella, se marca como reemplazado y se inserta el siguiente de la misma familia |
| Emisión | `SessionTokenIssuer.Issue(Guid)`: JWT HS256 de 15 min con `sub` y `jti` nuevos | **Se reutiliza** sin cambios (nuevo `jti` en cada renovación) |
| Controller | `AuthController` solo tiene `POST /auth/firebase` | **Se agrega** `POST /auth/refresh` |
| Almacén | `IIdentityStore` solo agrega huellas | **Se extiende** con la rotación atómica y la revocación de familia |

## 2. Historia y casos de prueba
**HU-003** — Historia de usuario (capas BD, Back y Mobile). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_HU_003_KK`.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-HU-003-01 | Dado un refresh token válido y su `deviceId`, cuando se pide renovar, entonces responde 200 `{ accessToken, refreshToken, esNuevo: false, perfilCompleto }` con un access token de 15 min (nuevo `jti`, mismo `sub`) y un refresh distinto del anterior. | `IntegrationTest/Identity/RefreshTests.ValidRefresh_ReturnsNewPair_TC_HU_003_01` |
| TC-HU-003-02 | Dada una renovación correcta, cuando se revisa `refresh_tokens`, entonces el documento anterior tiene `reemplazadoPor` apuntando al nuevo, el nuevo conserva `familiaId` y `dispositivoId`, `venceEn` = ahora + 30 días y solo existe su huella SHA-256 (calculada por una implementación independiente), nunca el token. | `DatabaseTest/Identity/RefreshRotationTests.RotationLinksTokensAndStoresOnlyFingerprint_TC_HU_003_02` |
| TC-HU-003-03 | Dado un refresh ya reemplazado, cuando alguien lo presenta, entonces responde 401, **todos** los documentos de su familia quedan con `revocadoEn` y `motivoRevocacion: "reuso"`, y el refresh más nuevo de esa familia también deja de servir (401). | `IntegrationTest/Identity/RefreshTests.ReusedToken_RevokesWholeFamily_TC_HU_003_03` |
| TC-HU-003-04 | Dado un refresh con `venceEn` en el pasado (reloj de pruebas), cuando se pide renovar, entonces responde 401 y no se crea ningún documento. | `IntegrationTest/Identity/RefreshTests.ExpiredToken_Returns401_TC_HU_003_04` |
| TC-HU-003-05 | Dado un refresh desconocido o alterado (32 bytes cualesquiera), cuando se pide renovar, entonces responde 401 con el **mismo** cuerpo que el caso 03 y 04 (sin distinguir motivo) y sin tocar la base; y uno que no es Base64Url de 32 bytes responde 422 con `errors.refreshToken` sin repetir el valor. | `SecurityTest/Identity/RefreshLeakTests.UnknownOrMalformedToken_SameBodyNoEcho_TC_HU_003_05` |
| TC-HU-003-06 | Dado un refresh válido pero un `deviceId` distinto del de su familia, cuando se pide renovar, entonces responde 401 y la familia se revoca (`motivoRevocacion: "dispositivo"`). | `IntegrationTest/Identity/RefreshTests.OtherDevice_Returns401AndRevokes_TC_HU_003_06` |
| TC-HU-003-07 | Dada una cuenta `suspendido`, cuando se pide renovar con un refresh válido, entonces responde 403 con `code: "account_suspended"`, no entrega tokens y la familia queda revocada (`motivoRevocacion: "suspension"`). | `IntegrationTest/Identity/RefreshTests.SuspendedAccount_Returns403NoTokens_TC_HU_003_07` |
| TC-HU-003-08 | Dadas dos renovaciones simultáneas con el **mismo** refresh, cuando terminan, entonces nunca hay dos respuestas 200 para el mismo padre: la rotación es atómica (un solo ganador) y el perdedor se trata como reuso (caso 03). | `IntegrationTest/Identity/RefreshConcurrencyTests.TwoParallelRenewals_OnlyOneWins_TC_HU_003_08` |
| TC-HU-003-09 | Dado cualquier resultado, cuando se revisa la respuesta, entonces lleva `Cache-Control: no-store`, `X-Correlation-Id` y, si es error, ProblemDetails con `traceId` y `correlationId` y sin tokens. | `IntegrationTest/Identity/RefreshTests.Responses_AreNoStoreAndProblemDetails_TC_HU_003_09` |
| TC-HU-003-10 | Dadas 11 peticiones a `/auth/refresh` en un minuto desde la misma IP, cuando llega la número 11, entonces responde 429 con `Retry-After`, antes de consultar la base (límite de `/auth/*` de TT-API-03). | `IntegrationTest/Identity/RefreshTests.EleventhRequestPerMinute_Returns429_TC_HU_003_10` |
| TC-HU-003-11 | Dado un cuerpo inválido (sin `refreshToken` o `deviceId`, vacíos, `deviceId` que no es UUID de 36), cuando se envía, entonces responde 422 con `errors` por campo sin repetir el valor; JSON ilegible → 400; cuerpo de más de 8 KiB → 413. | `UnitTest/Identity/RefreshRequestValidatorTests.InvalidBodies_ProduceFieldErrors_TC_HU_003_11` |
| TC-HU-003-12 | Dadas las renovaciones de los casos 01 a 11, cuando se captura la salida de logs, entonces no aparece ningún refresh token, huella, `deviceId`, `sub` ni access token. | `SecurityTest/Identity/RefreshLeakTests.NoSecretsInLogs_TC_HU_003_12` |
| TC-HU-003-13 | Dada la colección `refresh_tokens`, cuando se crean los índices, entonces existen `{ huella: 1 }` único, `{ familiaId: 1 }` y `{ venceEn: 1 }` TTL, y repetir la creación no cambia nada; los documentos revocados o reemplazados se conservan hasta `venceEn` (así se detecta el reuso). | `DatabaseTest/Identity/RefreshIndexesTests.IndexesExistAndAreIdempotent_TC_HU_003_13` |
| TC-HU-003-14 | Dado el contrato, cuando se genera el OpenAPI, entonces `POST /api/v{version}/auth/refresh` declara 200, 400, 401, 403, 413, 422 y 429 con ProblemDetails y las dos excepciones de §4. | `ContractTest/Identity/AuthContractTests.RefreshIsDeclared_TC_HU_003_14` |
| TC-HU-003-15 | Dada la app, cuando una petición falla con 401 por access token vencido, entonces el interceptor renueva **una vez** con una cola (sin renovar en paralelo), repite la petición y, si la renovación falla, lleva a entrar con el celular. Tokens solo en almacenamiento seguro. | (otro repo) knock-frontend: pruebas del interceptor |
| TC-HU-003-16 | Dado el trabajo terminado, cuando se compila en Release con análisis estricto y se verifica el formato, entonces hay 0 errores y 0 warnings. | (comando) `dotnet build Knock.slnx -c Release --no-restore -warnaserror` y `dotnet format Knock.slnx --verify-no-changes`; salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `AuthController`: acción `POST /auth/refresh` (público: el access token puede estar vencido), con `[RequestSizeLimit(8192)]`.
  - Servicio de renovación en `Modules/Identity/Services/` y validador FluentValidation del cuerpo.
  - `IIdentityStore` e `IdentityStore`: rotación atómica (transacción de MongoDB), revocación de familia, lectura de la cuenta para saber si está `suspendido`.
  - Campos `reemplazadoPor`, `revocadoEn` y `motivoRevocacion` en `refresh_tokens` (HU-001 los dejó previstos) y el índice `{ familiaId: 1 }`.
  - Pruebas de los casos 01 a 14 y 16; entrada en `Memoria.md`.
- **Out:**
  - **Cerrar sesión y revocar el access token** (HU-004). **Ver y cerrar sesiones de otros dispositivos** (HU-007).
  - Interceptor y almacenamiento seguro de la app (capa Mobile, knock-frontend).
  - Lista de revocados en Redis (opcional, solo en HU-004).
  - Cambiar el JWT, el canje de `/auth/firebase` o el límite por IP.
  - `docs/openapi.v1.json` (archivo protegido; lo regenera TT-CI-02 o el líder).

## 4. Contratos API y eventos
Errores en ProblemDetails (RFC 9457, `application/problem+json`) con `traceId` y `correlationId` (TT-API-03). Desde fuera se llegará por el gateway (ADR-001 regla 3).

**`POST /api/v1/auth/refresh`** — público (`[AllowAnonymous]`), cuerpo máximo 8 KiB, sin `Idempotency-Key`.

| Elemento | Detalle |
|---|---|
| Cuerpo | `{ refreshToken: string (Base64Url de 32 bytes, 43 caracteres), deviceId: string (UUID de 36 caracteres, el mismo del canje) }` |
| `200 OK` | `{ accessToken, refreshToken, esNuevo: false, perfilCompleto }`: el **mismo esquema** que el canje de HU-001 (un solo esquema para el cliente generado). El refresh anterior queda reemplazado. |
| `401` | Refresh desconocido, alterado, vencido, ya reemplazado o revocado, o de otro dispositivo: **mismo cuerpo siempre** (`Unauthorized`, `Invalid credentials.`). Si fue reuso o dispositivo distinto, la familia queda revocada. |
| `403` | Cuenta suspendida. ProblemDetails con `code: "account_suspended"`; sin tokens ni datos de la cuenta. |
| `413` / `400` | Cuerpo mayor de 8 KiB / JSON ilegible. |
| `422` | Campos inválidos (`errors` por campo, sin repetir el valor). |
| `429` | Límite por IP de `/auth/*` (10 por minuto, TT-API-03) con `Retry-After`. |
| Encabezados | `Cache-Control: no-store` en toda respuesta; `X-Correlation-Id`. |

**Excepciones a la constitución (se documentan en el OpenAPI y en `docs/endpoints.md`):** principio 15 (`Idempotency-Key`): la renovación no lo exige; es de un solo uso por diseño (un segundo uso del mismo refresh es reuso y revoca). Principio 11 (paginación): no aplica. Compresión (§12): `/auth/*` no se comprime (BREACH).

**Eventos:** ninguno.

## 5. Modelo de datos
Base lógica **`identity`** (`Mongo:Databases:Identity`). Acceso solo desde `Modules/Identity/Data/`. **`refresh_tokens`** — `cache: no` (sesión; principio 10).
```
{ _id: ObjectId, usuarioId: ObjectId, familiaId: ObjectId, huella: BinData(32), dispositivoId: string(36),
  creadoEn: fecha UTC, venceEn: fecha UTC,
  reemplazadoPor: ObjectId | null, revocadoEn: fecha UTC | null,
  motivoRevocacion: "reuso" | "dispositivo" | "suspension" | "logout" | null }
```
- Se agregan `reemplazadoPor`, `revocadoEn` y `motivoRevocacion` (nacen en `null`); los escribe esta historia y HU-004.
- Índices: `{ huella: 1 }` **único** (HU-001) · `{ venceEn: 1 }` **TTL** con `expireAfterSeconds: 0` (HU-001) · `{ familiaId: 1 }` (nuevo, para revocar la familia).
- **Rotación (una transacción):** `findOneAndUpdate({ huella, reemplazadoPor: null, revocadoEn: null, venceEn: { $gt: ahora } }, { $set: { reemplazadoPor: <_id nuevo> } })`; si devuelve un documento, se inserta el siguiente (misma `familiaId` y `dispositivoId`, huella nueva, `venceEn` = ahora + 30 días) y se confirma; si no devuelve nada, se lee el documento por huella: reemplazado o revocado → reuso; vencido o inexistente → 401. Un fallo en cualquier escritura no deja nada consumido.
- **Revocar la familia:** `updateMany({ familiaId, revocadoEn: null }, { $set: { revocadoEn: ahora, motivoRevocacion } })`.
- **Flujo:** validar el cuerpo → calcular la huella → rotar → (si no hay documento) 401 uniforme → comparar `deviceId` → leer el estado de la cuenta (`suspendido` → 403 y revocar) → emitir el JWT con `SessionTokenIssuer` → responder. Los rechazos 401 ocurren antes de consultar cuentas.
- Migración / semillas: ninguna (los campos nuevos son `null` por defecto; los documentos de HU-001 se leen como no reemplazados).

## 6. Seguridad, privacidad y amenazas
STRIDE sobre un endpoint público que canjea un secreto de larga vida (`security-core`, checks 2, 3, 6, 11, 13, 14 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Suplantación (S):** robo y uso del refresh token | Rotación en cada uso: quien lo robó y quien es dueño no pueden usarlo los dos; si el robado se usa después del legítimo (o al revés) se detecta el reuso y se revoca la familia | TC-HU-003-03 |
| **Suplantación (S):** refresh usado desde otra instalación | Vinculado al `deviceId` de su familia; distinto = 401 y revocación (`deviceId` lo genera la app y no se autentica: solo detecta otra instalación) | TC-HU-003-06 |
| **Alteración (T):** carrera para obtener dos hijos del mismo refresh | Rotación atómica con filtro `reemplazadoPor: null` en una transacción; un solo ganador | TC-HU-003-08 |
| **Alteración (T):** cuerpo mal formado o con intento de inyección | FluentValidation con 422 por campo; filtros tipados del driver, nunca JSON armado con la entrada | TC-HU-003-11 |
| **Repudio (R):** sin rastro de por qué se cerró una sesión | `motivoRevocacion` y `revocadoEn` en cada documento; log estructurado sin datos personales ni secretos | TC-HU-003-03 · TC-HU-003-06 · TC-HU-003-07 |
| **Fuga (I):** refresh token, huella o `deviceId` en logs o respuestas | Solo la huella en la base; el token solo viaja en la respuesta 200; logs sin esos valores; 401 uniforme | TC-HU-003-05 · TC-HU-003-12 |
| **Fuga (I):** enumeración de tokens válidos | Los rechazos 401 tienen cuerpo y tiempo equivalentes y ocurren antes de consultar cuentas | TC-HU-003-05 |
| **Fuga (I):** tokens en caché HTTP o intermediarios | `Cache-Control: no-store`; sin compresión de `/auth/*` | TC-HU-003-09 |
| **Denegación (D):** ráfagas o fuerza bruta contra el refresh | Límite por IP de `/auth/*` antes de autenticar (429 con `Retry-After`); 256 bits aleatorios hacen inviable adivinar | TC-HU-003-10 |
| **Denegación (D):** varias personas tras la misma IP (NAT) comparten el límite | Riesgo aceptado, ya anotado en TT-API-03; con renovaciones cada 15 minutos por persona casi no pesa | TC-HU-003-10 |
| **Elevación (E):** cuenta suspendida que sigue renovando | Se lee `estado` en cada renovación; `suspendido` = 403 y familia revocada | TC-HU-003-07 |
| **Elevación (E):** reuso inocente (mala señal, dos renovaciones a la vez) cierra la sesión legítima | Riesgo aceptado (duda 1): el interceptor de la app serializa las renovaciones | TC-HU-003-08 · TC-HU-003-15 |

Datos personales tocados: ninguno nuevo (solo huellas y metadatos de sesión). Residual: un access token ya emitido vale hasta 15 min aunque se revoque su familia (ver HU-004).

## 7. Pantallas
— (backend). El interceptor de renovación y el manejo de «sesión caducada» son de knock-frontend (caso 15).

## 8. Pruebas, gates y límites
**Plan de pruebas:** Unitaria (validador y reglas de rotación con reloj de pruebas), Integración (renovación real con `documentdb-local` por Testcontainers), Seguridad negativa (reuso, vencido, otro dispositivo, suspendido, fugas), Base de datos (índices y rotación), Contrato y Mutación de las reglas de rotación (meta 80 %). No aplican rendimiento, usabilidad ni accesibilidad en este repositorio. La suite de arquitectura y el análisis de seguridad del CI corren siempre.
**Gates:** `AGENTS.md §5`: build, formato, análisis estático y pruebas en verde; `sdd_lint.py --trace HU-003`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK; `gitleaks` limpio; `Memoria.md` al día; «Uso de IA» completo en el PR.
**Límites y dudas abiertas** (recomendación entre paréntesis; las decide el líder):
1. **Reuso por mala señal.** Si la app renueva, la respuesta se pierde y reintenta con el mismo refresh, parece reuso y cierra la sesión. (Recomendación: sin ventana de gracia; el interceptor serializa las renovaciones y, si pierde la respuesta, la persona vuelve a entrar con el SMS. Una ventana de 10 s abriría un hueco de robo.)
2. **Vigencia.** (Recomendación: ventana **deslizante**: cada renovación pone `venceEn` = ahora + 30 días, como pide la constitución: la sesión se pierde por logout, reuso o 30 días sin abrir la app.)
3. **Vincular al `deviceId`.** (Recomendación: sí; es un dato del cliente y no un control fuerte, pero detecta un refresh usado desde otra instalación.)
4. **Cuenta suspendida.** (Recomendación: 403 con `account_suspended` y revocar la familia, para que al reactivarla se entre de nuevo con el SMS.)
5. **Revocar un access token (CA6 de la ficha).** (Recomendación: pasa a HU-004 y solo si hay Redis configurado; sin Redis, un access token vale hasta 15 min tras revocar su familia: riesgo aceptado en la constitución §12.)
6. **Esquema de respuesta.** (Recomendación: reutilizar `SessionResponse` con `esNuevo: false`; un solo esquema para el cliente generado.)
7. **Dependencia.** HU-001 no está en `sprint-02`: el PR #75 se cerró sin fusionar. (Hay que reabrirlo, actualizarlo con `sprint-02`, unir `AuthNoStoreMiddleware` con `SecurityHeadersMiddleware` y fusionarlo antes de escribir código de esta historia.)

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
