# HU-009 — Indicar mi zona con mi ubicación o código postal

> **Jira:** KNOCK-47 · **Épica:** EP-02 · **Servicio:** Mercado (`Knock.Market.Api`, ADR-001) · **Módulo:** Catalog · **Sprint:** no en este sprint; se implementa después de la migración del catálogo (TT-BD-06), ver `plan.md` · **Puntos:** 3
> **Repositorio:** knock-backend (capa [Back]); la capa Mobile (Flutter) vive en knock-frontend
> **Skills:** story-workflow, spec-first-writing, vertical-slice-api, security-core, api-contract-openapi, testing-strategy-backend
> **Estado:** Borrador

**Cómo leer esta spec.** Esta historia **define** el listado de colonias por CP y la lista de espera, y **consume** la colonia por punto de TT-API-07 (KNOCK-141). La zona del perfil (`PUT /me/zona`) es de Identidad y queda fuera, como dependencia de HU-001/HU-002. La base y la consulta quedan cerradas con la evidencia del spike TT-BD-05 (KNOCK-147, `docs/evidencia/KNOCK-147-spike-documentdb.md`, pendiente de que se fusione #48).

## 1. Objetivo
**Como** persona que se registra, **quiero** indicar mi zona con mi ubicación o mi código postal, **para** ver expertos cerca sin dar mi dirección.

El paso se pide después del nombre (`docs/referencia/FUNCIONES_Y_FLUJOS.md §3.3`). Solo se guardan la **colonia** y el **código postal**; las coordenadas se convierten en colonia y se descartan. La colonia se calcula con el catálogo propio SEPOMEX del servicio Mercado, sin geocodificación externa (`DECISIONES.md #22`). Fuera de cobertura se ofrece lista de espera (`DECISIONES.md #12`). La ciudad piloto es Tula de Allende, Hidalgo (decisión del equipo del 2026-10-13, registrada en TT-BD-02); los textos de `DECISIONES.md` aún dicen Pachuca y se actualizan en un PR separado.

**Antecedente:** el borrador anterior resolvía la colonia con PostgreSQL + PostGIS y EF Core (TT-BD-02). ADR-002 reemplaza esa base por Azure DocumentDB (API de MongoDB), confirmada por el spike TT-BD-05; TT-BD-06 rehace el catálogo. Nada de esta spec depende ya de PostGIS ni de EF Core.

## 2. Historia y casos de prueba
**HU-009** — Historia de usuario. Todos los puntos y CP de las pruebas son sintéticos o del extracto SEPOMEX versionado; nunca ubicaciones de personas reales. Las pruebas unitarias usan un doble del catálogo de colonias; las de integración, Testcontainers con `documentdb-local`.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-HU-009-01 | **CA1.** Dado que la persona concede el permiso de ubicación y su punto está en la ciudad piloto, cuando la app consulta la zona, entonces se propone la colonia de ese punto y la persona puede confirmarla o cambiarla. | `UnitTest/Catalog/ZoneSuggestionTests.ProposesNeighborhoodFromPointLookupResult_TC_HU_009_01`: el servicio de zona propone la colonia con el resultado de `GET /api/v1/colonias/en-punto` (doble de ese resultado; sin coordenadas en la propuesta). La consulta en sí, la redacción de coordenadas en logs y sus pruebas son de **TT-API-07 (KNOCK-141)**; HU-009 no define una ruta propia. La pantalla vive en knock-frontend. |
| TC-HU-009-02 | **CA2.** Dado que la persona niega el permiso de ubicación o la ubicación falla, cuando continúa, entonces la app le pide su código postal. | (otro repo) Lógica de pantalla en knock-frontend. El soporte backend es `GET /api/v1/zonas/cp/{cp}`, verificado en TC-HU-009-03. |
| TC-HU-009-03 | **CA3.** Dado un CP de la ciudad piloto presente en el catálogo, cuando la persona lo escribe, entonces se listan sus colonias para elegir una; y dado un CP mal formado (no numérico, de 4 o de 6 dígitos, vacío), cuando se consulta, entonces responde `422` `application/problem+json` con la lista de campos (`errors.cp`) sin repetir el valor enviado (convención de TT-API-03) y el catálogo no se consulta. | `UnitTest/Catalog/ZoneServiceTests.PilotPostalCodeListsItsNeighborhoods_TC_HU_009_03` (doble con colonias de `db/sepomex/tula_colonias.csv`; solo colonias de ese CP), `UnitTest/Catalog/PostalCodeQueryValidatorTests.MalformedPostalCodeFailsOnCpField_TC_HU_009_03`, `ContractTest/Catalog/ZoneContractTests.ZoneResponsesIncluding422MatchOpenApi_TC_HU_009_03` (`200` y `422` contra el OpenAPI) e `IntegrationTest/Catalog/ZoneByPostalCodeTests.PilotPostalCodeListsItsNeighborhoodsAndMalformedReturns422_TC_HU_009_03` (Testcontainers `documentdb-local`). |
| TC-HU-009-04 | **CA4.** Dado un CP fuera de cobertura, cuando se consulta, entonces la respuesta indica «fuera de cobertura» con el nombre de la ciudad piloto para que la app muestre «Por ahora solo atendemos [ciudad piloto]» y el botón «Avísame cuando lleguen». Dado un CP inválido en la ruta o en el cuerpo de la lista de espera (incluidos intentos de inyección como `{"$ne":1}`, `.*` o `42800%00`), entonces responde `422` con `errors.cp`, no `400`. (La ubicación fuera de cobertura la responde `GET /colonias/en-punto`, TT-API-07.) | `UnitTest/Catalog/ZoneServiceTests.OutOfCoveragePostalCodeReturnsPilotCity_TC_HU_009_04` (CP válido sin colonias en cobertura), `SecurityTest/Catalog/ZoneInputValidationTests.InvalidPostalCodeReturns422WithFieldErrors_TC_HU_009_04` e integración con `documentdb-local`. |
| TC-HU-009-05 | **CA5.** Dada una consulta fuera de cobertura, cuando la persona confirma la lista de espera, entonces se guarda un registro con su CP (y municipio si se conoce) y la respuesta permite a la app agradecerle. | `UnitTest/Catalog/WaitlistServiceTests.JoiningWaitlistIsIdempotentPerUserAndPostalCode_TC_HU_009_05` (`201` la primera vez, `200` al repetir sin `Idempotency-Key`; el usuario sale del JWT; sin coordenadas) e `IntegrationTest/Catalog/WaitlistTests.JoiningWaitlistIsIdempotentPerUserAndPostalCode_TC_HU_009_05` (colección `listas_espera` con índice único, Testcontainers `documentdb-local`). |
| TC-HU-009-06 | Dado un `municipio` en la lista de espera de más de 100 caracteres, vacío o solo con espacios, o con caracteres fuera de los permitidos (dígitos, `<`, `>`, `$`, `{`, `}`, `;`, comillas dobles, caracteres de control o CR/LF), cuando se envía, entonces responde `422` con `errors.municipio` sin repetir el valor y no se guarda nada; y dado `Tula de Allende`, `San Agustín Tlaxiaca` o `Ixmiquilpan` (letras con acentos o ñ, espacios, punto, guion y apóstrofo, de 1 a 100 caracteres), entonces se acepta. Sin `municipio`, la petición es válida. | `UnitTest/Catalog/WaitlistRequestValidatorTests.MunicipalityLengthAndCharactersAreValidated_TC_HU_009_06` (Theory) · `SecurityTest/Catalog/ZoneInputValidationTests.InvalidMunicipalityReturns422WithoutStoring_TC_HU_009_06` |

**Criterios diferidos (dependencia, fuera de esta historia):** CA6 («Ahora no» asigna la colonia por defecto **Centro**, que se puede cambiar después) y CA7 (en el perfil solo quedan colonia y CP, nunca coordenadas) se agregan como casos 07 y 08 cuando exista PUT /me/zona (HU-001/HU-002). Ver §3 y §4.

## 3. Alcance / No-alcance
- **In (este repositorio, capa [Back], servicio Mercado):**
  - Buscar por CP: listar las colonias en cobertura de un CP (`GET /zonas/cp/{cp}`).
  - Lista de espera fuera de cobertura: guardar CP (y municipio si se conoce) de quien la pida (`POST /zonas/lista-espera`).
- **Consume (no define):** `GET /api/v1/colonias/en-punto` de TT-API-07 (KNOCK-141), única ruta de colonia por punto.
- **In (knock-frontend, solo referencia):** pantalla «¿Dónde buscas expertos?», permiso de ubicación, fallback a CP, selección y confirmación de colonia, aviso de cobertura y agradecimiento. Se especifica e implementa en knock-frontend.
- **Out:**
  - Colonia por punto (`/colonias/en-punto`): es de TT-API-07.
  - Zona del perfil (`PUT /me/zona`), que expone Identidad. CA6 y CA7: se agregan como casos 07 y 08 cuando exista PUT /me/zona (HU-001/HU-002).
  - Dirección exacta del servicio (HU-010).
  - Cambiar la zona después del registro (HU-049).
  - Geocodificación externa (Google, Mapbox, HERE, Nominatim).
  - Avisar a la lista de espera cuando se abra cobertura.
- **Reglas (firmes):**
  - Nunca guardar coordenadas, ni en la base ni en logs, trazas o mensajes de error.
  - Un CP sin colonia en cobertura está fuera de cobertura (ver §5 y pregunta d).
- **Dependencias:** TT-API-07 (KNOCK-141) para la colonia por punto; TT-BD-06 (catálogo en Azure DocumentDB); HU-001/HU-002 para el usuario y para `PUT /me/zona` en Identidad; spike TT-BD-05 (#48, pendiente de fusionar); TT-API-03 (KNOCK-37, en `sprint-01`) para el 422 con FluentValidation y el límite global por IP. **HU-001** trae `tests/ContractTest`, `docs/openapi.v1.json` y `docs/endpoints.md`, que hoy no existen: sin ellos no se puede cerrar la prueba de contrato de TC-HU-009-03.

## 4. Contratos API y eventos
**Firme.** Se fija en OpenAPI al implementar. Todos bajo `[ApiVersion("1.0")]`, autenticados con JWT, con errores en `ProblemDetails` y límite de peticiones según `docs/rate-limit-matrix.md`. Desde fuera se llega por el gateway (ADR-001).

| Método y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /api/v1/zonas/cp/{cp}` | `cp` = exactamente 5 dígitos (`^\d{5}$`) | `200` `{ enCobertura: true, colonias: [{ id, nombre, cp }] }` · `200` `{ enCobertura: false, ciudadPiloto }` · `422` formato inválido (`errors.cp`) · `401` · `429` |
| `POST /api/v1/zonas/lista-espera` | `{ cp, municipio? }` (`municipio`: 1 a 100 caracteres; letras, incluidas las acentuadas y la ñ, espacios, punto, guion y apóstrofo) | `201` · `200` al repetir con el mismo usuario y CP (idempotencia natural, sin duplicar) · `422` CP o municipio inválido (`errors.cp`, `errors.municipio`) · `401` · `429` |

- **Validación (convención de TT-API-03):** errores de entrada → `422` ProblemDetails con `errors` (campo en camelCase → motivos), sin repetir el valor. El `ValidationFilter` de TT-API-03 solo valida argumentos que tengan un `IValidator<T>` registrado y no valida un `string` suelto: el `cp` del `GET` se enlaza a un objeto de consulta (p. ej. `[FromRoute]` sobre un DTO con `Cp`) con su `IValidator` registrado; el cuerpo del `POST` tiene el suyo.
- **Paginación (principio 11) — excepción documentada (decisión (e) del líder):** `GET /zonas/cp/{cp}` devuelve un arreglo sin cursor. Motivo: el extracto SEPOMEX versionado tiene 12 colonias en total, así que el conjunto por CP está acotado.
- **Idempotencia (principio 15) — excepción documentada (decisión (f) del líder):** `POST /zonas/lista-espera` no exige `Idempotency-Key`. Es idempotente de forma natural por el índice único `(usuarioId, cp)`: la primera vez `201`; al repetir, `200` con el registro existente, sin modificarlo ni duplicarlo.
- **Consumido, no definido aquí:** `GET /api/v1/colonias/en-punto` (TT-API-07, KNOCK-141). Su entrada (`lat`, `lon`), su respuesta (incluido «fuera de cobertura») y la redacción de `lat`/`lon` en logs se fijan en esa tarea.
- **Dependencia, fuera de esta historia:** `PUT /api/v1/me/zona` lo expone **Identidad**; guarda solo `coloniaId` y `cp` y valida la colonia contra Mercado por contrato síncrono (`Knock.Contracts`; mientras sea un solo proyecto, `ICatalogApi`). Mercado no escribe en la base de Identidad (ADR-001 regla 1).
- Controllers, DTOs, validadores y servicios son `internal` al servicio. Lo que otro servicio necesite se expone por contrato (`ICatalogApi` hoy; `Knock.Contracts` al separar servicios, ADR-001 regla 2).
- **Eventos:** ninguno en esta historia.
- Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y regenerar `docs/openapi.v1.json` con `scripts/export_openapi.py`.

## 5. Modelo de datos
Base: **Azure DocumentDB** (API compatible con MongoDB, ADR-002), según la evidencia del spike TT-BD-05 (`docs/evidencia/KNOCK-147-spike-documentdb.md`, pendiente de que se fusione #48). Colecciones en lugar de tablas; índices y semillas versionados como scripts (ADR-002 regla 2). TT-BD-06 crea el catálogo.

- **Colección `colonias`** (de TT-BD-06) — `cache: 86400` (catálogo, principio 10; por CP, como declara TT-BD-06 §5): polígono y centro con índice `2dsphere`; la consulta por punto usa `$geoIntersects` (la implementa TT-BD-06 y la expone TT-API-07). Spike P3: consulta en servidor p95 **0.6 ms** (cumple < 100 ms). Esta historia la consulta por `cp`, con el índice `{ cp: 1 }` de TT-BD-06.
- **Colección `listas_espera`** — `cache: no` (datos personales: liga a un usuario con su CP, principio 10): `{ _id: ObjectId, usuarioId, cp: string(5), municipio?: string(≤100), creadoEn: fecha UTC }` con índice único `(usuarioId, cp)`; sin coordenadas. Nombre en `snake_case` plural y campos en camelCase (constitución §5).
- **Conexión:** `maxPoolSize` fijo por servicio (spike P7: el límite de conexiones del nivel gratuito no es público).
- **Invariantes:** ningún documento guarda coordenadas de la persona; la lista de espera es única por usuario y CP; el usuario sale del JWT.
- **Perfil:** fuera de esta historia; Identidad guardará solo `coloniaId` + `cp`.

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Coordenadas exactas guardadas o registradas revelan dónde vive la persona. | HU-009 no recibe coordenadas; la colonia por punto es de TT-API-07, que las redacta en logs y no las persiste. `listas_espera` no tiene campos de ubicación. | TC-HU-009-05 (y pruebas de TT-API-07) |
| CP malformado o inyección en la ruta o en el filtro. | Validar `^\d{5}$` con FluentValidation (422 con `errors.cp`); filtros con parámetros tipados, nunca concatenando la entrada. | TC-HU-009-03 · TC-HU-009-04 |
| `municipio` con texto largo, marcado o caracteres de control (inyección en el panel o en logs, relleno de la base). | Máximo 100 caracteres y lista de caracteres permitidos; 422 sin repetir el valor. | TC-HU-009-06 |
| Un usuario guarda la lista de espera de otro (BOLA). | El usuario sale del JWT, nunca del cuerpo. | TC-HU-009-05 |
| Enumeración masiva del catálogo o llenado de la lista de espera. | Límite global por IP (TT-API-03); el límite por usuario lo hará el gateway (TT-ARQ-02). Lista de espera única por usuario y CP. | TC-HU-009-05 |
| Agotar las conexiones del nivel gratuito de DocumentDB. | `maxPoolSize` fijo por servicio. | Revisión de `security-reviewer` |

## 7. Pantallas
Pantalla «¿Dónde buscas expertos?» del registro (después del nombre): «Usar mi ubicación», «Escribir código postal», «Ahora no»; confirmación/cambio de colonia; aviso «Por ahora solo atendemos [ciudad piloto]» con «Avísame cuando lleguen» y agradecimiento. Se implementan en knock-frontend; este repositorio solo provee los endpoints de §4 (y `/colonias/en-punto` de TT-API-07).

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Propuesta de colonia con el resultado de `/colonias/en-punto` (TC-HU-009-01, doble de ese resultado); regla de cobertura por CP (colonias en cobertura → `enCobertura: true`; sin colonias → fuera con ciudad piloto), validadores de CP y de `municipio`, idempotencia de la lista de espera. Doble del catálogo. `tests/UnitTest`. |
| Seguridad negativa | Sí | CP no numérico, de 4 o 6 dígitos o con intento de inyección → `422` con `errors.cp`; `municipio` inválido → `422` con `errors.municipio`; sin JWT → `401`; usuario del JWT, no del cuerpo. `tests/SecurityTest`. |
| Contrato | Sí, cuando exista la base | Respuestas `200` y `422` de `/api/v1/zonas/*` contra `docs/openapi.v1.json` en `tests/ContractTest`; ambos llegan con HU-001 (dependencia de §3). |
| Integración | Sí | Testcontainers con `documentdb-local` (reproduce a Azure en el spike): TC-HU-009-03 a 05, incluido el índice único de `listas_espera` y que ningún documento guarda coordenadas. No tomar el local como límite de conexiones (spike P7). |
| Rendimiento | No en esta historia | La colonia por punto es de TT-API-07; el spike ya midió p95 0.6 ms en servidor con `2dsphere` + `$geoIntersects`. |
| Eventos | No | La historia no publica eventos. |
| Mutación | Según la meta vigente del módulo Catalog | Nightly con Stryker. |

**Gates:** `AGENTS.md §5`. Esta spec es un borrador: no se han escrito ni ejecutado pruebas.

**Decisiones (revisión del Tech Lead):**
- **(a) Ruta de colonia por punto — Resuelto.** Una sola ruta: `GET /api/v1/colonias/en-punto` (TT-API-07, KNOCK-141). HU-009 no define `/zonas/colonia`; consume `/colonias/en-punto`. HU-009 conserva el listado por CP, la lista de espera y la parte de zona del perfil descrita en (b).
- **(b) Zona del perfil — Resuelto.** Identidad expone `PUT /me/zona` y guarda solo `coloniaId` y `cp`; valida la colonia contra Mercado por contrato síncrono (`Knock.Contracts`; mientras sea un solo proyecto, `ICatalogApi`). Mercado no escribe en la base de Identidad. Esta historia se acota a los `GET` y a la lista de espera; `PUT /me/zona` espera al usuario de HU-001/HU-002 y queda fuera, como dependencia.
- **(c) Consulta «colonia de un punto» — Resuelto con la evidencia del spike (pendiente de que se fusione #48).** Responsable: Alexis. Azure DocumentDB; índice `2dsphere` y `$geoIntersects`; consulta en servidor p95 0.6 ms (cumple < 100 ms); pruebas de integración con Testcontainers `documentdb-local`. La implementa TT-BD-06 y la expone TT-API-07.
- **(d) Cobertura y colecciones — Resuelto con la evidencia del spike (pendiente de que se fusione #48).** Responsable: Alexis. Colecciones en lugar de tablas (`colonias` y `listas_espera`); un CP sin colonias en el catálogo está fuera de cobertura; `maxPoolSize` fijo por servicio.

**Decisiones del líder (2026-10-23, sobre la revisión de Guillermo en #40):**
- **(e) Paginación de `GET /zonas/cp/{cp}` (principio 11) — Resuelto.** Sin paginar, como excepción documentada al principio 11. Motivo: el extracto SEPOMEX versionado tiene 12 colonias en total, así que el conjunto por CP está acotado. Es la misma pregunta que hizo Erick en #31 sobre `GET /oficios`.
- **(f) `Idempotency-Key` en `POST /zonas/lista-espera` (principio 15) — Resuelto.** Idempotencia natural por el índice único `(usuarioId, cp)`, como excepción al principio 15, sin `Idempotency-Key`: la primera vez `201`; al repetir, `200` con el registro existente, sin modificarlo ni duplicarlo.
- **Caché (principio 10) — Resuelto.** `colonias` con `cache: 86400` (catálogo) y `listas_espera` con `cache: no` (datos personales), como en §5.
- **Validación — Resuelto.** Los errores de entrada responden `422` ProblemDetails con la lista de campos (`errors.cp`, y `errors.municipio` en la lista de espera), convención de TT-API-03; el `cp` del `GET` se enlaza a un objeto con su `IValidator` registrado.

**Dudas abiertas para el líder:**
- **(g) Pantallas sin sesión.** Los endpoints de §4 exigen JWT. Confirmar que ninguna pantalla sin sesión (HU-005, exploración antes de registrarse) necesita listar colonias por CP o entrar a la lista de espera; si alguna lo necesita, hay que decidir si estos endpoints pasan a públicos con el límite por IP o si HU-005 usa otra ruta.

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
