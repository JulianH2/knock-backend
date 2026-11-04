# HU-002 — Completar mi perfil la primera vez (nombre y foto opcional): versión mínima con `PUT /me`

> **Jira:** KNOCK-44 · **Épica:** EP-01 Acceso y cuenta · **Servicio:** Identidad y confianza (`Knock.Identity.Api`, ADR-001; hoy módulo Identity en `src/Knock.Api/`) · **Responsable:** Erick Trejo Reséndiz (como en la ficha) · **Sprint:** demo de Google Play y evaluación de la segunda unidad (la ficha dice S1) · **Puntos:** 2 (la ficha; esta versión mínima cubre solo el nombre) · **Capas:** BD y Back (este repositorio) y Mobile (knock-frontend)
> **Repositorio:** knock-backend (capas [BD] y [Back]) · **Otra parte en:** knock-frontend (capa [Mobile]: pantalla del nombre y «¡Listo, ya entraste!») · **Ficha:** `docs/backlog/HU-002.md` (generada; no se edita a mano)
> **Depende de:** **HU-001 (KNOCK-43)**: crea `usuarios` (con `perfilCompleto` en `false`), el JWT y `GET /me`. **Su código está en la rama `feature/KNOCK-43-phone-login-code`; el PR #75 se cerró sin fusionar y debe reabrirse y fusionarse antes de implementar** · TT-API-03 (ProblemDetails, 422, límite) · TT-BD-06 (cliente de Mongo)
> **Fuentes:** `docs/backlog/HU-002.md` (CA1 a CA7) · spec HU-001 (§4 `GET /me`, §5 `usuarios`, decisión sobre `perfilCompleto`) · constitución 2.2.x (principios 3, 10, 11, 15; §5; §6) · ADR-002 (regla 1) · skills `security-core` y `api-contract-openapi`
> **Skills:** story-workflow, spec-first-writing, auth-firebase-jwt, database-scripts, vertical-slice-api, security-core, api-contract-openapi, testing-strategy-backend, reuse-before-create
> **Estado:** Aprobada

## 1. Objetivo
Que quien acaba de crear su cuenta pueda **poner su nombre** y quedar con el perfil completo, para que el resto de la app sepa con quién trata. Esta spec es la **versión mínima para el demo**: solo el nombre. La **foto** (CA3 a CA5 de la ficha) y la **zona** (HU-009) quedan fuera y se agregan después sin romper el contrato. Este repositorio entrega `PUT /api/v1/me`, el campo `nombre` en `usuarios` y la ampliación de `GET /api/v1/me`; la pantalla es de knock-frontend.

**Terminada cuando** (ficha, CA1, CA2, CA6 y CA7): un nombre de 2 a 60 letras se guarda y `perfilCompleto` pasa a `true`; uno inválido responde 422 sin guardar nada; al volver a entrar no se pide de nuevo; y ninguna persona puede cambiar el perfil de otra (la identidad sale solo del `sub` del JWT).

**Qué ya existe y qué es nuevo** (verificado en la rama de HU-001):

| Pieza | En la rama de HU-001 | En esta historia |
|---|---|---|
| `GET /me` | Devuelve `{ id }` con el `sub` del JWT | **Se amplía** con `nombre` (puede ser `null`) y `perfilCompleto` |
| `usuarios` | `perfilCompleto` nace `false`; HU-001 dice que lo cambia HU-002 | **Se agrega** el campo `nombre`; `perfilCompleto` pasa a `true` al guardar un nombre válido |
| Almacén | `IIdentityStore` busca y crea cuentas | **Se extiende** con la actualización del nombre por `sub` |
| Ruta | La ficha dice `PUT /api/v1/me/perfil` | **`PUT /api/v1/me`** (recurso propio, junto a `GET /me`; duda 1) |

## 2. Historia y casos de prueba
**HU-002** — Historia de usuario (capas BD, Back y Mobile). Nombre de pruebas: `Metodo_Escenario_Resultado_TC_HU_002_KK`.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-HU-002-01 | Dada una cuenta nueva con sesión, cuando se envía `PUT /me` con `{ "nombre": "José Pérez" }`, entonces responde 200 `{ id, nombre: "José Pérez", perfilCompleto: true }` y el nombre queda en `usuarios`; `GET /me` devuelve los mismos valores. | `IntegrationTest/Identity/ProfileTests.ValidName_SavesAndCompletesProfile_TC_HU_002_01` |
| TC-HU-002-02 | Dado un nombre con espacios sobrantes (`"  José   Pérez "`), cuando se guarda, entonces se normaliza a NFC y con un solo espacio entre palabras (`"José Pérez"`). | `UnitTest/Identity/PersonNameTests.NormalizesWhitespaceAndForm_TC_HU_002_02` |
| TC-HU-002-03 | Dado un nombre inválido (menos de 2 o más de 60 caracteres, vacío o solo espacios, con números o símbolos, con caracteres de control o invisibles), cuando se envía, entonces responde 422 con `errors.nombre` y **no** guarda nada ni cambia `perfilCompleto`; el valor enviado no se repite. | `UnitTest/Identity/ProfileRequestValidatorTests.InvalidNames_ProduceFieldError_TC_HU_002_03` |
| TC-HU-002-04 | Dados nombres reales con acento, `ñ`, guion o apóstrofo (`"María-José"`, `"D'Angelo"`, `"Núñez"`), cuando se envían, entonces se aceptan (duda 2). | `UnitTest/Identity/ProfileRequestValidatorTests.RealWorldNamesAreAccepted_TC_HU_002_04` |
| TC-HU-002-05 | Dado el mismo `PUT /me` enviado dos veces, cuando se responde, entonces ambas son 200 con el mismo cuerpo y `version` solo sube si el contenido cambió. | `IntegrationTest/Identity/ProfileTests.SamePutTwice_IsIdempotent_TC_HU_002_05` |
| TC-HU-002-06 | Dado un cuerpo con campos extra (`id`, `sub`, `estado`, `perfilCompleto: false`), cuando se envía, entonces se ignoran: el `sub`, el `estado` y el resto salen solo del JWT y del servidor. | `SecurityTest/Identity/ProfileTests.ExtraFieldsAreIgnored_TC_HU_002_06` |
| TC-HU-002-07 | Dada una petición sin `Authorization`, con un token alterado o vencido, cuando se envía `PUT /me` o `GET /me`, entonces responde 401 uniforme y no toca la base. | `SecurityTest/Identity/ProfileTests.Unauthenticated_Returns401_TC_HU_002_07` |
| TC-HU-002-08 | Dada una cuenta `suspendido` con un access token aún vigente, cuando se envía `PUT /me`, entonces responde 403 con `code: "account_suspended"` y no cambia nada. | `IntegrationTest/Identity/ProfileTests.SuspendedAccount_Returns403_TC_HU_002_08` |
| TC-HU-002-09 | Dado el diseño del endpoint, entonces no recibe id de cuenta (ni en la ruta, ni en el cuerpo, ni en la consulta): dos personas con sesión solo pueden cambiar su propio nombre (caso 7 de la ficha: no existe forma de apuntar a otra cuenta). | `SecurityTest/Identity/ProfileTests.NoWayToTargetAnotherAccount_TC_HU_002_09` |
| TC-HU-002-10 | Dada una cuenta sin nombre, cuando se llama `GET /me`, entonces devuelve `{ id, nombre: null, perfilCompleto: false }`; con nombre, `perfilCompleto: true`. Toda respuesta lleva `Cache-Control: no-store`. | `IntegrationTest/Identity/ProfileTests.GetMeShowsProfileState_TC_HU_002_10` |
| TC-HU-002-11 | Dado un cuerpo de más de 2 KiB o JSON ilegible, cuando se envía, entonces responde 413 o 400 respectivamente, con ProblemDetails con `traceId` y `correlationId`. | `IntegrationTest/Identity/ProfileTests.OversizedOrUnreadableBody_413Or400_TC_HU_002_11` |
| TC-HU-002-12 | Dadas las operaciones de los casos 01 a 11, cuando se captura la salida de logs, entonces no aparece el nombre, el `sub` ni ningún token. | `SecurityTest/Identity/ProfileLeakTests.NoPersonalDataInLogs_TC_HU_002_12` |
| TC-HU-002-13 | Dado un `PUT /me`, cuando se cuenta el límite, entonces queda bajo el límite global por IP de TT-API-03 (100 por minuto) y responde 429 con `Retry-After` al excederlo. | `IntegrationTest/Identity/ProfileTests.GlobalRateLimitApplies_TC_HU_002_13` |
| TC-HU-002-14 | Dado el contrato, cuando se genera el OpenAPI, entonces `PUT /api/v{version}/me` declara 200, 400, 401, 403, 413, 422 y 429, y `GET /me` declara los campos nuevos como opcionales (cambio compatible). | `ContractTest/Identity/MeContractTests.PutMeAndGetMeAreDeclared_TC_HU_002_14` |
| TC-HU-002-15 | Dada la app, cuando una cuenta nueva escribe un nombre válido y toca «Continuar», entonces se guarda y se muestra «¡Listo, ya entraste!»; con un nombre inválido ve «Escribe tu nombre (solo letras)» y no avanza; al volver a entrar no se le pide de nuevo. | (otro repo) knock-frontend: pruebas de widget y unitarias |
| TC-HU-002-16 | Dado el trabajo terminado, cuando se compila en Release con análisis estricto y se verifica el formato, entonces hay 0 errores y 0 warnings. | (comando) `dotnet build Knock.slnx -c Release --no-restore -warnaserror` y `dotnet format Knock.slnx --verify-no-changes`; salida en el PR |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `MeController`: acción `PUT /me` (con `[Authorize]`) y ampliación de `GET /me`.
  - Validador del nombre (FluentValidation), normalización y `IIdentityStore.UpdateNameAsync(sub, nombre)`.
  - Campo `nombre` en `usuarios`; `perfilCompleto` pasa a `true` al guardar un nombre válido; `version` se incrementa si el contenido cambia.
  - Pruebas de los casos 01 a 14 y 16; entrada en `Memoria.md`.
- **Out:**
  - **Foto de perfil** (CA3 a CA5 de la ficha), que usa TT-API-05 (archivos): una historia aparte cuando haga falta.
  - **Zona** (`PUT /me/zona`, HU-009) y **editar después** desde Mis datos (HU-049).
  - Pantallas (knock-frontend). Continuar donde me quedé sin cuenta (HU-005).
  - `docs/openapi.v1.json` (protegido; lo regenera TT-CI-02 o el líder).

## 4. Contratos API y eventos
Errores en ProblemDetails (RFC 9457) con `traceId` y `correlationId` (TT-API-03).

**`PUT /api/v1/me`** — autenticado (`[Authorize]`; el `sub` sale del JWT), cuerpo máximo 2 KiB.

| Elemento | Detalle |
|---|---|
| Cuerpo | `{ nombre: string (2 a 60 caracteres: letras con acentos, espacios, guion y apóstrofo) }` |
| `200 OK` | `{ id, nombre, perfilCompleto: true }` (`id` es el `sub`). |
| `401` | Sin token, alterado o vencido (cuerpo uniforme). |
| `403` | Cuenta suspendida. ProblemDetails con `code: "account_suspended"`. |
| `413` / `400` | Cuerpo mayor de 2 KiB / JSON ilegible. |
| `422` | `nombre` inválido (`errors.nombre`, sin repetir el valor). |
| `429` | Límite global por IP (100 por minuto, TT-API-03) con `Retry-After`. |
| Encabezados | `Cache-Control: no-store`; `X-Correlation-Id`. |

**`GET /api/v1/me`** (ya existe) pasa a devolver `{ id, nombre: string | null, perfilCompleto: boolean }`: los campos nuevos son aditivos (compatibles con `oasdiff`).

**Excepciones (documentadas en el OpenAPI y en `docs/endpoints.md`):** principio 15 (`Idempotency-Key`): un `PUT` con el mismo cuerpo es idempotente por naturaleza. `If-Match`/`ETag` (§5): no se exige en el demo; gana la última escritura (duda 4). Principio 11 (paginación): no aplica. **Eventos:** ninguno.

## 5. Modelo de datos
Base lógica **`identity`**. **`usuarios`** (definida en HU-001) — `cache: no` (datos personales; principio 10). Se agrega:
```
nombre: string | null   (2 a 60 caracteres; NFC; sin espacios de más)
```
- `nombre` nace en `null`. Al guardar un nombre válido: `nombre` = valor normalizado, `perfilCompleto` = `true` y `version` + 1 **solo si el contenido cambió**.
- Actualización: `findOneAndUpdate({ sub, estado: "activo" }, …)` con filtro **solo por `sub`** (nunca por un dato del cliente). Si no hay documento activo: se lee el estado para responder 403 (`suspendido`) o 401.
- El nombre se guarda **en claro** (se mostrará a otras personas en el perfil) y **nunca** se escribe en logs. No hay índices nuevos.
- Migración / semillas: ninguna (`nombre` ausente se lee como `null`).

## 6. Seguridad, privacidad y amenazas
STRIDE sobre un endpoint autenticado que escribe un dato personal (`security-core`, checks 1, 3, 4, 6, 11 y 15).

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| **Suplantación (S):** cambiar el perfil de otra cuenta | La identidad sale solo del `sub` del JWT; el endpoint no recibe ningún id | TC-HU-002-06 · TC-HU-002-09 |
| **Suplantación (S):** usar el endpoint sin sesión o con un token alterado | `[Authorize]` con validación del JWT (HS256, 15 min, `ClockSkew` 0); 401 uniforme | TC-HU-002-07 |
| **Alteración (T):** inyección o caracteres peligrosos en el nombre | FluentValidation estricta (letras, espacios, guion y apóstrofo; sin control ni invisibles), normalización NFC, filtros tipados del driver, nunca JSON armado con la entrada; 422 sin repetir el valor | TC-HU-002-03 · TC-HU-002-04 |
| **Alteración (T):** el cliente elige `estado` o `perfilCompleto` | Se ignoran los campos extra; solo se escribe `nombre`, `perfilCompleto` y `version` | TC-HU-002-06 |
| **Repudio (R):** sin rastro de quién cambió qué | `CorrelationId` en logs estructurados sin el nombre; `version` en el documento | TC-HU-002-12 |
| **Fuga (I):** nombre o `sub` en logs o errores | Logs sin esos valores; ProblemDetails sin el valor enviado | TC-HU-002-12 |
| **Fuga (I):** perfil en caché HTTP o intermediarios | `Cache-Control: no-store` | TC-HU-002-10 |
| **Denegación (D):** ráfagas de escrituras | Límite global por IP de TT-API-03 (429) y cuerpo máximo de 2 KiB | TC-HU-002-11 · TC-HU-002-13 |
| **Elevación (E):** cuenta suspendida que sigue editando con un access token vigente | Se exige `estado: "activo"` en la escritura; 403 | TC-HU-002-08 |

Datos personales tocados: **el nombre** (dato personal visible a otras personas). Declarado en el aviso de privacidad de TT-WEB-02; no es dato sensible (no se cifra a nivel de campo).

## 7. Pantallas
— (backend). La pantalla del nombre, el mensaje «Escribe tu nombre (solo letras)» y «¡Listo, ya entraste!» son de knock-frontend (caso 15). En esta versión no hay pantalla de foto ni de zona.

## 8. Pruebas, gates y límites
**Plan de pruebas:** Unitaria (validador y normalización del nombre), Integración (guardado real con `documentdb-local` por Testcontainers), Seguridad negativa (sin sesión, suspendida, campos extra, fugas), Contrato y Mutación del validador (meta 70 %). No aplican rendimiento, usabilidad ni accesibilidad en este repositorio. La suite de arquitectura y el análisis de seguridad del CI corren siempre.
**Gates:** `AGENTS.md §5`: build, formato, análisis estático y pruebas en verde; `sdd_lint.py --trace HU-002`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK; `gitleaks` limpio; `Memoria.md` al día; «Uso de IA» completo en el PR.
**Límites y dudas abiertas** (recomendación entre paréntesis; las decide el líder):
1. **Ruta.** La ficha dice `PUT /api/v1/me/perfil` y HU-001 dice `PUT /me/zona`. (Recomendación: `PUT /api/v1/me` para el nombre, junto a `GET /me`; la zona sigue siendo `/me/zona` en HU-009. La ficha es generada: no se edita a mano; el contrato aprobado es esta spec.)
2. **Caracteres del nombre.** La ficha dice «solo letras y espacios». (Recomendación: aceptar también guion y apóstrofo (`María-José`, `D'Angelo`), porque rechazarlos rechazaría nombres reales; siguen prohibidos números, símbolos y caracteres de control o invisibles.)
3. **Qué es «perfil completo».** (Recomendación: en esta versión `perfilCompleto` = tiene nombre. Cuando se agreguen zona y foto se redefine en la spec que las agregue, sin cambiar el contrato.)
4. **Concurrencia.** (Recomendación: sin `If-Match` en el demo; gana la última escritura. `version` queda para cuando haga falta.)
5. **Nombre en claro.** (Recomendación: sí; no es dato sensible según la constitución y se muestra a otras personas; nunca va a logs.)
6. **Dependencia.** HU-001 no está en `sprint-02` (el PR #75 se cerró sin fusionar). (Hay que fusionarlo antes de escribir código de esta historia.)

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-03
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
