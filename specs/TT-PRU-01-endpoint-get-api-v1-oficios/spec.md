# TT-PRU-01 — Ejercicio fuera de rol (Erick): endpoint GET /api/v1/oficios con su prueba

> **Jira:** KNOCK-29 · **Épica:** EP-13 · **Módulo:** Catalog · **Responsable:** Erick Trejo Reséndiz · **Sprint:** S0 (semana de prueba de roles) · **Puntos:** 1 · **Capas:** Back
> **Repositorio:** knock-backend · **Otra parte en:** — (knock-frontend solo vendoriza el contrato cuando exista la publicación; ver §4)
> **Ficha:** `docs/backlog/TT-PRU-01.md` · **Rama:** `feature/KNOCK-29-trades-endpoint` → PR a `sprint-01` (PR #31)
> **Depende de:** TT-API-01 (KNOCK-20), TT-BD-02 (KNOCK-23) — ambas ya fusionadas (en `sprint-01`)
> **Fuentes:** DECISIONES.md #1 (oficios como datos) · DECISION_TECNICA.md (REST + OpenAPI `/api/v1`, módulo Catalog) · specs TT-API-01 y TT-BD-02
> **Skills:** story-workflow, vertical-slice-api, api-contract-openapi, testing-strategy-backend, security-core, reuse-before-create
> **Estado:** Aprobada

## 1. Objetivo
Ejercicio fuera de rol (Erick es de móvil): hacer un endpoint pequeño del backend de punta a punta. `GET /api/v1/oficios` devuelve los oficios **activos** del catálogo (`catalog.oficios`, sembrado por TT-BD-02) como `[{ clave, nombre, modalidad }]` ordenados por nombre, con una prueba de integración con `WebApplicationFactory` + Testcontainers (PostGIS real). Es el **primer endpoint de negocio** del backend: fija el molde de controller dentro de un módulo (ver §8, decisión D1) y es el primer cambio del contrato OpenAPI.

## 2. Historia y casos de prueba
**TT-PRU-01** — Tarea técnica. Nombre de pruebas: `Metodo_Escenario_Resultado_TC_TT_PRU_01_KK`. Las pruebas de integración levantan su propio contenedor `postgis/postgis:16-3.4` con `PostgisFixture` y preparan la base con `CatalogTestDatabase.MigrateAndSeedAsync` (migraciones + `db/seeds/catalog/oficios.sql`). Datos extra solo sintéticos.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-PRU-01-01 | Dada la base migrada y con la semilla de oficios, cuando se hace `GET /api/v1/oficios`, entonces responde 200 `application/json` con un arreglo de exactamente 3 elementos, en este orden: `{clave:"ele", nombre:"Electricidad", modalidad:"domicilio"}`, `{clave:"mec", nombre:"Mecánica", modalidad:"taller"}`, `{clave:"plo", nombre:"Plomería", modalidad:"domicilio"}` (V1 de la ficha). | `IntegrationTest/Catalog/OficiosEndpointTests.GetOficios_SeededCatalog_Returns200WithThreeTradesOrderedByName_TC_TT_PRU_01_01` |
| TC-TT-PRU-01-02 | Dado, además de la semilla, un oficio sintético **inactivo** cuyo nombre quedaría primero (p. ej. `alb` / «Albañilería» / `domicilio` / `activo = false`), cuando se hace `GET /api/v1/oficios`, entonces no aparece y la respuesta sigue siendo los 3 oficios activos. | `IntegrationTest/Catalog/OficiosEndpointTests.GetOficios_InactiveTrade_IsExcluded_TC_TT_PRU_01_02` |
| TC-TT-PRU-01-03 | Dado un oficio sintético **activo** insertado al final cuyo orden por `clave` difiere de su orden por nombre (p. ej. `zap` / «Carpintería» / `taller`), cuando se hace `GET /api/v1/oficios`, entonces el arreglo sale ordenado por `nombre` (Carpintería, Electricidad, Mecánica, Plomería), no por `clave` ni por orden de inserción. | `IntegrationTest/Catalog/OficiosEndpointTests.GetOficios_OrderIsByNameNotByKeyOrInsertion_TC_TT_PRU_01_03` |
| TC-TT-PRU-01-04 | Dada la respuesta de `GET /api/v1/oficios`, entonces cada elemento tiene **solo** las propiedades `clave`, `nombre` y `modalidad` (camelCase, cadenas no vacías) y no expone `id`, `activo` ni otras columnas. | `IntegrationTest/Catalog/OficiosEndpointTests.GetOficios_Response_ExposesOnlyClaveNombreModalidad_TC_TT_PRU_01_04` |
| TC-TT-PRU-01-05 | Dado un cliente sin encabezado `Authorization` (endpoint público, ver §8 D3), cuando hace `GET /api/v1/oficios`, entonces responde 200 (no 401/403). | `IntegrationTest/Catalog/OficiosEndpointTests.GetOficios_WithoutCredentials_Returns200_TC_TT_PRU_01_05` |
| TC-TT-PRU-01-06 | Dado el entorno Development, cuando se pide `GET /openapi/v1.json`, entonces el documento contiene la operación GET de la ruta de oficios con respuesta 200 y un esquema de arreglo cuyos elementos tienen `clave`, `nombre` y `modalidad` (el contrato se genera del código y el controller es descubierto por MVC). | `IntegrationTest/OpenApi/OficiosOpenApiTests.GetOpenApi_Development_DocumentsOficiosOperation_TC_TT_PRU_01_06` |
| TC-TT-PRU-01-07 | Dado el PR de KNOCK-29 hacia `sprint-01`, cuando corre `ci-pr.yml`, entonces el check `gate` y todos los jobs que exige pasan (build Release 0/0 con `-warnaserror`, formato, Unit + Architecture, Integration, Security, Database, gobierno y escáneres) (V2 de la ficha). | Checks de GitHub Actions del PR (comando); enlace al run y salida de `dotnet build Knock.slnx -c Release --no-restore` y `dotnet format Knock.slnx --verify-no-changes` en el PR |

Regresión obligatoria (casos de otra spec, no se renumeran ni se copian): `TC-TT-API-01-07` (fronteras), `TC-TT-API-01-09` (solo 2 tipos públicos por módulo) y `TC-TT-API-01-05` (sin detalle de error fuera de Development) deben seguir en verde con el controller nuevo.

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Endpoint `GET /api/v1/oficios` en un controller delgado del módulo Catalog (`[ApiController]`, `[ApiVersion("1.0")]`, `[Route("api/v{version:apiVersion}/oficios")]`, `[ProducesResponseType]` por código, sin `try/catch` ni `DbContext`). Ubicación según §8 D1.
  - Consulta de solo lectura de oficios activos en un servicio interno del módulo Catalog: `AsNoTracking`, filtro `activo = true`, proyección a `clave`, `nombre`, `modalidad` y orden por nombre (regla exacta en §8 D4). Reutiliza `Oficio` y `CatalogDbContext` (TT-BD-02); no crea entidades ni migraciones.
  - DTO de respuesta `record` inmutable e `internal` con `clave`, `nombre`, `modalidad`.
  - D1-A: se **reutiliza** el `InternalControllerFeatureProvider` que ya existe en `Hosting/AuthenticationExtensions.cs` (`AddInternalControllers()`, de TT-API-02/KNOCK-21, con su prueba de descubrimiento TC-TT-API-02-06); no se crea otro proveedor y se mantiene `TC-TT-API-01-09`.
  - Pruebas de §2 en `tests/IntegrationTest` (reutilizando `KnockApiFactory`, `PostgisFixture` y `CatalogTestDatabase`).
  - `docs/endpoints.md` con la fila del endpoint (si el revisor lo confirma, §8 D6).
- **Out:**
  - Paginación, caché, `ETag`/`Cache-Control` y rate limiting (ficha §4; el límite de peticiones ya lo da TT-API-03, §8 D7).
  - Autenticación/autorización (TT-API-02) y cualquier otro endpoint de Catalog (colonias, técnicos, búsqueda).
  - Alta, baja o edición de oficios; traducción de nombres; oficio «Otro».
  - Crear `scripts/export_openapi.py`, `contract-publish.yml`, `docs/openapi.v1.json` o `tests/ContractTest` (no existen; `scripts/` y `.github/` son protegidos; §8 D5).
  - Cambios en knock-frontend (consumo del endpoint o vendorizado del contrato).
  - Cambiar `ICatalogApi`, semillas, migraciones o el esquema `catalog`.

## 4. Contratos API y eventos
| Método y ruta | Auth | Respuesta | Notas |
|---|---|---|---|
| `GET /api/v1/oficios` | pública (§8 D3) | 200 `application/json` | Arreglo (no paginado) de `OficioResponse`; sin parámetros de consulta ni cuerpo. Si no hay oficios activos: 200 con `[]`. Errores no controlados: 500 `application/problem+json` sin detalle fuera de Development (pipeline de TT-API-01). Versión desconocida (`/api/v9/oficios`): no se sirve (comportamiento de TT-API-01). |

Esquema de respuesta (`OficioResponse`, elementos del arreglo):

| Propiedad | Tipo | Ejemplo | Notas |
|---|---|---|---|
| `clave` | string (≤ 16) | `"ele"` | Clave estable del catálogo (única en `catalog.oficios`). |
| `nombre` | string (≤ 100) | `"Electricidad"` | Nombre para mostrar, en español. |
| `modalidad` | string | `"domicilio"` | Hoy `domicilio` o `taller` (check de la tabla). Se documenta como string con valores conocidos, no como `enum` cerrado, para no romper el contrato si se agrega otra modalidad (DECISIONES.md: remoto en fase 2) — §8 D8. |

Ejemplo:
```json
[
  { "clave": "ele", "nombre": "Electricidad", "modalidad": "domicilio" },
  { "clave": "mec", "nombre": "Mecánica", "modalidad": "taller" },
  { "clave": "plo", "nombre": "Plomería", "modalidad": "domicilio" }
]
```

**Cambio de contrato:** es una operación **nueva** (cambio compatible, no requiere `v2`). El documento OpenAPI se genera del código (`Microsoft.AspNetCore.OpenApi`, `GET /openapi/v1.json`, solo en Development) y el caso TC-TT-PRU-01-06 comprueba que la operación aparece. Hoy no existen `scripts/export_openapi.py`, `docs/openapi.v1.json`, `contract-publish.yml` ni `contracts/` en knock-frontend; los pasos del CI que los usan se omiten solos. Por eso:
1. En este PR el contrato vive en esta §4, en el OpenAPI generado en Development y (si D6) en `docs/endpoints.md`. **No** se escribe `docs/openapi.v1.json` a mano (AGENTS.md §4).
2. Cuando exista la exportación (`export_openapi.py` → `docs/openapi.v1.json`) y `contract-publish.yml`, este endpoint entra en el primer `docs/openapi.v1.json` versionado y el workflow abre el PR en knock-frontend que actualiza `contracts/openapi.v1.json`; allí se regeneran los clientes Dart y TypeScript (nunca a mano). El PR de KNOCK-29 debe decir «cambia el contrato: agrega `GET /api/v1/oficios`».

**Eventos:** ninguno.

## 5. Modelo de datos
Sin cambios. Se lee `catalog.oficios (id uuid, clave varchar(16) UNIQUE, nombre varchar(100), modalidad varchar(16) CHECK IN ('domicilio','taller'), activo bool)` creada por la migración `CatalogBaseOficiosColonias` y sembrada por `db/seeds/catalog/oficios.sql` (`mec`, `plo`, `ele`, los tres activos). Consulta: `WHERE activo` + proyección de tres columnas + orden de §8 D4; sin índice nuevo (tabla de catálogo de pocas filas).

**Caché (constitución 2.2.0 §10, decisión del líder en el PR #31, 2026-10-24):** `cache: 86400` para la colección de oficios (catálogo que cambia muy poco). Se declara aquí sin cambiar los casos; el repositorio con caché del servicio llega con TT-BD-08 (TT-BD-06, #73, deja las TTL declaradas pero sin capa de caché; nota del líder en el PR #31, 2026-10-28).

## 6. Seguridad, privacidad y amenazas
STRIDE breve. Endpoint público, de solo lectura, sin parámetros de entrada y sin datos personales.

| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Fuga de información (API3): exponer columnas internas (`id`, `activo`) o filas no publicadas | Proyección explícita a un DTO de 3 propiedades (nunca serializar la entidad); filtro `activo = true` en la consulta | TC-TT-PRU-01-02 · TC-TT-PRU-01-04 |
| Fuga de información: detalle técnico (cadena de conexión, SQL, stack trace) si la base falla | Sin `try/catch` en el controller; `UseExceptionHandler` + ProblemDetails sin detalle fuera de Development (TT-API-01) | TC-TT-API-01-05 (regresión, sin caso nuevo) |
| Suplantación / denegación por error de configuración: que una política de autorización global futura (TT-API-02) deje el catálogo en 401 y rompa el registro de técnicos | Decisión explícita «pública» (§8 D3); `[AllowAnonymous]` en la acción para que sobreviva a una política de respaldo | TC-TT-PRU-01-05 |
| Alteración / inyección (A03): SQL manipulado | Sin entrada del usuario; consulta LINQ de EF Core (sin SQL interpolado ni `FromSqlRaw`); solo `GET`, consulta `AsNoTracking` sin escritura | TC-TT-PRU-01-01 (comportamiento) · Semgrep en CI |
| Elevación / alteración de arquitectura: el controller usa internos de otro módulo o agrega tipos públicos al módulo | Controller y DTO dentro de Catalog e `internal` (D1-A) o en el host usando solo `ICatalogApi` (D1-B) | TC-TT-API-01-07 · TC-TT-API-01-09 (regresión) |
| Denegación (API4): endpoint anónimo que consulta la base en cada llamada | Respuesta acotada (catálogo de pocas filas, sin parámetros); cubierto por la política global de TT-API-03 (100 peticiones por minuto por IP; solo `/health` está exento; §8 D7) | Casos de TT-API-03 |
| Repudio | N/A: no cambia estado ni hay acción de usuario que auditar | — |

Checks de `security-core` que no aplican: 1 (autorización por recurso: no hay recurso de usuario), 2 (sin sesión, por decisión D3), 3 (sin entrada que validar), 5, 7, 8, 9, 10, 15 y 16 (sin datos personales, archivos, sesión de app, auditoría ni IA). 12 (headers/CORS) queda en la tarea del pipeline de seguridad.

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Integración (`tests/IntegrationTest`) | Sí | `KnockApiFactory(connectionString: …)` + `PostgisFixture` (Testcontainers `postgis/postgis:16-3.4`) + `CatalogTestDatabase.MigrateAndSeedAsync`; casos 01–06. Un contenedor por prueba, como el resto de la suite (CA1515 impide fixtures de colección públicos; Memoria 2026-10-16). Nunca EF InMemory. |
| Arquitectura (`tests/UnitTest/Architecture`) | Sí, regresión | `TC-TT-API-01-07` y `TC-TT-API-01-09` deben seguir en verde sin modificarlas. No se agrega regla nueva («controllers sin DbContext/try-catch» está en la estrategia pero aún no existe; fuera de este ejercicio). |
| Unitarias | No | La lógica es una consulta LINQ; probarla sin base sería probar EF. El comportamiento (filtro, orden, proyección) se prueba contra PostgreSQL real. |
| Seguridad negativa (`tests/SecurityTest`) | No (caso nuevo) | No hay autenticación ni entrada; la exposición de datos (04) y el acceso anónimo (05) necesitan base real y viven en IntegrationTest (SecurityTest no tiene Testcontainers; agregarlo sería una dependencia nueva). `TC-TT-API-01-05` cubre el error sin detalle. |
| Base de datos (`tests/DatabaseTest`) | No (caso nuevo) | Sin cambios de esquema ni semillas; `TC-TT-BD-02-03` ya prueba la semilla. |
| Contrato (`tests/ContractTest`, oasdiff) | No en esta tarea | El proyecto y `docs/openapi.v1.json` no existen; el caso 06 comprueba el OpenAPI generado. Ver D5. |
| Mutación | Informativa | Catalog no es módulo crítico (metas solo para Identity, Requests, Trust, Reputation). Mutantes esperables (quitar el filtro `activo`, cambiar el orden, agregar campos) los matan los casos 02, 03 y 04. |
| Rendimiento, fuzzing, caos, smoke, UI | No | Endpoint de 1 punto sin parámetros; RESTler/k6 cuando exista el OpenAPI exportado. |
| Comando / CI | Sí | Caso 07: checks de GitHub; build Release 0/0 y `dotnet format` sin cambios. |

**Gates:** `AGENTS.md §5`. En esta máquina no hay .NET 10 SDK ni Docker en marcha: la implementación debe ejecutar build, formato y las suites en un equipo que los tenga (o confiar solo en el CI para el caso 07) y decirlo en el informe; no se da nada por pasado sin ejecutarlo.

**Decisiones (resueltas por el revisor José Julián Hernández Hernández el 2026-10-19 en el PR #31):**
- **D1 — Dónde va el controller.** **Resuelto: A.** `OficiosController` `internal sealed` en `Modules/Catalog/Controllers/V1/`. El revisor pidió implementar el `ControllerFeatureProvider` en este PR para que TT-API-02 lo reutilizara, pero TT-API-02 (KNOCK-21, PR #26) se fusionó antes en `sprint-01` con `InternalControllerFeatureProvider` (`Hosting/AuthenticationExtensions.cs`, `AddInternalControllers()`) y su prueba de descubrimiento (TC-TT-API-02-06). Por eso este PR **lo reutiliza y no crea otro** (confirmado por el líder, 2026-10-20). *Propuesta original y alternativas:*
  - *Restricción real:* MVC por defecto solo descubre controllers **públicos**, y `TC-TT-API-01-09` exige que cada módulo tenga exactamente 2 tipos públicos (`AddXxxModule` e `I<Modulo>Api`). Un `public class OficiosController` en `Modules/Catalog/` rompe esa prueba aprobada. Fuera de `Modules/`, un controller público dispara CA1515 (solo está desactivado en `src/**/Modules/**.cs` y en el `TestController`), y con `TreatWarningsAsErrors` el build falla.
  - *A (propuesta):* `Modules/Catalog/Controllers/V1/OficiosController.cs` **`internal sealed`**, DTO `Modules/Catalog/Dtos/OficioResponse.cs` `internal`, servicio interno `Modules/Catalog/Services/…` que usa `CatalogDbContext`; y un `ControllerFeatureProvider` en `Hosting/` registrado en `AddControllers().ConfigureApplicationPartManager(...)` que acepte además controllers `internal` cuyo espacio de nombres empiece por `Knock.Api.Modules.`. Porqué: sigue la estructura de `vertical-slice-api` (cada módulo dueño de su HTTP), no toca `ICatalogApi` (que es para otros módulos) ni la prueba aprobada, y deja el molde para todas las HU. Riesgos a confirmar al implementar: que Asp.Versioning y el generador de OpenAPI traten igual al controller `internal` (lo prueban 01 y 06) y que el analizador no marque el controller como «nunca instanciado» (CA1812); si lo marca, la salida es una entrada en `.editorconfig` (archivo de analizadores, protegido) autorizada por el revisor, nunca un `#pragma` ni `NoWarn` global.
  - *B:* controller en el host (p. ej. `src/Knock.Api/Controllers/V1/`) que llama a `ICatalogApi`. Requiere agregar a `ICatalogApi` un método que devuelva los oficios sin crear un tipo público nuevo en Catalog (tupla `(Clave, Nombre, Modalidad)` o un tipo en `Common/`), y resolver CA1515 igual que en A (proveedor de controllers internos o excepción en `.editorconfig`). Mezcla HTTP del catálogo en el host y crece la interfaz entre módulos sin que otro módulo la necesite.
  - *Descartadas:* controller público en el módulo modificando `TC-TT-API-01-09` (cambia una spec aprobada de otra tarea); Minimal API desde `CatalogModuleExtensions` (no agrega tipos públicos, pero contradice la convención de controllers de AGENTS.md §6 y la ficha, que pide `OficiosController`).
- **D2 — Ruta en español vs. convención.** **Resuelto: `/api/v1/oficios`**, como la ficha, y propiedades `clave/nombre/modalidad`. El revisor corrige la skill `api-contract-openapi`. *Propuesta original:* La skill `api-contract-openapi` pide rutas en inglés, plural y kebab-case (sería `/api/v1/trades`); la ficha y su V1 piden `/api/v1/oficios` y propiedades `clave/nombre/modalidad`. Se propone respetar la ficha porque es el requisito y las propiedades ya están en español en la base y en la semilla; si el revisor prefiere `trades`, cambia §4 y los casos 01–06 (mismo número de casos). Cualquiera que se elija fija precedente para los demás recursos de Catalog.
- **D3 — ¿Público o con autenticación?** **Resuelto: público, con `[AllowAnonymous]` explícito.** *Propuesta original:* No hay autenticación todavía (TT-API-02); el catálogo no tiene datos personales y lo necesita la app antes o durante el registro (elegir oficios). Confirmar con Guillermo; si debe exigir sesión, el caso 05 cambia a «sin token → 401» y depende de TT-API-02.
- **D4 — Regla exacta de orden.** **Resuelto: `ORDER BY lower(nombre) COLLATE "C", clave`.** El límite de los acentos se anota en el código y en `Memoria.md`; con los 3 oficios actuales no afecta. **Nota del líder (2026-10-20):** esa regla es de PostgreSQL; con la migración del catálogo a MongoDB (TT-BD-06, ADR-002) la consulta se reimplementa. Por eso las pruebas (casos 01 y 03) verifican el **orden resultante** —por nombre sin importar mayúsculas y, a igual nombre, por clave—, no el SQL. *Propuesta original:* `ORDER BY lower(nombre) COLLATE "C", clave`, igual que el desempate de TT-BD-02, para que no dependa de la intercalación de la base (la imagen de pruebas y Azure pueden diferir). Límite conocido: con `"C"` un nombre que empiece con vocal acentuada («Ébano») quedaría al final; con los 3 oficios actuales no ocurre. Alternativa: intercalación ICU `es-x-icu` (verificar que exista en `postgis/postgis:16-3.4` y en Azure Flexible). Ordenar en memoria con `StringComparer` de cultura es otra opción, pero mueve la regla fuera de la base. **Nota del líder (PR #31, 2026-10-28):** con la migración del catálogo a MongoDB (TT-BD-06, #73), la regla pasa a **`nombreBusqueda` y luego `clave`**, sobre el campo ya normalizado (mayúsculas, sin acentos, espacios colapsados) e indexado de #73. La consulta es `CatalogStore.FindActiveOficiosAsync(ct)` (filtro `activo == true`, proyección `{clave, nombre, modalidad}`); `OficioQueries` solo la usa y no toca `IMongoCollection` (TC-TT-BD-06-16). Las pruebas 02 y 03 insertan los oficios sintéticos con `CatalogSeeder.SeedAsync` para que exista `nombreBusqueda` (sin él, un documento se ordena primero y TC-03 pasaría en falso). Motivo: evita el límite de los acentos de `COLLATE "C"` y aprovecha el índice; los casos no cambian.
- **D5 — Publicación del contrato.** **Resuelto: fuera de este PR.** No se crean `export_openapi.py`, `docs/openapi.v1.json` ni `contract-publish.yml` (son de `scripts/` y `.github/`); el revisor abre una tarea aparte. El PR dice «cambia el contrato: agrega `GET /api/v1/oficios`». *Propuesta original:* AGENTS.md y `api-contract-openapi` nombran `scripts/export_openapi.py`, `docs/openapi.v1.json`, `contract-publish.yml`, `scripts/check_endpoints.py` y `tests/ContractTest`, pero **ninguno existe** en el repo (ni `contracts/` en knock-frontend). Propuesta: este PR no los crea (son de `scripts/` y `.github/`, protegidos); el equipo abre una TT para la exportación y publicación del contrato y KNOCK-29 lo declara en el PR como cambio de contrato pendiente de publicar. Relacionado: con el versionado actual (sin `Asp.Versioning.Mvc.ApiExplorer`), el OpenAPI puede mostrar la ruta como `/api/v{version}/oficios` en lugar de `/api/v1/oficios`; el caso 06 acepta cualquiera de las dos formas mientras no se decida; corregirlo implica un paquete nuevo con evaluación y queda para esa TT.
- **D6 — `docs/endpoints.md`.** **Resuelto: sí**, se crea `docs/endpoints.md` con la fila del endpoint. *Propuesta original:* con la fila de §4 (AGENTS.md §5 lo exige y aún no existe). `check_endpoints.py` no existe; la comparación con los controllers no se automatiza aquí.
- **D7 — Rate limiting.** **Resuelto: cubierto por TT-API-03** (fusionado en `sprint-01`): la política global de 100 peticiones por minuto por IP aplica a este endpoint; el host de pruebas fija el límite en 1 000 000, así que estas pruebas no reciben 429 (revisión de Guillermo, 2026-10-23). *Propuesta original:* No existe `docs/rate-limit-matrix.md` ni `RateLimiter` en el pipeline (pendiente desde TT-API-01, Memoria 2026-10-15). Propuesta: aceptar el riesgo residual (§6) y que la tarea del pipeline de seguridad agregue el límite de endpoints públicos, incluido este.
- **D8 — `modalidad` en OpenAPI.** **Resuelto: string** con los valores conocidos descritos, no `enum`. *Propuesta original:* Propuesta: string con descripción de valores conocidos (`domicilio`, `taller`), no `enum`, para que agregar una modalidad no sea cambio incompatible. Confirmar.
- **D9 — Respuesta como arreglo y no `CursorPage<T>`.** **Resuelto: arreglo**, no `CursorPage<T>`, **como excepción documentada a la constitución 2.2.0 §11** (decisión del líder, PR #31, 2026-10-24): es un catálogo acotado (unas pocas decenas de filas como máximo). Mientras no pase de 50 elementos no se pagina; si pasa, el cambio a `CursorPage<T>` se publica como `v2`. *Propuesta original:* `vertical-slice-api` pide `CursorPage<T>` en listas públicas; la ficha pide `[{…}]` y deja fuera la paginación. Propuesta: arreglo, porque el catálogo lo administra el equipo y es pequeño; pasar a página después sería un cambio incompatible (→ `v2`). Confirmar.
- **Nota de la ficha:** la ficha dice rama `feature/KNOCK-29-endpoint-oficios`; la rama real es `feature/KNOCK-29-trades-endpoint` (descripción en inglés, CONTRIBUTING.md). La ficha también pide anotar en el PR cuánto tardó, qué fue difícil y el uso de IA.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-22
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
