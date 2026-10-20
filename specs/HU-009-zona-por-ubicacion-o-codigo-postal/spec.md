# HU-009 — Indicar mi zona con mi ubicación o código postal

> **Jira:** KNOCK-47 · **Épica:** EP-02 · **Módulo:** Catalog · **Sprint:** por definir (ver §8, pregunta b) · **Puntos:** 3
> **Repositorio:** knock-backend (capa [Back]); la capa Mobile (Flutter) vive en knock-frontend
> **Skills:** story-workflow, spec-first-writing, vertical-slice-api, postgis-geo, security-core, api-contract-openapi, testing-strategy-backend
> **Estado:** Borrador

## 1. Objetivo
**Como** persona que se registra, **quiero** indicar mi zona con mi ubicación o mi código postal, **para** ver expertos cerca sin dar mi dirección.

El paso se pide después del nombre (`docs/referencia/FUNCIONES_Y_FLUJOS.md §3.3`). Solo se guardan la **colonia** y el **código postal**; las coordenadas se convierten en colonia y se descartan. La colonia se calcula con la tabla propia SEPOMEX + PostGIS, sin geocodificación externa (`DECISIONES.md #22`). Fuera de cobertura se ofrece lista de espera (`DECISIONES.md #12`). La ciudad piloto es Tula de Allende, Hidalgo (decisión del equipo del 2026-10-13, registrada en TT-BD-02); los textos de `DECISIONES.md` aún dicen Pachuca y se actualizan en un PR separado.

## 2. Historia y casos de prueba
**HU-009** — Historia de usuario. Todos los puntos y CP de las pruebas son sintéticos o del extracto SEPOMEX versionado; nunca ubicaciones de personas reales.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-HU-009-01 | **CA1.** Dado que la persona concede el permiso de ubicación y su punto está en la ciudad piloto, cuando la app consulta la zona, entonces el backend propone la colonia de ese punto, la persona puede confirmarla o cambiarla, y las coordenadas no se guardan en ninguna tabla. | `IntegrationTest/ZoneEndpointsTests.LocationInsidePilotCityProposesNeighborhoodWithoutPersistingCoordinates_TC_HU_009_01`; Testcontainers PostGIS, `GET /api/v1/zonas/colonia` con punto sintético dentro de una colonia piloto y revisión de que ninguna tabla contiene la coordenada. Flujo de pantalla en knock-frontend. |
| TC-HU-009-02 | **CA2.** Dado que la persona niega el permiso de ubicación o la ubicación falla, cuando continúa, entonces la app le pide su código postal. | Lógica de pantalla en knock-frontend (otro repo). El soporte backend es `GET /api/v1/zonas/cp/{cp}`, verificado en TC-HU-009-03. |
| TC-HU-009-03 | **CA3.** Dado un CP de la ciudad piloto presente en el catálogo, cuando la persona lo escribe, entonces se listan sus colonias para elegir una. | `IntegrationTest/ZoneEndpointsTests.PilotPostalCodeListsItsNeighborhoods_TC_HU_009_03`; CP tomado de `db/sepomex/tula_colonias.csv`; la respuesta trae solo colonias de ese CP. |
| TC-HU-009-04 | **CA4.** Dado un CP o una ubicación fuera de cobertura, cuando se consulta, entonces la respuesta indica «fuera de cobertura» con el nombre de la ciudad piloto para que la app muestre «Por ahora solo atendemos [ciudad piloto]» y el botón «Avísame cuando lleguen». | `IntegrationTest/ZoneEndpointsTests.OutOfCoverageLocationOrPostalCodeReturnsPilotCityAndWaitlistOffer_TC_HU_009_04`; un punto sintético de otra ciudad y un CP válido sin colonias activas. |
| TC-HU-009-05 | **CA5.** Dada una consulta fuera de cobertura, cuando la persona confirma la lista de espera, entonces se guarda un registro con su CP (y municipio si se conoce) y la respuesta permite a la app agradecerle. | `IntegrationTest/ZoneEndpointsTests.JoiningWaitlistStoresPostalCodeAndReturnsCreated_TC_HU_009_05`; `POST /api/v1/zonas/lista-espera` y lectura de `catalog.lista_espera`; sin coordenadas en la fila. |
| TC-HU-009-06 | **CA6.** Dado que la persona elige «Ahora no», cuando continúa, entonces se le asigna la colonia por defecto **Centro** de la ciudad piloto, que puede cambiar después. | `IntegrationTest/ZoneEndpointsTests.SkippingZoneAssignsDefaultCentroNeighborhood_TC_HU_009_06`; `PUT /api/v1/me/zona` con la opción «omitir» guarda Centro y su CP. |
| TC-HU-009-07 | **CA7.** Dado que la persona confirma una colonia, cuando se guarda su zona, entonces en su perfil solo quedan la colonia y el CP (nunca coordenadas). | `IntegrationTest/ZoneEndpointsTests.ConfirmingNeighborhoodStoresOnlyNeighborhoodAndPostalCode_TC_HU_009_07`; `PUT /api/v1/me/zona` y lectura del perfil: solo `colonia_id` y `cp`. Depende de la entidad de usuario (§8, pregunta c). |

## 3. Alcance / No-alcance
- **In (este repositorio, capa [Back]):**
  - Usar la ubicación: recibir `lat`/`lng`, convertirlos en colonia con la consulta existente de `ICatalogApi` y descartarlos (no se guardan ni se registran).
  - Buscar por CP: listar las colonias activas de un CP.
  - Confirmar o cambiar la colonia propuesta y guardarla en el perfil (colonia y CP).
  - «Ahora no»: asignar la colonia por defecto Centro de la ciudad piloto.
  - Lista de espera fuera de cobertura: guardar CP (y municipio si se conoce) de quien la pida.
- **In (knock-frontend, solo referencia):** pantalla «¿Dónde buscas expertos?», permiso de ubicación, fallback a CP, selección y confirmación de colonia, aviso de cobertura y agradecimiento. Se especifica e implementa en knock-frontend.
- **Out:**
  - Dirección exacta del servicio (HU-010).
  - Cambiar la zona después del registro (HU-049).
  - Geocodificación externa (Google, Mapbox, HERE, Nominatim).
  - Avisar a la lista de espera cuando se abra cobertura.
- **Reglas:**
  - Nunca guardar coordenadas, ni en tablas ni en logs, trazas o mensajes de error.
  - Cobertura = colonias activas del catálogo (`catalog.colonias`). Un punto o CP sin colonia activa está fuera de cobertura.
- **Dependencias:** TT-BD-02 (KNOCK-23) para el catálogo y la consulta por punto/CP; HU-001/HU-002 para la entidad de usuario autenticado (§8, pregunta c).

## 4. Contratos API y eventos
Diseño propuesto; se fija en OpenAPI al implementar. Todos bajo `[ApiVersion("1.0")]`, autenticados con JWT, con errores en `ProblemDetails` y límite de peticiones según `docs/rate-limit-matrix.md`.

| Método y ruta | Entrada | Respuesta |
|---|---|---|
| `GET /api/v1/zonas/colonia?lat={lat}&lng={lng}` | `lat` ∈ [-90, 90], `lng` ∈ [-180, 180] | `200` `{ enCobertura: true, colonia: { id, nombre, cp } }` · `200` `{ enCobertura: false, ciudadPiloto: "Tula de Allende" }` · `400` fuera de rango o ausentes |
| `GET /api/v1/zonas/cp/{cp}` | `cp` = 5 dígitos | `200` `{ enCobertura: true, colonias: [{ id, nombre, cp }] }` · `200` `{ enCobertura: false, ciudadPiloto }` · `400` formato inválido |
| `POST /api/v1/zonas/lista-espera` | `{ cp, municipio? }` | `201` · `400` CP inválido · repetir con el mismo usuario y CP no duplica (`200` idempotente) |
| `PUT /api/v1/me/zona` | `{ coloniaId }` o `{ omitir: true }` | `204` · `400` colonia inexistente o inactiva |

- Las coordenadas viajan solo en la query de `GET /zonas/colonia`; el middleware de logging debe redactar `lat`/`lng` de la URL.
- Los tipos públicos del módulo siguen siendo solo `AddCatalogModule` e `ICatalogApi`; controllers, DTOs, validadores y servicios son `internal`.
- **Eventos:** ninguno en esta historia.
- Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y regenerar `docs/openapi.v1.json` con `scripts/export_openapi.py`.

## 5. Modelo de datos
- **Nueva:** `catalog.lista_espera (id, usuario_id, cp, municipio NULL, creado_en timestamptz)`; único `(usuario_id, cp)`; sin columnas de coordenadas.
- **Existente:** `catalog.colonias` ya tiene `cp`. No tiene columna `activa`; hoy solo existe `piloto` (= tiene polígono). Para la regla «cobertura = colonias activas» se propone agregar `activa boolean NOT NULL DEFAULT true` (§8, pregunta e).
- **Perfil:** colonia (`colonia_id`) y `cp` del usuario. La tabla dueña depende de HU-001/HU-002 (Identity); Catalog no escribe tablas de otro módulo: `PUT /me/zona` valida la colonia con `ICatalogApi` y la persistencia del perfil va por `IIdentityApi` (§8, pregunta c).
- **Consulta por punto:** debe poder usar los índices GiST de `geom` y `centro` (§8, pregunta d).

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Coordenadas exactas guardadas o registradas revelan dónde vive la persona. | Usar el punto solo como parámetro; no persistirlo; redactar `lat`/`lng` en logs de petición y errores. | TC-HU-009-01 · TC-HU-009-07 |
| Coordenadas fuera de rango o invertidas clasifican otra zona o provocan errores 500. | Validar rango en el backend antes de consultar PostGIS; X=`lng`, Y=`lat`. | TC-HU-009-01 · TC-HU-009-04 |
| CP malformado o inyección en ruta. | Validar `^\d{5}$`; consulta parametrizada (sin SQL interpolado). | TC-HU-009-03 · TC-HU-009-04 |
| Un usuario guarda la zona o la lista de espera de otro (BOLA). | `usuario_id` sale del JWT, nunca del cuerpo; `/me/zona` solo actúa sobre el propio usuario. | TC-HU-009-05 · TC-HU-009-07 |
| Enumeración masiva del catálogo o llenado de la lista de espera. | Rate limit por usuario; lista de espera única por `(usuario_id, cp)`. | TC-HU-009-05 |
| Colonia inactiva o inexistente asignada al perfil. | Validar `coloniaId` contra colonias activas vía `ICatalogApi`. | TC-HU-009-06 · TC-HU-009-07 |

## 7. Pantallas
Pantalla «¿Dónde buscas expertos?» del registro (después del nombre): «Usar mi ubicación», «Escribir código postal», «Ahora no»; confirmación/cambio de colonia; aviso «Por ahora solo atendemos [ciudad piloto]» con «Avísame cuando lleguen» y agradecimiento. Se implementan en knock-frontend; este repositorio solo provee los endpoints de §4.

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Unitarias | Sí | Regla de cobertura (colonia activa → en cobertura; sin colonia → fuera), validadores de `lat`/`lng` y CP, elección de Centro por defecto. `tests/UnitTest`. |
| Integración PostGIS | Sí | Testcontainers `postgis/postgis` con fixture del catálogo: TC-HU-009-01, 03 a 07. |
| Seguridad negativa | Sí | `lat`/`lng` fuera de rango o ausentes → `400`; CP no numérico, de 4 o 6 dígitos → `400`; sin JWT → `401`; las coordenadas no aparecen en logs capturados (sink de prueba) ni en ninguna tabla. `tests/SecurityTest`. |
| Contrato | Sí | Respuestas de `/api/v1/zonas/*` y `/api/v1/me/zona` contra `docs/openapi.v1.json`. `tests/ContractTest`. |
| Rendimiento | Sí | Consulta de colonia por punto < 100 ms con el catálogo completo de la ciudad piloto, verificando con `EXPLAIN` que usa el índice GiST. `tests/DatabaseTest` + k6 smoke del endpoint. |
| Eventos | No | La historia no publica eventos. |
| Mutación | Según la meta vigente del módulo Catalog | Nightly con Stryker. |

**Gates:** `AGENTS.md §5`. Esta spec es un borrador: no se han escrito ni ejecutado pruebas.

**Preguntas abiertas:**
- **(a) Posible duplicado con KNOCK-141.** Si KNOCK-141 define `GET /colonias/en-punto`, decidir cuál ruta queda (`/zonas/colonia` o `/colonias/en-punto`) para no exponer dos endpoints con la misma consulta.
- **(b) Rama destino.** ¿El PR va a `sprint-00` o a `sprint-01`? La historia no es del Sprint 0 según el backlog.
- **(c) Dependencia de usuario.** No existe entidad de usuario: Identity solo tiene `IIdentityApi.ModuleName`. `PUT /me/zona` y `lista_espera.usuario_id` necesitan el usuario de HU-001/HU-002. ¿Se espera a esas historias o se acota esta a los `GET` y se deja el guardado para después?
- **(d) La consulta por punto no usa GiST.** `CatalogApi.FindNeighborhoodAtAsync` convierte `c.geom::geometry` y combina `ST_Covers` con `ST_DWithin` en un `OR`, por lo que PostgreSQL no puede usar el índice GiST sobre `geography` y hace un recorrido completo. Para cumplir < 100 ms hay que reescribirla (p. ej. usar `ST_Covers` sobre `geography`, o índice de expresión `geom::geometry`, y separar contención y respaldo por centro en consultas o `UNION` que usen cada índice). Requiere tocar código de TT-BD-02.
- **(e) Campo «activa».** `catalog.colonias` no tiene `activa`; solo `piloto`. ¿Se agrega `activa` (propuesta en §5) o se define cobertura = todas las colonias del catálogo?

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
