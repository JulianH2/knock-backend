# TT-API-05 — Servicio único de fotos privadas: subida con SAS, confirmación sin EXIF y lectura con permiso

> **Jira:** KNOCK-42 · **Área:** API · **Épica:** EP-13 · **Sprint:** S1 (rama base `sprint-01`) · **Puntos:** 3
> **Servicio afectado:** Mercado (`Knock.Market.Api`, ADR-001)
> **Repositorio:** knock-backend
> **Fuentes:** DECISIONES.md #23 · DECISION_TECNICA.md «Archivos privados» · ADR-001 · ADR-002 · ADR-005
> **Skills:** story-workflow, spec-first-writing, vertical-slice-api, security-core, api-contract-openapi, testing-strategy-backend, foundation-backend
> **Estado:** Borrador

## 1. Objetivo
Dar a Knock **un solo servicio** para subir y ver fotos privadas, que reutilizarán el perfil (HU-002), las solicitudes y los trabajos. Los archivos viven en contenedores privados de Blob Storage y solo se accede a ellos con enlaces SAS de corta vida (`DECISIONES.md #23`, `docs/DECISION_TECNICA.md` «Archivos privados»). En desarrollo y pruebas se usa Azurite.

**Primera entrega (antes de TT-BD-06):** solo lo que no depende de la base de datos: SAS de 5 min, validación por contenido real (no por extensión) y quitar EXIF. El registro de cada archivo (propietario, estado) queda detrás de una interfaz interna cuya implementación con MongoDB llega con TT-BD-06, después de que el spike TT-BD-05 (KNOCK-147) decida la base (ADR-002).

## 2. Historia y casos de prueba
**TT-API-05** — Tarea técnica. Las pruebas usan Azurite real (Testcontainers) e imágenes sintéticas generadas en la prueba; nunca fotos de personas reales. Las partes que necesitan MongoDB (propietario y estado del archivo) están **pendientes de TT-BD-06**.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-API-05-01 | **V1.** Dado un enlace SAS emitido por el servicio (de escritura o de lectura), cuando se usa después de su vencimiento de 5 min, entonces Blob Storage lo rechaza (`403`) y no se lee ni se escribe el blob. | `IntegrationTest/PrivateFilesTests.ExpiredSasLinkIsRejectedByBlobStorage_TC_TT_API_05_01`; Azurite con Testcontainers y `TimeProvider` falso para emitir el SAS ya vencido (o con vencimiento corto). |
| TC-TT-API-05-02 | **V2.** Dada una foto JPG con EXIF que incluye coordenadas GPS, cuando se sube y se confirma, entonces la versión guardada y la versión reducida no conservan EXIF (ni GPS, ni modelo de cámara, ni fecha) y respetan la orientación original. | `IntegrationTest/PrivateFilesTests.ConfirmedPhotoHasNoExifIncludingGps_TC_TT_API_05_02`; JPG sintético con EXIF GPS creado en la prueba; leer los blobs resultantes de Azurite y comprobar que no hay perfil EXIF. |
| TC-TT-API-05-03 | **V3.** Dado un archivo declarado de 9 MB o un PDF (aunque se llame `.jpg`), cuando se pide la subida o se confirma, entonces la API responde `422` con `ProblemDetails` y no queda ningún blob en el contenedor final. | `IntegrationTest/PrivateFilesTests.OversizedOrNonImageFileIsRejectedWith422_TC_TT_API_05_03`; teoría con 9 MB declarados en la petición, 9 MB reales subidos con SAS y PDF renombrado. |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - `POST /api/v1/archivos/subidas`: recibe el uso previsto (tipo de recurso), el tipo de contenido y el tamaño declarado; rechaza con `422` tamaños > 8 MB y tipos distintos de JPG/PNG; devuelve un SAS **de escritura** sobre un blob nuevo de un contenedor privado de cuarentena, válido 5 min.
  - `POST /api/v1/archivos/{id}/confirmar`: descarga el blob, verifica **por contenido real** (firma/magic bytes y decodificación completa, no por extensión ni `Content-Type`) que es JPG o PNG y que mide ≤ 8 MB; aplica la orientación, **quita EXIF** y demás metadatos; genera una versión reducida; guarda ambas en el contenedor privado definitivo; borra el original de cuarentena. Si falla, `422` y borra el blob.
  - Lectura: SAS **de lectura** de 5 min solo para quien tiene permiso sobre el recurso (ver §4 y §8, pregunta b).
  - Registro del archivo detrás de una interfaz interna (`IArchivosStore` o similar); su implementación con MongoDB está **pendiente de TT-BD-06**.
  - Azurite en `docker-compose` para desarrollo y pruebas, y fixture de Testcontainers para IntegrationTest.
  - Contenedores privados (acceso anónimo deshabilitado); nunca URLs públicas ni SAS de cuenta o de contenedor.
  - Comprobación `ready` de Blob en `/health/ready` (pendiente desde TT-API-01).
- **Out:**
  - Fotos de verificación (INE y selfie) de HU-021: tienen su propio cifrado, retención y auditoría (constitución §6).
  - Crear la cuenta de Storage real en Azure (TT-AZ-02, KNOCK-34).
  - **CDN (lineamiento 15c):** no aplica a estas fotos. Son privadas y se sirven con SAS de 5 min, que no deben cachearse en un CDN. Queda para TT-API-10, y solo para versiones públicas.
  - Moderación de imágenes (lineamiento 16b, TT-IA-02): fuera de esta entrega; ver §8, pregunta f.
  - Antivirus y borrado programado de subidas sin confirmar (se anota como límite).
  - Las pantallas de cada HU que usa el servicio.
- **Dependencias:** TT-API-01 (KNOCK-20) para el proyecto base; TT-API-02 para la autenticación JWT; TT-BD-05 (KNOCK-147) y TT-BD-06 para MongoDB; HU-001 para el usuario real.

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
- **Contrato para otros servicios:** Identidad y confianza (foto de perfil, HU-002) y los módulos del propio Mercado usan el servicio **solo por su contrato** (`IMediaApi` dentro de Mercado; tipos en `Knock.Contracts` y REST a través del gateway entre servicios, ADR-001). Ninguno lee la colección de archivos. El contrato permite confirmar que un `archivoId` está `listo` y es de un usuario, y emitir un SAS de lectura **después** de que el dueño del recurso autorizó a quien lo pide.
- **Eventos:** ninguno en esta tarea.
- Actualizar `docs/endpoints.md`, `docs/rate-limit-matrix.md` y regenerar `docs/openapi.v1.json` con `scripts/export_openapi.py`.

## 5. Modelo de datos
- **Decisión (Tech Lead, revisión de esta spec):** el módulo de archivos (`Media`) vive en el **servicio de Mercado** (`Knock.Market.Api`, base lógica `market`, ADR-001), junto a Catalog, Requests y Reputation. Expone solo `AddMediaModule` e `IMediaApi`; el resto es `internal`. Requiere agregarlo a `tests/UnitTest/Architecture/ModuleCatalog.cs`.
- **Colección MongoDB `archivos`** en la base `market` (ADR-002): `{ _id, propietarioId, uso, estado, contentType, tamanoBytes, blobOriginal, blobReducido, ancho, alto, creadoEn, confirmadoEn }`; `estado ∈ {pendiente, listo, rechazado}`. Índices y forma definitiva **pendientes de TT-BD-06**; hasta entonces el acceso va por una interfaz interna del módulo y no se crea esquema.
- Nombres de blob generados por el servidor (UUID v7), nunca con el nombre que envía el cliente.
- Contenedores: `cuarentena` (subidas sin confirmar) y `archivos` (definitivo), ambos privados.
- **Dueño del recurso antes de HU-001:** `propietarioId` es el `sub` del JWT de la petición; no hay referencia a una colección de usuarios (no existe). El permiso de lectura es: (1) el propietario, o (2) otro módulo o servicio que llamó al contrato tras validar su propia regla (p. ej. Requests autoriza al técnico invitado a ver las fotos de una solicitud). Media no conoce las reglas de otros módulos.
- **Librería de imágenes (comparación):**

| | SixLabors.ImageSharp | SkiaSharp |
|---|---|---|
| Licencia | Six Labors Split License 1.0: Apache 2.0 para código abierto, uso transitivo o empresas con < 1 M USD de ingresos; si no, licencia comercial de pago | MIT |
| Pasa el CI (`deny-licenses: AGPL-3.0, GPL-3.0, GPL-2.0, SSPL-1.0`) | No está en la lista negada, pero la licencia no es SPDX estándar y obliga a revisar la elegibilidad si Knock genera ingresos | Sí, sin condiciones |
| Dependencias | 100 % administrado, sin binarios nativos | Binarios nativos de Skia: en Linux/contenedor requiere `SkiaSharp.NativeAssets.Linux.NoDependencies` |
| EXIF y orientación | API directa para quitar perfiles y `AutoOrient` | Recodificar desde el bitmap descarta metadatos; la orientación se aplica a mano con `SKCodec.EncodedOrigin` |

  **Recomendación:** **SkiaSharp (MIT)**, porque su licencia no tiene condiciones de ingresos y no deja dudas en la cadena de suministro; el costo es el paquete nativo de Linux en el Dockerfile y unas líneas para la orientación. Si el equipo prefiere ImageSharp, registrar en el PR que el proyecto cumple las condiciones de Apache 2.0 de la Split License. En ambos casos el paquete pasa por la evaluación de dependencias antes de agregarse.

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
| Integración con MongoDB | **Pendiente de TT-BD-06** | Testcontainers de MongoDB para el registro del archivo (propietario, estado, `404` a ajenos, `409` ya confirmado). Hasta entonces, la interfaz interna se prueba con un doble en unitarias. |
| Unitarias | Sí | Detección de tipo por magic bytes, límites de tamaño/dimensiones, cálculo de vencimiento con `TimeProvider` falso, regla de permiso de lectura. |
| Seguridad negativa | Sí (parcial) | Sin JWT → `401`; cuerpo JSON excesivo → `413`; SAS de lectura no permite escribir y viceversa; URLs con SAS ausentes en logs. Confirmar o leer archivo ajeno → `404` **pendiente de TT-BD-06**. `tests/SecurityTest`. |
| Contrato | Sí | `/api/v1/archivos/*` contra `docs/openapi.v1.json`. `tests/ContractTest`. |
| Arquitectura | Sí | El módulo solo expone `AddMediaModule` e `IMediaApi` (`ModuleSurfaceTests` y `ModuleBoundaryTests`). |
| Eventos, rendimiento, UI | No | Sin eventos; carga y UI fuera de esta tarea. |
| Mutación | Según la meta vigente del módulo | Nightly con Stryker. |

**Gates:** `AGENTS.md §5`. Esta spec es un borrador: no se han escrito ni ejecutado pruebas.

**Límites:** sin antivirus; los blobs sin confirmar se limpian en una tarea posterior (regla de ciclo de vida o mensaje programado).

**Decisiones (revisión del Tech Lead):**
- El módulo de archivos vive en el servicio de **Mercado**; Identidad y confianza lo consume solo por su contrato, sin leer la colección.
- La persistencia es una colección de MongoDB; hasta TT-BD-06 queda detrás de una interfaz interna y la primera entrega cubre solo SAS, validación por contenido y EXIF.
- CDN no aplica a estas fotos privadas (TT-API-10, solo versiones públicas).

**Preguntas abiertas:**
- **(a) Librería.** ¿SkiaSharp (MIT, recomendada) o ImageSharp (Split License)? Requiere evaluación de paquete antes de agregarlo.
- **(b) Sesión JWT.** No hay autenticación JWT en el código de esta rama (Memoria: pasó a TT-API-02) ni usuario de HU-001. ¿Se espera a TT-API-02 o las pruebas usan un esquema de autenticación de prueba con `sub` sintético mientras tanto?
- **(c) Azurite.** ¿Se agrega al `deploy/docker-compose.local.yml` existente (hoy solo tiene `postgres`) o a un compose de pruebas aparte? En CI las pruebas usan Testcontainers de todos modos.
- **(d) Blob real.** TT-AZ-02 (KNOCK-34) aún no crea la cuenta de Storage; esta tarea solo se verifica con Azurite y deja la configuración lista para identidad administrada.
- **(e) Llamada desde Identidad y confianza.** ¿El contrato entre servicios es REST interno detrás del gateway o se adelanta a TT-ARQ-03? Mientras siga el monolito, es `IMediaApi`.
- **(f) Moderación de imágenes (lineamiento 16b, TT-IA-02).** ¿Se modera antes de guardar? El punto de enganche natural es `POST /archivos/{id}/confirmar`, después de validar el contenido y antes de mover a `archivos`; falta decidir proveedor, costo y qué responde la API si se rechaza.

## Aprobación
- [ ] Revisor 1 — nombre: fecha:
- [ ] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
