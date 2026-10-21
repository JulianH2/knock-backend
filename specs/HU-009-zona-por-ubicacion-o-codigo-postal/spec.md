# HU-009 — Indicar mi zona con mi ubicación o código postal

> **Jira:** KNOCK-47 · **Épica:** EP-02 · **Servicio:** Mercado (`Knock.Market.Api`, ADR-001) · **Módulo:** Catalog · **Sprint:** no en este sprint; se implementa después de la migración del catálogo (TT-BD-06), ver `plan.md` · **Puntos:** 3
> **Repositorio:** knock-backend (capa [Back]); la capa Mobile (Flutter) vive en knock-frontend
> **Skills:** story-workflow, spec-first-writing, vertical-slice-api, security-core, api-contract-openapi, testing-strategy-backend
> **Estado:** Borrador

**Cómo leer esta spec.** Lo que no depende de la base de datos (comportamiento, endpoints, validaciones, privacidad y pruebas unitarias, de seguridad y de contrato) queda **firme**. Lo que depende de la base se marca **«pendiente de TT-BD-05»** (spike de MongoDB en Azure DocumentDB, ADR-002) y se completa cuando el spike responda.

## 1. Objetivo
**Como** persona que se registra, **quiero** indicar mi zona con mi ubicación o mi código postal, **para** ver expertos cerca sin dar mi dirección.

El paso se pide después del nombre (`docs/referencia/FUNCIONES_Y_FLUJOS.md §3.3`). Solo se guardan la **colonia** y el **código postal**; las coordenadas se convierten en colonia y se descartan. La colonia se calcula con el catálogo propio SEPOMEX del servicio Mercado, sin geocodificación externa (`DECISIONES.md #22`). Fuera de cobertura se ofrece lista de espera (`DECISIONES.md #12`). La ciudad piloto es Tula de Allende, Hidalgo (decisión del equipo del 2026-10-13, registrada en TT-BD-02); los textos de `DECISIONES.md` aún dicen Pachuca y se actualizan en un PR separado.

**Antecedente:** el borrador anterior resolvía la colonia con PostgreSQL + PostGIS y EF Core (TT-BD-02). ADR-002 reemplaza esa base por MongoDB; TT-BD-06 rehace el catálogo. Nada de esta spec depende ya de PostGIS ni de EF Core.

## 2. Historia y casos de prueba
**HU-009** — Historia de usuario. Todos los puntos y CP de las pruebas son sintéticos o del extracto SEPOMEX versionado; nunca ubicaciones de personas reales. Las pruebas unitarias usan un doble del catálogo de colonias, así que no dependen de la base.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-HU-009-01 | **CA1.** Dado que la persona concede el permiso de ubicación y su punto está en la ciudad piloto, cuando la app consulta la zona, entonces el backend propone la colonia de ese punto, la persona puede confirmarla o cambiarla, y las coordenadas no se guardan ni salen en logs. | **Firme:** `UnitTest/Catalog/ZoneServiceTests.LocationInsidePilotCityProposesNeighborhood_TC_HU_009_01` (doble del catálogo; el servicio no pasa las coordenadas a ningún almacenamiento) y `SecurityTest/Catalog/ZoneLocationPrivacyTests.CoordinatesNeverAppearInLogs_TC_HU_009_01` (`ConsoleCapture` sobre `GET /api/v1/zonas/colonia`). **Pendiente de TT-BD-05:** prueba de integración que revise que ninguna colección contiene la coordenada. Flujo de pantalla en knock-frontend. |
| TC-HU-009-02 | **CA2.** Dado que la persona niega el permiso de ubicación o la ubicación falla, cuando continúa, entonces la app le pide su código postal. | Lógica de pantalla en knock-frontend (otro repo). El soporte backend es `GET /api/v1/zonas/cp/{cp}`, verificado en TC-HU-009-03. |
| TC-HU-009-03 | **CA3.** Dado un CP de la ciudad piloto presente en el catálogo, cuando la persona lo escribe, entonces se listan sus colonias para elegir una. | **Firme:** `UnitTest/Catalog/ZoneServiceTests.PilotPostalCodeListsItsNeighborhoods_TC_HU_009_03` (doble con colonias de `db/sepomex/tula_colonias.csv`; solo colonias de ese CP) y `ContractTest/Catalog/ZoneContractTests.ZoneResponsesMatchOpenApi_TC_HU_009_03`. **Pendiente de TT-BD-05:** integración contra el catálogo real. |
| TC-HU-009-04 | **CA4.** Dado un CP o una ubicación fuera de cobertura, cuando se consulta, entonces la respuesta indica «fuera de cobertura» con el nombre de la ciudad piloto para que la app muestre «Por ahora solo atendemos [ciudad piloto]» y el botón «Avísame cuando lleguen». | **Firme:** `UnitTest/Catalog/ZoneServiceTests.OutOfCoverageLocationOrPostalCodeReturnsPilotCity_TC_HU_009_04` (punto sintético de otra ciudad y CP válido sin colonias en cobertura) y `SecurityTest/Catalog/ZoneInputValidationTests.InvalidCoordinatesOrPostalCodeReturn400_TC_HU_009_04`. **Pendiente de TT-BD-05:** integración. |
| TC-HU-009-05 | **CA5.** Dada una consulta fuera de cobertura, cuando la persona confirma la lista de espera, entonces se guarda un registro con su CP (y municipio si se conoce) y la respuesta permite a la app agradecerle. | **Firme:** `UnitTest/Catalog/WaitlistServiceTests.JoiningWaitlistIsIdempotentPerUserAndPostalCode_TC_HU_009_05` (`201` la primera vez, `200` al repetir; el usuario sale del JWT; sin coordenadas). **Pendiente de TT-BD-05:** colección de lista de espera, índice único y su prueba de integración. |
| TC-HU-009-06 | **CA6.** Dado que la persona elige «Ahora no», cuando continúa, entonces se le asigna la colonia por defecto **Centro** de la ciudad piloto, que puede cambiar después. | **Firme:** `UnitTest/Catalog/ZoneServiceTests.SkippingZoneAssignsDefaultCentroNeighborhood_TC_HU_009_06` (`PUT /api/v1/me/zona` con `omitir: true` produce Centro y su CP). Dónde se guarda depende de la pregunta abierta (b). |
| TC-HU-009-07 | **CA7.** Dado que la persona confirma una colonia, cuando se guarda su zona, entonces en su perfil solo quedan la colonia y el CP (nunca coordenadas). | **Firme:** `UnitTest/Catalog/ZoneServiceTests.ConfirmingNeighborhoodKeepsOnlyNeighborhoodAndPostalCode_TC_HU_009_07` (lo que se envía al perfil es solo `coloniaId` + `cp`). **Pendiente de TT-BD-05 y de la pregunta (b):** integración que lea el perfil guardado. |

## 3. Alcance / No-alcance
- **In (este repositorio, capa [Back], servicio Mercado):**
  - Usar la ubicación: recibir `lat`/`lng`, convertirlos en colonia con el catálogo de Mercado y descartarlos (no se guardan ni se registran).
  - Buscar por CP: listar las colonias en cobertura de un CP.
  - Confirmar o cambiar la colonia propuesta y guardarla en el perfil (colonia y CP).
  - «Ahora no»: asignar la colonia por defecto Centro de la ciudad piloto.
  - Lista de espera fuera de cobertura: guardar CP (y municipio si se conoce) de quien la pida.
- **In (knock-frontend, solo referencia):** pantalla «¿Dónde buscas expertos?», permiso de ubicación, fallback a CP, selección y confirmación de colonia, aviso de cobertura y agradecimiento. Se especifica e implementa en knock-frontend.
- **Out:**
  - Dirección exacta del servicio (HU-010).
  - Cambiar la zona después del registro (HU-049).
  - Geocodificación externa (Google, Mapbox, HERE, Nominatim).
  - Avisar a la lista de espera cuando se abra cobertura.
- **Reglas (firmes):**
  - Nunca guardar coordenadas, ni en la base ni en logs, trazas o mensajes de error.
  - Un punto o CP sin colonia en cobertura está fuera de cobertura. Cómo se marca la cobertura en el catálogo es **pendiente de TT-BD-05** (pregunta d).
- **Dependencias:** TT-BD-05 (spike de la base) y TT-BD-06 (catálogo en MongoDB con la consulta por punto); HU-001/HU-002 para el usuario autenticado; contrato entre Identidad y Mercado (pregunta b).

## 4. Contratos API y eventos
**Firme.** Se fija en OpenAPI al implementar. Todos bajo `[ApiVersion("1.0")]`, autenticados con JWT, con errores en `ProblemDetails` y límite de peticiones según `docs/rate-limit-matrix.md`. Desde fuera se llega por el gateway (ADR-001).

| Método y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /api/v1/zonas/colonia?lat={lat}&lng={lng}` | `lat` ∈ [-90, 90], `lng` ∈ [-180, 180]; ambos obligatorios | `200` `{ enCobertura: true, colonia: { id, nombre, cp } }` · `200` `{ enCobertura: false, ciudadPiloto: "Tula de Allende" }` · `400` fuera de rango o ausentes |
| `GET /api/v1/zonas/cp/{cp}` | `cp` = exactamente 5 dígitos (`^\d{5}$`) | `200` `{ enCobertura: true, colonias: [{ id, nombre, cp }] }` · `200` `{ enCobertura: false, ciudadPiloto }` · `400` formato inválido |
| `POST /api/v1/zonas/lista-espera` | `{ cp, municipio? }` | `201` · `400` CP inválido · repetir con el mismo usuario y CP no duplica (`200` idempotente) |
| `PUT /api/v1/me/zona` | `{ coloniaId }` o `{ omitir: true }` | `204` · `400` colonia inexistente o fuera de cobertura |

- Las coordenadas viajan solo en la query de `GET /zonas/colonia`; el logging de peticiones (servicio y gateway) redacta `lat`/`lng` de la URL.
- Controllers, DTOs, validadores y servicios son `internal` al servicio. Lo que otro servicio necesite se expone por contrato (`Knock.Contracts`, ADR-001 regla 2).
- El servicio dueño de `PUT /me/zona` depende de la pregunta (b); la ruta y la respuesta no cambian.
- **Eventos:** ninguno en esta historia.
- Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y regenerar `docs/openapi.v1.json` con `scripts/export_openapi.py`.

## 5. Modelo de datos — pendiente de TT-BD-05
Todo este apartado se define cuando el spike confirme MongoDB en DocumentDB (ADR-002) y TT-BD-06 migre el catálogo. Las colecciones, llaves e índices se versionan como scripts de índices y semillas (ADR-002 regla 2).

- **Consulta «colonia de un punto»:** diseño pendiente de TT-BD-05 (punto dentro de polígono y respaldo por centro de la colonia).
- **Índice:** pendiente de TT-BD-05; se espera `2dsphere` sobre el polígono y el centro (por confirmar). Antes: GiST sobre `geography` en PostGIS.
- **Colecciones:** pendiente de TT-BD-05. Antes eran tablas SQL: `catalog.colonias` y `catalog.lista_espera (id, usuario_id, cp, municipio, creado_en)` con único `(usuario_id, cp)`.
- **Invariantes firmes, sea cual sea el diseño:** ningún documento guarda coordenadas de la persona; la lista de espera es única por usuario y CP; el usuario sale del JWT.
- **Perfil:** Identidad guarda solo `coloniaId` + `cp` (pregunta b).

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Coordenadas exactas guardadas o registradas revelan dónde vive la persona. | Usar el punto solo como parámetro; no persistirlo; redactar `lat`/`lng` en logs de petición y errores. | TC-HU-009-01 · TC-HU-009-07 |
| Coordenadas fuera de rango o invertidas clasifican otra zona o provocan errores 500. | Validar rango en el backend antes de consultar la base; X=`lng`, Y=`lat`. | TC-HU-009-01 · TC-HU-009-04 |
| CP malformado o inyección en la ruta o en el filtro. | Validar `^\d{5}$`; filtros con parámetros tipados, nunca concatenando la entrada. | TC-HU-009-03 · TC-HU-009-04 |
| Un usuario guarda la zona o la lista de espera de otro (BOLA). | El usuario sale del JWT, nunca del cuerpo; `/me/zona` solo actúa sobre el propio usuario. | TC-HU-009-05 · TC-HU-009-07 |
| Enumeración masiva del catálogo o llenado de la lista de espera. | Rate limit por usuario; lista de espera única por usuario y CP. | TC-HU-009-05 |
| Colonia inexistente o fuera de cobertura asignada al perfil. | Validar `coloniaId` contra el catálogo de Mercado por contrato. | TC-HU-009-06 · TC-HU-009-07 |

## 7. Pantallas
Pantalla «¿Dónde buscas expertos?» del registro (después del nombre): «Usar mi ubicación», «Escribir código postal», «Ahora no»; confirmación/cambio de colonia; aviso «Por ahora solo atendemos [ciudad piloto]» con «Avísame cuando lleguen» y agradecimiento. Se implementan en knock-frontend; este repositorio solo provee los endpoints de §4.

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí — **firme** | Regla de cobertura (colonia en cobertura → `enCobertura: true`; sin colonia → fuera con ciudad piloto), validadores de `lat`/`lng` y CP, Centro por defecto, idempotencia de la lista de espera, perfil solo con `coloniaId` + `cp`. Doble del catálogo; sin base. `tests/UnitTest`. |
| Seguridad negativa | Sí — **firme** | `lat`/`lng` fuera de rango o ausentes → `400`; CP no numérico, de 4 o 6 dígitos → `400`; sin JWT → `401`; usuario del JWT, no del cuerpo; las coordenadas no aparecen en logs capturados con `ConsoleCapture` (mismo patrón que `SecurityTest/Identity/SensitiveDataLeakTests`). `tests/SecurityTest`. |
| Contrato | Sí — **firme** | Respuestas de `/api/v1/zonas/*` y `/api/v1/me/zona` contra `docs/openapi.v1.json`. `tests/ContractTest`. |
| Integración | Sí — **pendiente de TT-BD-05** | Contra el catálogo real (Testcontainers de MongoDB por confirmar, ADR-002): TC-HU-009-01, 03 a 07, incluida la revisión de que ninguna colección guarda coordenadas. |
| Rendimiento | Sí — **pendiente de TT-BD-05** | Colonia por punto < 100 ms con el catálogo completo de la ciudad piloto, verificando el plan de la consulta con el índice que fije el spike; k6 smoke del endpoint. |
| Eventos | No | La historia no publica eventos. |
| Mutación | Según la meta vigente del módulo Catalog | Nightly con Stryker. |

**Gates:** `AGENTS.md §5`. Esta spec es un borrador: no se han escrito ni ejecutado pruebas.

**Preguntas abiertas:**
- **(a) Posible duplicado con KNOCK-141.** Si KNOCK-141 define `GET /colonias/en-punto`, decidir cuál ruta queda (`/zonas/colonia` o `/colonias/en-punto`) para no exponer dos endpoints con la misma consulta.
- **(b) Zona del perfil y usuario.** Propuesta: Identidad guarda solo `coloniaId` + `cp` en el perfil y valida la colonia contra Mercado por contrato (`Knock.Contracts`); Mercado no escribe en la base de Identidad (ADR-001 regla 1). Falta decidir: qué servicio expone `PUT /me/zona`, si la validación es síncrona (REST) o por evento, y si se espera al usuario de HU-001/HU-002 o se acota esta historia a los `GET` y la lista de espera.
- **(c) Consulta «colonia de un punto» — pendiente de TT-BD-05.** El spike debe probar punto dentro de polígono con las colonias de Tula y el índice `2dsphere` (ADR-002, punto 2). TT-BD-06 la implementa; esta historia la consume.
- **(d) Cobertura — pendiente de TT-BD-05.** Antes se proponía agregar `activa` a `catalog.colonias`. Decidir en el diseño de la colección si la cobertura es un campo de la colonia o son todas las colonias del catálogo.

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
