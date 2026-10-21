# TT-API-05 — Servicio único de fotos privadas: subida con SAS, confirmación sin EXIF y lectura con permiso

> **Jira:** KNOCK-42 · **Área:** API · **Épica:** EP-13 · **Sprint:** S1 (rama base `sprint-01`) · **Puntos:** 3
> **Servicio afectado:** Mercado (`Knock.Market.Api`, ADR-001)
> **Repositorio:** knock-backend
> **Fuentes:** DECISIONES.md #23 · DECISION_TECNICA.md «Archivos privados» · ADR-001 · ADR-002 · ADR-005
> **Skills:** story-workflow, spec-first-writing, vertical-slice-api, security-core, api-contract-openapi, testing-strategy-backend, foundation-backend
> **Estado:** Aprobada

## 1. Objetivo
Dar a Knock **un solo servicio** para subir y ver fotos privadas, que reutilizarán el perfil (HU-002), las solicitudes y los trabajos. Los archivos viven en contenedores privados de Blob Storage y solo se accede a ellos con enlaces SAS de corta vida (`DECISIONES.md #23`, `docs/DECISION_TECNICA.md` «Archivos privados»). En desarrollo y pruebas se usa Azurite.

**Primera entrega (antes de TT-BD-06):** solo lo que no depende de la base de datos: SAS de 5 min, validación por contenido real (no por extensión) y quitar EXIF. El registro de cada archivo (propietario, estado) queda detrás de una interfaz interna cuya implementación llega con TT-BD-06. El spike TT-BD-05 (KNOCK-147) recomienda **Azure DocumentDB** (API compatible con MongoDB, ADR-002).

## 2. Historia y casos de prueba
**TT-API-05** — Tarea técnica. Las pruebas usan Azurite real (Testcontainers) e imágenes sintéticas generadas en la prueba; nunca fotos de personas reales. La autenticación usa la sesión JWT real de TT-API-02 (KNOCK-21). Las partes que necesitan la base de documentos (propietario y estado del archivo) están **pendientes de TT-BD-06**.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-05-01 | **V1.** Dado un enlace SAS emitido por el servicio (de escritura o de lectura), cuando se usa después de su vencimiento de 5 min, entonces Blob Storage lo rechaza (`403`) y no se lee ni se escribe el blob. | `IntegrationTest/PrivateFilesTests.ExpiredSasLinkIsRejectedByBlobStorage_TC_TT_API_05_01`; Azurite con Testcontainers y `TimeProvider` falso para emitir el SAS ya vencido (o con vencimiento corto). |
| TC-TT-API-05-02 | **V2.** Dada una foto JPG con EXIF que incluye coordenadas GPS, cuando se sube y se confirma, entonces la versión guardada y la versión reducida no conservan EXIF (ni GPS, ni modelo de cámara, ni fecha) y respetan la orientación original. | `IntegrationTest/PrivateFilesTests.ConfirmedPhotoHasNoExifIncludingGps_TC_TT_API_05_02`; JPG sintético con EXIF GPS creado en la prueba; leer los blobs resultantes de Azurite y comprobar que no hay perfil EXIF. |
| TC-TT-API-05-03 | **V3.** Dado un archivo declarado de 9 MB o un PDF (aunque se llame `.jpg`), cuando se pide la subida o se confirma, entonces la API responde `422` con `ProblemDetails` y no queda ningún blob en el contenedor final. | `IntegrationTest/PrivateFilesTests.OversizedOrNonImageFileIsRejectedWith422_TC_TT_API_05_03`; teoría con 9 MB declarados en la petición, 9 MB reales subidos con SAS y PDF renombrado. |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `POST /api/v1/archivos/subidas`: recibe el uso previsto (tipo de recurso), el tipo de contenido y el tamaño declarado; rechaza con `422` tamaños > 8 MB y tipos distintos de JPG/PNG; devuelve un SAS **de escritura** sobre un blob nuevo de un contenedor privado de cuarentena, válido 5 min.
  - `POST /api/v1/archivos/{id}/confirmar`: descarga el blob, verifica **por contenido real** (firma/magic bytes y decodificación completa, no por extensión ni `Content-Type`) que es JPG o PNG y que mide ≤ 8 MB; aplica la orientación, **quita EXIF** y demás metadatos; genera una versión reducida; guarda ambas en el contenedor privado definitivo; borra el original de cuarentena. Si falla, `422` y borra el blob.
  - Lectura: SAS **de lectura** de 5 min solo para quien tiene permiso sobre el recurso (ver §4 y §5).
  - Registro del archivo detrás de una interfaz interna (`IArchivosStore` o similar); su implementación sobre Azure DocumentDB (API de MongoDB) está **pendiente de TT-BD-06**.
  - Azurite solo con Testcontainers en las pruebas (servicios reales en integración). **No** se toca `deploy/docker-compose.local.yml`: se rehace en TT-ARQ-01.
  - Blob real: solo Azurite hasta TT-AZ-02 (KNOCK-34); la configuración queda lista para identidad administrada.
  - Contenedores privados (acceso anónimo deshabilitado); nunca URLs públicas ni SAS de cuenta o de contenedor.
  - Comprobación `ready` de Blob en `/health/ready` (pendiente desde TT-API-01).
- **Out:**
  - Fotos de verificación (INE y selfie) de HU-021: tienen su propio cifrado, retención y auditoría (constitución §6).
  - Crear la cuenta de Storage real en Azure (TT-AZ-02, KNOCK-34).
  - **CDN (lineamiento 15c):** no aplica a estas fotos. Son privadas y se sirven con SAS de 5 min, que no deben cachearse en un CDN. Queda para TT-API-10, y solo para versiones públicas.
  - Moderación de imágenes (lineamiento 16b): fuera de esta tarea. Se deja documentado el punto de enganche en `POST /archivos/{id}/confirmar` (después de validar el contenido y antes de mover a `archivos`); TT-IA-02 decide proveedor, costo y la respuesta de la API ante un rechazo.
  - Antivirus y borrado programado de subidas sin confirmar (se anota como límite).
  - Las pantallas de cada HU que usa el servicio.
- **Dependencias:** TT-API-01 (KNOCK-20) para el proyecto base; TT-API-02 (KNOCK-21, PR #26, fusionado en `sprint-01`) para la sesión JWT: emisor y validación reales; TT-BD-05 (KNOCK-147) y TT-BD-06 para la base de documentos; HU-001 para el usuario real.

## 4. Contratos API y eventos
Diseño propuesto; se fija en OpenAPI al implementar. `[ApiVersion("1.0")]`, autenticados, errores con `ProblemDetails`, límite de peticiones en `docs/rate-limit-matrix.md`.

| Método y ruta | Entrada | Respuesta |
|---|---|---|
| `POST /api/v1/archivos/subidas` | `{ uso: "perfil" \| "solicitud" \| "trabajo", contentType: "image/jpeg" \| "image/png", tamanoBytes }` | `201` `{ id, urlSubida, venceEn }` (SAS de escritura, 5 min, un solo blob, solo `create`/`write`) · `422` tipo o tamaño declarado no permitido · `413` cuerpo de la petición demasiado grande · `401` |
| `POST /api/v1/archivos/{id}/confirmar` | — | `200` `{ id, estado: "listo", ancho, alto }` · `422` contenido no es JPG/PNG o > 8 MB · `404` si no existe o no es de quien llama · `409` ya confirmado |
| `GET /api/v1/archivos/{id}/lectura?version=original\|reducida` | — | `200` `{ url, venceEn }` (SAS de lectura, 5 min, un solo blob, solo `read`) · `404` si no existe o quien llama no tiene permiso (no se distingue para no filtrar existencia) |

- **Códigos de tamaño, sin mezclarlos:**
  - `413 Payload Too Large`: el **cuerpo HTTP** de la petición a la API (el JSON) supera el límite del endpoint; lo responde `RequestBodyLimitMiddleware` (`src/Knock.Api/Hosting/`) antes de llegar al controller. La foto nunca pasa por la API (va directo a Blob con el SAS).
  - `422 Unprocessable Entity`: el **archivo** no es válido: tamaño declarado o real > 8 MB, o tipo declarado o real distinto de JPG/PNG.
- **Contrato para otros servicios:** Identidad y confianza (foto de perfil, HU-002) y los módulos del propio Mercado usan el servicio **solo por su contrato**: `IMediaApi` mientras la API sea un solo proyecto; cuando se separen los servicios, el contrato pasa a `Knock.Contracts` (TT-ARQ-03, ADR-001). Ninguno lee la colección de archivos. El contrato permite confirmar que un `archivoId` está `listo` y es de un usuario, y emitir un SAS de lectura **después** de que el dueño del recurso autorizó a quien lo pide.
- **Eventos:** ninguno en esta tarea.
- Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y regenerar `docs/openapi.v1.json` con `scripts/export_openapi.py`.

## 5. Modelo de datos
- **Decisión (Tech Lead, revisión de esta spec):** el módulo de archivos (`Media`) vive en el **servicio de Mercado** (`Knock.Market.Api`, base lógica `market`, ADR-001), junto a Catalog, Requests y Reputation. Expone solo `AddMediaModule` e `IMediaApi`; el resto es `internal`. Requiere agregarlo a `tests/UnitTest/Architecture/ModuleCatalog.cs`.
- **Base:** el spike TT-BD-05 (KNOCK-147) recomienda **Azure DocumentDB** (API compatible con MongoDB, ADR-002).
- **Colección `archivos`** en la base `market`: `{ _id, propietarioId, uso, estado, contentType, tamanoBytes, blobOriginal, blobReducido, ancho, alto, creadoEn, confirmadoEn }`; `estado ∈ {pendiente, listo, rechazado}`. Índices: uno por `propietario_id` y uno por `estado`. Pool de conexiones con `maxPoolSize` fijo. La implementación es de **TT-BD-06**; hasta entonces el acceso va por una interfaz interna del módulo y no se crea esquema.
- Nombres de blob generados por el servidor (UUID v7), nunca con el nombre que envía el cliente.
- Contenedores: `cuarentena` (subidas sin confirmar) y `archivos` (definitivo), ambos privados.
- **Dueño del recurso:** `propietarioId` es el `sub` del JWT de la petición, emitido y validado por la sesión real de TT-API-02 (KNOCK-21); no hay referencia a una colección de usuarios hasta HU-001. El permiso de lectura es: (1) el propietario, o (2) otro módulo o servicio que llamó al contrato tras validar su propia regla (p. ej. Requests autoriza al técnico invitado a ver las fotos de una solicitud). Media no conoce las reglas de otros módulos.
- **Librería de imágenes (comparación):**

| | SixLabors.ImageSharp | SkiaSharp |
|---|---|---|
| Licencia | Six Labors Split License 1.0: Apache 2.0 para código abierto, uso transitivo o empresas con < 1 M USD de ingresos; si no, licencia comercial de pago | MIT |
| Pasa el CI (`deny-licenses: AGPL-3.0, GPL-3.0, GPL-2.0, SSPL-1.0`) | No está en la lista negada, pero la licencia no es SPDX estándar y obliga a revisar la elegibilidad si Knock genera ingresos | Sí, sin condiciones |
| Dependencias | 100 % administrado, sin binarios nativos | Binarios nativos de Skia: en Linux/contenedor requiere `SkiaSharp.NativeAssets.Linux.NoDependencies` |
| EXIF y orientación | API directa para quitar perfiles y `AutoOrient` | Recodificar desde el bitmap descarta metadatos; la orientación se aplica a mano con `SKCodec.EncodedOrigin` |

  **Decisión (Tech Lead):** **SkiaSharp (MIT)**, porque su licencia no tiene condiciones de ingresos y no deja dudas en la cadena de suministro; el costo es el paquete nativo de Linux en el Dockerfile y unas líneas para la orientación. Queda sujeta a la evaluación del paquete al implementar (licencia, mantenimiento y vulnerabilidades conocidas), con el resultado registrado en `Memoria.md`.

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Enlaces filtrados o reenviados dan acceso indefinido a fotos privadas. | SAS de un solo blob, permisos mínimos (`create`/`write` o `read`), vencimiento 5 min; contenedores privados; nunca URLs públicas ni SAS de cuenta; sin CDN para estas fotos. | TC-TT-API-05-01 |
| EXIF con GPS revela dónde vive o trabaja la persona. | Confirmar recodifica la imagen y descarta EXIF/XMP/IPTC antes de guardar original y reducida. | TC-TT-API-05-02 |
| Archivo malicioso o no imagen disfrazado con extensión `.jpg` (polyglot, PDF, SVG con script). | Validar por magic bytes y decodificación completa; solo JPG/PNG; guardar la versión recodificada, no los bytes del cliente; cuarentena hasta confirmar. | TC-TT-API-05-03 |
| Archivos enormes o «bombas de descompresión» agotan memoria. | Tamaño declarado ≤ 8 MB (`422`), tamaño real del blob ≤ 8 MB antes de descargar, límite de dimensiones antes de decodificar; cuerpo JSON acotado por `RequestBodyLimitMiddleware` (`413`). | TC-TT-API-05-03 |
| Un usuario lee o confirma archivos de otro (BOLA). | `propietarioId` del JWT; `404` sin distinguir existencia; lectura ajena solo por el contrato tras la autorización del dueño del recurso. **Pendiente de TT-BD-06** (requiere el registro persistido). | Pendiente de TT-BD-06 |
| Otro servicio lee la colección de archivos directamente. | Regla 1 de ADR-001: solo por contrato; la colección vive en la base `market`. | Revisión de `security-reviewer` |
| Nombre de blob controlado por el cliente (path traversal, sobrescritura). | Nombre generado por el servidor; el SAS apunta a un blob que no existe aún. | TC-TT-API-05-03 |
| Cadena de conexión o clave de cuenta en código o logs. | Configuración y secretos según la constitución; en Azure, identidad administrada y SAS de delegación de usuario (TT-AZ-02); no registrar URLs con SAS. | Revisión de `security-reviewer` |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:**

| Tipo | ¿Aplica? | Motivo / herramienta |
|---|---|---|
| Integración con Azurite | Sí | Testcontainers con Azurite: TC-TT-API-05-01 a 03 (SAS, validación por contenido, EXIF). |
| Integración con la base de documentos | **Pendiente de TT-BD-06** | Testcontainers (imagen compatible con la API de MongoDB) para el registro del archivo (propietario, estado, `404` a ajenos, `409` ya confirmado). Hasta entonces, la interfaz interna se prueba con un doble en unitarias. |
| Unitarias | Sí | Detección de tipo por magic bytes, límites de tamaño/dimensiones, cálculo de vencimiento con `TimeProvider` falso, regla de permiso de lectura. |
| Seguridad negativa | Sí (parcial) | Con el emisor y la validación JWT reales de TT-API-02: sin JWT → `401`; cuerpo JSON excesivo → `413`; SAS de lectura no permite escribir y viceversa; URLs con SAS ausentes en logs. Confirmar o leer archivo ajeno → `404` **pendiente de TT-BD-06**. `tests/SecurityTest`. |
| Contrato | Sí | `/api/v1/archivos/*` contra `docs/openapi.v1.json`. `tests/ContractTest`. |
| Arquitectura | Sí | El módulo solo expone `AddMediaModule` e `IMediaApi` (`ModuleSurfaceTests` y `ModuleBoundaryTests`). |
| Eventos, rendimiento, UI | No | Sin eventos; carga y UI fuera de esta tarea. |
| Mutación | Según la meta vigente del módulo | Nightly con Stryker. |

**Gates:** `AGENTS.md §5`. Esta spec es un borrador: no se han escrito ni ejecutado pruebas.

**Límites:** sin antivirus; los blobs sin confirmar se limpian en una tarea posterior (regla de ciclo de vida o mensaje programado).

**Decisiones (revisión del Tech Lead):**
- El módulo de archivos vive en el servicio de **Mercado**; Identidad y confianza lo consume solo por su contrato, sin leer la colección.
- La persistencia es la colección `archivos` en Azure DocumentDB (recomendación del spike TT-BD-05), con índices por `propietario_id` y por `estado` y `maxPoolSize` fijo; hasta TT-BD-06 queda detrás de una interfaz interna y la primera entrega cubre solo SAS, validación por contenido y EXIF.
- CDN no aplica a estas fotos privadas (TT-API-10, solo versiones públicas).

**Preguntas resueltas (Tech Lead):**
- **(a) Librería — Resuelto.** SkiaSharp (MIT), sujeta a evaluación (licencia, mantenimiento, vulnerabilidades) al implementar y registro en `Memoria.md`.
- **(b) Sesión JWT — Resuelto.** Ya existe (TT-API-02, KNOCK-21, PR #26, fusionado en `sprint-01`). Se usan el emisor y la validación reales en código y pruebas.
- **(c) Azurite — Resuelto.** En pruebas con Testcontainers (servicios reales en integración). No se toca `deploy/docker-compose.local.yml`; se rehace en TT-ARQ-01.
- **(d) Blob real — Resuelto.** Solo Azurite hasta TT-AZ-02 (KNOCK-34).
- **(e) Contrato — Resuelto.** `IMediaApi` mientras sea un solo proyecto; al separar servicios, en `Knock.Contracts` (TT-ARQ-03).
- **(f) Moderación — Resuelto.** Fuera de esta tarea. Se documenta el punto de enganche en `POST /archivos/{id}/confirmar` (tras validar el contenido y antes de mover a `archivos`); TT-IA-02 decide la respuesta ante un rechazo.

**Preguntas abiertas:** ninguna.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-21
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
