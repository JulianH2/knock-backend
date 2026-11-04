# HU-004 — Cerrar sesión

> **Jira:** KNOCK-46 · **Épica:** EP-01 Acceso y cuenta · **Servicio:** Identidad y confianza (`Knock.Identity.Api`, ADR-001; hoy módulo Identity en `src/Knock.Api/`) · **Responsable:** Erick Trejo Reséndiz (como en la ficha) · **Sprint:** demo de Google Play y evaluación de la segunda unidad (la ficha dice S1) · **Puntos:** 1 · **Capas:** BD y Back (este repositorio) y Mobile (knock-frontend)
> **Repositorio:** knock-backend (capas [BD] y [Back]) · **Otra parte en:** knock-frontend (capa [Mobile]: confirmación, limpieza del dispositivo, cola de revocación sin conexión) · **Ficha:** `docs/backlog/HU-004.md` (generada; no se edita a mano)
> **Depende de:** **HU-003 (KNOCK-45)**: `refresh_tokens` con `familiaId`, `revocadoEn` y `motivoRevocacion`, e índice por `familiaId`; y por tanto **HU-001 (KNOCK-43)**, cuyo PR #75 se cerró sin fusionar y debe reabrirse y fusionarse antes · TT-API-03 (ProblemDetails, 422, límite por IP) · opcional: Redis (TT-AZ-02) para la lista de `jti` revocados
> **Fuentes:** `docs/backlog/HU-004.md` (CA1 a CA5) · spec HU-001 (decisión 9: HU-001 no revoca tokens) y spec HU-003 · constitución 2.2.x (principio 13 «Logout real»; §3 «Acceso»; §12 «Redis» y su riesgo aceptado) · ADR-002 (regla 4: la lista de revocación) · `docs/DECISION_TECNICA.md` §8
> **Skills:** story-workflow, spec-first-writing, auth-firebase-jwt, database-scripts, vertical-slice-api, security-core, api-contract-openapi, testing-strategy-backend, reuse-before-create
> **Estado:** Borrador

## 1. Objetivo
Que cerrar sesión **cancele la sesión en el servidor**, no solo en el teléfono: `POST /api/v1/auth/logout` revoca de forma permanente la **familia del refresh token** del dispositivo (MongoDB) y, si hay Redis configurado, también el `jti` del access token vigente. Este repositorio entrega el endpoint y la revocación; la confirmación, la limpieza local y la cola de revocación sin conexión son de knock-frontend.

**Terminada cuando** (ficha, CA1 a CA5, parte de servidor): un refresh válido deja de servir tras cerrar sesión (y los demás de su familia también); cerrar sesión es idempotente y no revela si el token existía; las sesiones de otros dispositivos de la misma cuenta siguen vivas; y, con Redis, el access token cerrado responde 401 de inmediato. **Sin Redis, un access token ya emitido vale hasta 15 minutos** (riesgo aceptado en la constitución §12), y eso queda documentado y probado.

**Qué ya existe y qué es nuevo:**

| Pieza | Hoy (rama de HU-001 y spec de HU-003) | En esta historia |
|---|---|---|
| `refresh_tokens` | HU-001 guarda la huella con `familiaId`; HU-003 agrega `reemplazadoPor`, `revocadoEn`, `motivoRevocacion` y el índice por familia | **Se reutiliza:** el logout escribe `revocadoEn` y `motivoRevocacion: "logout"` en toda la familia |
| Controller | `AuthController` con `/auth/firebase` (HU-001) y `/auth/refresh` (HU-003) | **Se agrega** `POST /auth/logout` |
| Revocación de access token | No existe (HU-001, decisión 9) | **Nuevo y opcional:** `revocado:{jti}` en Redis con TTL = vida restante, solo si hay `ConnectionStrings:KnockRedis` |

## 2. Historia y casos de prueba
**HU-004** — Historia de usuario (capas BD, Back y Mobile). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_HU_004_KK`.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-HU-004-01 | Dado un refresh token válido, cuando se cierra sesión, entonces responde 204 sin cuerpo y **todos** los documentos de su familia quedan con `revocadoEn` y `motivoRevocacion: "logout"`. | `IntegrationTest/Identity/LogoutTests.ValidRefresh_Returns204AndRevokesFamily_TC_HU_004_01` |
| TC-HU-004-02 | Dado un cierre de sesión hecho, cuando se usa ese refresh (o el más nuevo de su familia) en `/auth/refresh`, entonces responde 401. | `IntegrationTest/Identity/LogoutTests.RefreshAfterLogout_Returns401_TC_HU_004_02` |
| TC-HU-004-03 | Dado un cierre de sesión ya hecho, cuando se repite con el mismo refresh, entonces responde 204 de nuevo y no cambia ningún documento (idempotente). | `IntegrationTest/Identity/LogoutTests.SecondLogout_IsIdempotent_TC_HU_004_03` |
| TC-HU-004-04 | Dado un refresh bien formado pero desconocido, cuando se cierra sesión, entonces responde 204 con la misma respuesta y sin tocar la base (no se revela si existía). | `SecurityTest/Identity/LogoutLeakTests.UnknownToken_Returns204NoEnumeration_TC_HU_004_04` |
| TC-HU-004-05 | Dado un cuerpo inválido (sin `refreshToken`, vacío o que no es Base64Url de 32 bytes), cuando se envía, entonces responde 422 con `errors.refreshToken` sin repetir el valor; JSON ilegible → 400; cuerpo de más de 8 KiB → 413. | `UnitTest/Identity/LogoutRequestValidatorTests.InvalidBodies_ProduceFieldErrors_TC_HU_004_05` |
| TC-HU-004-06 | Dada una cuenta con dos dispositivos (dos familias), cuando se cierra sesión en uno, entonces el refresh del otro dispositivo sigue renovando (200). | `IntegrationTest/Identity/LogoutTests.OtherDeviceKeepsSession_TC_HU_004_06` |
| TC-HU-004-07 | Dado un access token válido de una persona y el refresh de **otra**, cuando se cierra sesión con ambos, entonces responde 204 y no se revoca ninguna de las dos familias. | `SecurityTest/Identity/LogoutLeakTests.MismatchedOwner_RevokesNothing_TC_HU_004_07` |
| TC-HU-004-08 | Dado Redis configurado y un access token válido en `Authorization`, cuando se cierra sesión, entonces se guarda `revocado:{jti}` con TTL = vida restante del token y una petición posterior con ese access token responde 401. | `IntegrationTest/Identity/LogoutRedisTests.AccessTokenRevokedWithRedis_TC_HU_004_08` |
| TC-HU-004-09 | Dado **Redis no configurado**, cuando se cierra sesión y se usa el access token antes de vencer, entonces responde 200 en `GET /me`: es el riesgo aceptado de §12, y la prueba lo deja afirmado y documentado (`docs/endpoints.md`). | `IntegrationTest/Identity/LogoutTests.WithoutRedisAccessTokenLivesUntilExpiry_TC_HU_004_09` |
| TC-HU-004-10 | Dado cualquier resultado, cuando se revisa la respuesta, entonces lleva `Cache-Control: no-store`, `X-Correlation-Id` y, si es error, ProblemDetails con `traceId` y `correlationId`. | `IntegrationTest/Identity/LogoutTests.Responses_AreNoStoreAndProblemDetails_TC_HU_004_10` |
| TC-HU-004-11 | Dadas 11 peticiones a `/auth/logout` en un minuto desde la misma IP, cuando llega la número 11, entonces responde 429 con `Retry-After`. | `IntegrationTest/Identity/LogoutTests.EleventhRequestPerMinute_Returns429_TC_HU_004_11` |
| TC-HU-004-12 | Dados los casos 01 a 11, cuando se captura la salida de logs, entonces no aparece ningún refresh token, huella, `sub`, `jti` ni access token; el cierre queda como un evento estructurado sin datos personales. | `SecurityTest/Identity/LogoutLeakTests.NoSecretsInLogs_TC_HU_004_12` |
| TC-HU-004-13 | Dado el contrato, cuando se genera el OpenAPI, entonces `POST /api/v{version}/auth/logout` declara 204, 400, 413, 422 y 429 con ProblemDetails y las excepciones de §4. | `ContractTest/Identity/AuthContractTests.LogoutIsDeclared_TC_HU_004_13` |
| TC-HU-004-14 | Dada la app, cuando se confirma «Cerrar sesión», entonces se borran los tokens del almacenamiento seguro y la caché, se vuelve al mapa sin cuenta y, si no hay conexión, la revocación se reintenta al volver la red; un cancelar no cierra nada. | (otro repo) knock-frontend: pruebas de widget y unitarias |
| TC-HU-004-15 | Dado el trabajo terminado, cuando se compila en Release con análisis estricto y se verifica el formato, entonces hay 0 errores y 0 warnings. | (comando) `dotnet build Knock.slnx -c Release --no-restore -warnaserror` y `dotnet format Knock.slnx --verify-no-changes`; salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `AuthController`: acción `POST /auth/logout` (público: la posesión del refresh es la prueba), con `[RequestSizeLimit(8192)]`.
  - Revocación de la familia en `IIdentityStore`/`IdentityStore` (reutiliza la de HU-003) y validador del cuerpo.
  - **Opcional con Redis:** servicio de revocados (`revocado:{jti}`) y comprobación en la validación del JWT; si no hay `ConnectionStrings:KnockRedis`, no se registra y no se exige Redis.
  - Pruebas de los casos 01 a 13 y 15; entrada en `Memoria.md`.
- **Out:**
  - Pantalla de confirmación, limpieza local y cola sin conexión (knock-frontend).
  - Cerrar sesión en todos los dispositivos y ver sesiones (HU-007). Desregistrar el token de notificaciones (HU-050).
  - Auditoría en base de datos del cierre (duda 4). Contenedor de Redis y su contraseña (TT-AZ-02).
  - `docs/openapi.v1.json` (protegido; lo regenera TT-CI-02 o el líder).

## 4. Contratos API y eventos
Errores en ProblemDetails (RFC 9457) con `traceId` y `correlationId` (TT-API-03).

**`POST /api/v1/auth/logout`** — público (`[AllowAnonymous]`), cuerpo máximo 8 KiB, sin `Idempotency-Key`. `Authorization: Bearer` es **opcional**: si viene y es válido, se revoca también su `jti` (solo con Redis).

| Elemento | Detalle |
|---|---|
| Cuerpo | `{ refreshToken: string (Base64Url de 32 bytes, 43 caracteres) }` |
| `204 No Content` | Siempre que el cuerpo esté bien formado, **exista o no** el refresh (idempotente y sin revelar existencia). Si existía, su familia queda revocada. |
| `413` / `400` | Cuerpo mayor de 8 KiB / JSON ilegible. |
| `422` | `refreshToken` ausente o que no es Base64Url de 32 bytes (`errors`, sin repetir el valor). |
| `429` | Límite por IP de `/auth/*` (10 por minuto, TT-API-03) con `Retry-After`. |
| Encabezados | `Cache-Control: no-store` en toda respuesta; `X-Correlation-Id`. |

**Excepciones (documentadas en el OpenAPI y en `docs/endpoints.md`):** principio 15 (`Idempotency-Key`): el logout es idempotente por naturaleza. Principio 13 (lista de revocados **en Redis**): aquí Redis es opcional por configuración; sin Redis la revocación del refresh es permanente en MongoDB y el access token vale hasta 15 min (constitución §12). Principio 11: no aplica. Compresión: `/auth/*` no se comprime.

**Eventos:** ninguno (solo un evento de registro estructurado, sin datos personales).

## 5. Modelo de datos
Base lógica **`identity`**. **`refresh_tokens`** (definida en HU-001 y ampliada en HU-003) — `cache: no` (sesión): el logout escribe `revocadoEn` y `motivoRevocacion: "logout"` con `updateMany({ familiaId, revocadoEn: null })`; no agrega campos ni índices. Los documentos revocados se conservan hasta `venceEn` (el TTL los limpia).

**Redis (opcional):** clave `revocado:{jti}` con valor vacío y TTL = `exp` − ahora del access token; sin persistencia (riesgo aceptado de §12). No se guarda ningún dato personal. La validación del JWT consulta la clave solo si Redis está configurado; si Redis falla en ese momento, se aplica la regla de la duda 3.

**Flujo:** validar el cuerpo → calcular la huella → buscar por huella → si existe, revocar su familia (y, si hay `Authorization` válido de la misma persona y Redis, guardar el `jti`) → responder 204. Si no existe, 204 sin escribir. Un access token de una persona distinta a la dueña del refresh no revoca nada (caso 07).

Migración / semillas: ninguna.

## 6. Seguridad, privacidad y amenazas
STRIDE sobre un endpoint público que invalida una sesión (`security-core`, checks 2, 3, 6, 11, 13 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Suplantación (S):** usar un refresh tras cerrar sesión | La familia queda revocada de forma permanente; `/auth/refresh` responde 401 | TC-HU-004-02 |
| **Suplantación (S):** cerrar la sesión de otra persona | Solo quien posee el refresh (256 bits, inadivinable) puede revocar su familia; un access token ajeno no revoca nada | TC-HU-004-07 |
| **Alteración (T):** repetir o falsear el cierre | Idempotente; solo se escriben `revocadoEn` y `motivoRevocacion` de la familia indicada | TC-HU-004-03 |
| **Repudio (R):** sin rastro del cierre | `motivoRevocacion: "logout"` y `revocadoEn` en cada documento; evento de registro estructurado sin datos personales | TC-HU-004-01 · TC-HU-004-12 |
| **Fuga (I):** enumeración de tokens válidos | 204 igual exista o no el refresh; mismo tiempo y cuerpo | TC-HU-004-04 |
| **Fuga (I):** tokens, `jti` o `sub` en logs | Logs estructurados sin esos valores; el refresh solo viaja en el cuerpo de la petición | TC-HU-004-12 |
| **Fuga (I):** respuestas en caché | `Cache-Control: no-store`; sin compresión de `/auth/*` | TC-HU-004-10 |
| **Denegación (D):** ráfagas contra el logout | Límite por IP de `/auth/*` antes de autenticar (429) | TC-HU-004-11 |
| **Elevación (E):** access token cerrado que sigue sirviendo | Con Redis, `revocado:{jti}` lo invalida de inmediato; sin Redis vale hasta 15 min (riesgo aceptado, §12) y se afirma en la prueba | TC-HU-004-08 · TC-HU-004-09 |
| **Denegación (D):** Redis caído o reiniciado pierde los revocados | Riesgo aceptado (§12): la revocación del refresh es permanente en MongoDB y no se pierde | TC-HU-004-09 |

Datos personales tocados: ninguno nuevo.

## 7. Pantallas
— (backend). La confirmación con el texto de la maqueta, la limpieza del almacenamiento seguro y de la caché y la cola de revocación sin conexión son de knock-frontend (caso 14).

## 8. Pruebas, gates y límites
**Plan de pruebas:** Unitaria (validador), Integración (logout real con `documentdb-local` y, para el caso 08, un contenedor de Redis con Testcontainers), Seguridad negativa (enumeración, dueño distinto, fugas), Contrato. No aplican mutación (lógica mínima), rendimiento, usabilidad ni accesibilidad en este repositorio. La suite de arquitectura y el análisis de seguridad del CI corren siempre.
**Gates:** `AGENTS.md §5`: build, formato, análisis estático y pruebas en verde; `sdd_lint.py --trace HU-004`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK; `gitleaks` limpio; `Memoria.md` al día; «Uso de IA» completo en el PR.
**Límites y dudas abiertas** (recomendación entre paréntesis; las decide el líder):
1. **¿Redis en esta versión?** El principio 13 de la constitución pide la lista de revocados en Redis, pero sumar el cliente de Redis agrega un paquete sin evaluar (licencia, mantenimiento, vulnerabilidades). (Recomendación: la parte de Redis queda como **opcional por configuración** y se implementa solo si el líder aprueba el paquete; el logout funciona sin ella y el riesgo de 15 min queda escrito.)
2. **Autenticación del logout.** (Recomendación: público con el refresh en el cuerpo, porque la persona puede cerrar sesión con el access token vencido y porque el reintento sin conexión no tiene access token; el access token es opcional y solo sirve para revocar su `jti`.)
3. **Redis caído al validar un JWT.** (Recomendación: fallar abierto —aceptar el token— y registrar la falla, para no tumbar toda la API por la lista de revocados; el riesgo ya está aceptado en §12.)
4. **Auditoría del cierre en la base.** (Recomendación: no en esta versión; solo un evento de registro estructurado sin datos personales. Se reevalúa con la retención de `auditoria`, decisión D11 de HU-001.)
5. **Cerrar solo este dispositivo.** (Recomendación: sí; «cerrar todas las sesiones» es de HU-007.)
6. **Dependencia.** HU-001 no está en `sprint-02` (el PR #75 se cerró sin fusionar) y HU-003 está en Borrador. (Hay que fusionar HU-001 y HU-003 antes de escribir código de esta historia.)

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
