# Spec 058 — Colección `archivos` de Media: registro de archivos privados en MongoDB y tope de subidas pendientes

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Clave de trabajo:** KNOCK-185 (TT-BD-10 · Colección archivos de Media) · **Relacionadas:** spec 013 (archivos y moderación, Aprobada), spec 032 (imágenes públicas, Aprobada), spec 020 (solicitudes, consume el flujo privado), spec 003 (bases MongoDB y Redis) y la spec histórica TT-API-05 (`specs/_legacy/TT-API-05-servicio-de-fotos-privadas-blob-sas/`)
> **Servicio:** Mercado (módulo `Media`) · **Colecciones:** `archivos` (nueva, base `market`) · **Sale:** contenedor privado `registros-temporales` y `BlobMediaFileStore`
> **Sprint:** 5 · **Responsable sugerido:** Alexis (dueña de la tarjeta; Jira la etiqueta `resp-angel`, `area-bd`, `sprint-5`) · **Puntos:** los de la tarjeta
> **Nota sobre la etiqueta `bloqueada`:** la tarjeta está bloqueada porque no existía spec; esta spec la desbloquea solo cuando la apruebe el Tech Lead.
> **Decisiones D1 a D8 (§14):** contestadas «sí» con la recomendación del autor por el Tech Lead (2026-11-20) y aplicadas en el texto de esta spec; la spec 020 se enmienda en el mismo PR (D4).

## 1. Objetivo
Que el registro de cada archivo privado de Media (fotos de una solicitud y, en general, todo lo que sube una persona al flujo de la spec 013) viva en una colección de MongoDB (`archivos`, base `market`) y no en un JSON por archivo dentro del contenedor privado `registros-temporales`, con índices, caducidad de lo que nunca se confirmó y un **tope de subidas pendientes por persona** que se cumple aunque lleguen peticiones al mismo tiempo. El contrato HTTP y los nombres de blob no cambian.

## 2. Historia y alcance
**2.1 Historia**
- **TT-BD-10 (KNOCK-185)** — Como equipo, necesitamos que los registros de archivos estén en MongoDB, con índices y un tope por persona, para cumplir la regla de que ningún dato de negocio vive en Blob (constitución, spec 003) y para que una persona no pueda llenar el almacenamiento con subidas que nunca confirma.

**Descripción de la tarjeta (se cita tal cual):** «Reemplazar el contenedor privado `registros-temporales` y `BlobMediaFileStore` de TT-API-05 por la colección `archivos` en MongoDB: `_id` ObjectId y campo `archivoId` (UUID v7) con índice único, `propietarioId` y `estado` en camelCase con sus índices, sin cambiar el contrato HTTP ni los nombres de blob; `IMediaFileStore` pasa a camelCase. Incluye el tope de subidas pendientes por usuario. Terminada cuando: los registros viven en MongoDB, se borra el contenedor `registros-temporales`, el contrato HTTP no cambia y hay pruebas de índices, de migración y del tope por usuario.»

**2.2 Dentro**
- La colección `archivos` (campos, tipos, índices, caducidad) y su script en `db/`.
- Un `MongoMediaFileStore` que implementa `IMediaFileStore` con los campos en camelCase y `_id` `ObjectId`; `archivoId` como identificador de negocio (UUID v7) con índice único.
- La migración de los registros que existan en `registros-temporales`, idempotente por `archivoId`.
- El tope de subidas pendientes por persona, contado de forma segura con peticiones concurrentes.
- Retirar `BlobMediaFileStore`, la opción `Media:RecordsContainer` y el contenedor `registros-temporales` (el borrado del contenedor en Azure lo ejecuta una persona, no el agente).
- Pruebas de índices, de migración, del tope y de concurrencia; contrato, documentación y `Memoria.md`.

**2.3 Fuera**
- Cambiar rutas, cuerpos, respuestas o códigos HTTP que ya existan (hoy el módulo `Media` de Mercado **no expone ninguna ruta** privada: ver §5).
- El flujo público de imágenes y su colección `subidas_imagen` (spec 032); no se mezclan (ver §7).
- Los blobs: nombres, contenedores `cuarentena` y `archivos`, SAS y procesamiento de imágenes no cambian.
- Las fotos de verificación de identidad (spec 025), que viven en Identidad.
- Unificar `archivos` y `subidas_imagen` (queda como decisión abierta, §14).
- Ejecutar nada en Azure (borrar el contenedor, correr la migración) ni cambiar Terraform ya aplicado.

## 3. Requisitos
- **R-01:** Existe la colección `archivos` en la base `market` con los campos de §6. `_id` es un `ObjectId` generado al insertar; `archivoId` es el identificador de negocio (UUID v7, 32 hexadecimales en minúscula, el mismo valor que hoy lleva `MediaFile.Id`) y tiene un índice **único**.
- **R-02:** Los campos y las colecciones siguen la convención de la spec 003 (R-03): colección en `snake_case` plural, campos en `camelCase`, fechas en UTC y campo `version` para la concurrencia optimista. `IMediaFileStore` y el documento persistido pasan de `snake_case` (`propietario_id`, `blob_key`, `creado_en`) a `camelCase` (`propietarioId`, `blobKey`, `creadoEn`). Los valores de `estado` siguen siendo `pendiente`, `listo` y `rechazado`.
- **R-03:** Los índices son los de §6 y los crea el script nuevo de `db/` (spec 003, R-05 y R-06): único por `archivoId`; compuesto por `propietarioId`, `estado` y `confirmarAntesDe` (cubre «archivos de una persona», «archivos en un estado» de una persona y el conteo de pendientes vigentes); y TTL sobre `expiraEn`. El script es idempotente: crea lo que falta y nunca borra ni cambia lo que ya existe.
- **R-04:** `MongoMediaFileStore` es la única implementación registrada en producción de `IMediaFileStore`. Conserva el comportamiento actual de la interfaz: `AddAsync` falla si ya existe el `archivoId` (`InvalidOperationException`), `FindAsync` devuelve el registro con su versión y `TryReplaceAsync` reemplaza solo si la versión sigue siendo la esperada (actualización condicionada por `archivoId` y `version`, que sube en 1); responde `false` si otra petición ganó. `InMemoryMediaFileStore` sigue existiendo solo para pruebas unitarias.
- **R-05:** El `propietarioId` es siempre el `sub` del JWT de la petición; nunca se acepta del cuerpo, de la ruta ni de un encabezado. Un archivo ajeno o inexistente produce el mismo resultado (`NotFound`, 404 uniforme) en `MediaService`, sin distinguirlos. `archivoId` no es enumerable: es un UUID v7 generado por el servidor y la consulta siempre lleva `archivoId` **y** se compara el dueño antes de revelar nada.
- **R-06:** Los nombres de blob y de contenedor no cambian: cuarentena `cuarentena/{blobKey}`; final `archivos/{blobKey}/original.jpg` y `archivos/{blobKey}/reducida.jpg`; `blobKey` son 32 hexadecimales aleatorios (128 bits) independientes de `archivoId`. El documento guarda `blobKey`, `blobOriginal` y `blobReducido` tal como hoy.
- **R-07:** **Caducidad.** Un documento `pendiente` o `rechazado` lleva `expiraEn = creadoEn + 7 días` (decisión D2, 2026-11-20) y se elimina solo con el índice TTL (`expireAfterSeconds: 0`). Un documento `listo` **no caduca**: al pasar a `listo` su `expiraEn` se fija a `9999-12-31T00:00:00Z` (mismo mecanismo que la spec 032 R-19, porque la documentación de Azure DocumentDB no describe qué pasa con documentos sin el campo). El TTL es limpieza, no regla: el código nunca da por vigente un `pendiente` solo por existir (R-09).
- **R-08:** **Migración.** Un migrador interno lee cada JSON de `registros-temporales` (forma antigua en `snake_case`, un blob `{id:N}.json` por archivo), lo traduce a la forma de §6 y lo inserta con `AddAsync`. Es **idempotente por `archivoId`**: si el índice único responde que ya existe, cuenta el registro como «ya migrado» y no lo toca (nunca sobrescribe un documento que pudo avanzar de estado). Se puede ejecutar varias veces o reanudar tras un corte sin duplicar ni perder registros. Reglas por estado: `listo` y `rechazado` vigentes se migran; `pendiente` **vigente** (dentro de su ventana, R-09) se migra; `pendiente` **caducado** no se migra y se borra su blob de `cuarentena` si existe. Un JSON ilegible o con un valor desconocido no detiene la migración: se cuenta como fallido, se reporta por su `archivoId` (nombre del blob) y el contenedor no se borra.
- **R-09:** **Tope de subidas pendientes.** Una persona puede tener como máximo **10** archivos `pendiente` vigentes al mismo tiempo (igual al de la spec 032 R-14; decisión D1, 2026-11-20). Es «vigente» el `pendiente` con `confirmarAntesDe` en el futuro; `confirmarAntesDe = creadoEn + 30 minutos` (igual que la spec 032 R-05; decisión D2, 2026-11-20). Los archivos `listo`, `rechazado` o con `confirmarAntesDe` vencido no cuentan. El tope cuenta **todos** los usos del flujo privado de la persona.
- **R-10:** **Conteo seguro con concurrencia (insertar y verificar).** Crear un archivo pendiente es: (1) insertar el documento `pendiente`; (2) contar los `pendiente` vigentes de esa persona, incluido el recién insertado; (3) si el conteo supera el tope, borrar el propio documento con una condición (`_id` y `estado: pendiente`) y responder que se alcanzó el tope; si no, continuar. Con peticiones simultáneas puede rechazarse alguna de más (si todas cuentan a la vez), pero **nunca quedan más pendientes que el tope**: el último documento insertado ve a todos los demás y se retira si hay exceso. La cuenta sale de MongoDB (no de Redis ni de memoria) para que sea la misma entre réplicas. La alternativa de contador por persona o de «ranuras» con índice parcial único se descarta en §13.
- **R-11:** Al alcanzar el tope, `MediaService.CreateUploadAsync` devuelve un resultado nuevo `LimitExceeded` y la capa HTTP que se defina para el flujo privado lo traduce a **429** con `code: limit_exceeded` y `Retry-After` en segundos (hasta que venza el pendiente vigente más antiguo, entre 1 y 1 800). No se crea un código nuevo: `limit_exceeded` ya existe en la spec 032 y el 429 está definido en la spec 002 R-11 (decisión D3, 2026-11-20). No se guarda nada cuando se rechaza (el documento insertado en R-10 se borra) y el cuerpo del error no repite valores del cliente.
- **R-12:** Los registros de la aplicación y las métricas de este cambio contienen solo códigos, conteos y duraciones: nunca el `sub`, el `propietarioId`, el `blobKey`, nombres de blob ni el contenido del documento. Contadores: archivos creados, rechazados por tope, migrados, ya migrados, caducados descartados y fallidos de migración; duración de la migración.
- **R-13:** El documento no contiene datos personales ni cifrados: ni nombre de archivo del cliente, ni metadatos de la imagen, ni coordenadas, ni direcciones ni SAS. `propietarioId` es el `sub` interno (no es celular, dirección ni documento de identidad, spec 009 R-07). Por eso `archivos` no lleva cifrado de campos.
- **R-14:** El contrato HTTP no cambia: ninguna ruta, cuerpo, respuesta ni código existente se modifica, y `docs/openapi.v1.json` regenerado con `scripts/export_openapi.py` no tiene diferencias por este cambio, **salvo** la descripción del campo `fotos` de `POST /api/v1/solicitudes`, que pasa de «24 hexadecimales» a «32 hexadecimales» por la decisión D4 (R-18). El único efecto nuevo visible es el 429 `limit_exceeded` de R-11, que se documenta junto con las rutas privadas cuando existan (§5 y decisión D3).
- **R-18:** **Formato de `archivoId` (decisión D4, 2026-11-20).** El identificador público de un archivo es el UUID v7 de 32 hexadecimales en minúscula que ya usa el código. La spec 020 se enmienda en este mismo PR (R-04: de «24 caracteres hexadecimales» a «32 hexadecimales, UUID v7 sin guiones»). En la implementación se ajustan el validador de `fotos` de `POST /api/v1/solicitudes` (`CreateRequestBodyValidator`, hoy `^[0-9a-fA-F]{24}$`), el comentario del DTO y la descripción de `docs/openapi.v1.json`. Como la 020 aún no puede usar fotos (`UnavailableRequestPhotos`), no hay clientes ni datos que dependan de los 24 hexadecimales.
- **R-15:** Se retira `BlobMediaFileStore`, la opción `Media:RecordsContainer` (también de `appsettings.json`) y toda referencia a `registros-temporales` en el código. El contenedor se borra en Azure solo después de la verificación de la migración (R-16), y lo hace una persona con permiso.
- **R-16:** **Cuándo se borra el contenedor (decisión D5, 2026-11-20).** Una persona revisa primero el contenedor en Azure. Si no existe o tiene cero JSON, **no se ejecuta la migración** y solo se borra el contenedor (si existe). Si tiene JSON, se ejecuta el migrador y el borrado ocurre solo si (a) el migrador terminó con cero fallidos y el conteo de «migrados + ya migrados + caducados descartados» es igual al número de JSON del contenedor, y (b) una persona lo confirmó en el PR. El migrador y sus pruebas se implementan igual, como protección para el caso de que haya registros; que se ejecute depende de esa revisión (R-17).
- **R-17:** **Estado real de los datos.** En el repositorio no hay ninguna ruta HTTP que cree archivos de Media (no hay controller del módulo; `MediaService` solo se registra, `docs/openapi.v1.json` no lo expone y la spec 020 usa `UnavailableRequestPhotos`), y el contenedor `registros-temporales` solo se crea al usarse (`CreateIfNotExists`) y no está en Terraform. Por eso **se espera que el entorno de Azure de evaluación no tenga registros que migrar**. No hay en la documentación del repositorio un inventario de lo que hay en Azure: esto **no está verificado** y la migración se implementa como protección y solo se ejecuta si la revisión manual muestra registros (decisión D5, R-16).
- **R-19:** **Ciclo de vida de `cuarentena` (decisión D8, 2026-11-20).** Se agrega una regla de ciclo de vida de 1 día al contenedor `cuarentena` de la cuenta privada (como la de `imagenes-cuarentena` de la spec 032), para que un blob sin confirmar no sobreviva a su registro caducado. Es un cambio de infraestructura (`deploy/**`): lo aplica una persona en un PR aparte y no se ejecuta en este cambio. Como `cuarentena` y `archivos` hoy los crea el código al usarse, ese PR también decide si se declaran en Terraform.

## 4. Criterios de aceptación
- **CA-01:** Dado el script nuevo de `db/`, cuando se ejecuta dos veces sobre una base vacía, entonces existen la colección `archivos` y los índices `archivoId_1` (único), `propietarioId_1_estado_1_confirmarAntesDe_1` y `expiraEn_1` (TTL, `expireAfterSeconds: 0`), sin error y sin duplicados en la segunda ejecución.
- **CA-02:** Dado un `archivoId` ya existente, cuando se intenta guardar otro documento con el mismo, entonces el índice único lo rechaza y `AddAsync` lanza `InvalidOperationException` sin cambiar el documento original.
- **CA-03:** Dado un `MediaFile` guardado con `MongoMediaFileStore`, cuando se lee el documento crudo, entonces `_id` es `ObjectId`, todas las claves están en camelCase (ninguna lleva `_` salvo `_id`), `archivoId` y `blobKey` son 32 hexadecimales y al leerlo con `FindAsync` se obtiene un `MediaFile` igual al original.
- **CA-04:** Dado un documento `pendiente` y otro `rechazado` con `expiraEn` vencido y uno `listo` con `expiraEn = 9999-12-31`, cuando corre el TTL de la base, entonces los dos primeros desaparecen y el `listo` sigue existiendo.
- **CA-05:** Dadas dos confirmaciones simultáneas del mismo archivo `pendiente`, cuando las dos llaman a `TryReplaceAsync` con la misma versión, entonces una devuelve `true`, la otra `false` y el documento queda con una sola transición y `version` aumentada en 1.
- **CA-06:** Dado un archivo de otra persona o un `archivoId` inexistente, cuando se confirma o se consulta con `MediaService` e `IMediaApi`, entonces el resultado es el mismo `NotFound` (o `false`) en ambos casos.
- **CA-07:** Dada una persona con 10 pendientes vigentes, cuando pide una subida nueva, entonces recibe `LimitExceeded` (429 `limit_exceeded` con `Retry-After`), no queda ningún documento nuevo y los 10 existentes no cambian.
- **CA-08:** Dada una persona con 0 pendientes y 25 peticiones de subida simultáneas (dos réplicas simuladas), cuando terminan, entonces quedan como máximo 10 documentos `pendiente` vigentes de esa persona (nunca 11), todas las demás recibieron `LimitExceeded` y ninguna quedó a medias.
- **CA-09:** Dada una persona con 8 pendientes y 5 peticiones simultáneas, cuando terminan, entonces quedan como máximo 10 pendientes en total.
- **CA-10:** Dada una persona con 10 pendientes de los que uno se confirmó (`listo`), otro se rechazó y otro tiene `confirmarAntesDe` vencido, cuando pide una subida nueva, entonces se acepta; dada otra persona con 0 pendientes, el tope de la primera no la afecta.
- **CA-11:** Dado un archivo que pasa a `listo` con el almacenamiento de Azurite, cuando se inspeccionan los blobs, entonces los nombres son `cuarentena/{blobKey}` (borrado al confirmar), `archivos/{blobKey}/original.jpg` y `archivos/{blobKey}/reducida.jpg`, igual que antes del cambio.
- **CA-12:** Dado un contenedor `registros-temporales` con JSON de un archivo `listo`, uno `rechazado` vigente, un `pendiente` vigente y un `pendiente` caducado, cuando corre el migrador, entonces `archivos` tiene los tres primeros con sus campos en camelCase y los mismos `archivoId`, `blobKey` y `propietarioId`; el `pendiente` caducado no se migra y su blob de `cuarentena` se borra.
- **CA-13:** Dado el mismo contenedor, cuando el migrador corre dos veces seguidas (y una vez interrumpido a la mitad y reanudado), entonces el número de documentos no cambia, no hay duplicados y ningún documento ya migrado se sobrescribe.
- **CA-14:** Dado un contenedor inexistente o vacío, cuando corre el migrador, entonces termina con éxito, migra 0 y no crea el contenedor.
- **CA-15:** Dado un JSON ilegible o con un `estado` o `uso` desconocido, cuando corre el migrador, entonces migra el resto, cuenta un fallido, lo reporta por el nombre del blob sin su contenido y no marca el contenedor como listo para borrarse (R-16).
- **CA-16:** Dado el código fuente, cuando se busca `BlobMediaFileStore`, `RecordsContainer` y `registros-temporales` en `src/`, `deploy/` y `appsettings*.json`, entonces no hay ninguna aparición (las menciones históricas quedan solo en `specs/_legacy/` y `Memoria*.md`), y el único `IMediaFileStore` registrado es `MongoMediaFileStore`.
- **CA-17:** Dada una petición de subida cuyo cuerpo incluye un campo `propietarioId` (o `sub`) distinto del token, cuando se procesa, entonces el archivo se guarda con el `sub` del token y el campo del cuerpo se ignora; el DTO de la petición no declara ningún campo de dueño.
- **CA-18:** Dados 1 000 `archivoId` generados por el servidor, cuando se inspeccionan, entonces son UUID v7 de 32 hexadecimales y distintos entre sí; dado un `archivoId` adivinado de otra persona, la respuesta es la misma que para uno inexistente.
- **CA-19:** Dados los registros y las métricas de una creación, un rechazo por tope y una migración, cuando se inspeccionan, entonces no contienen el `sub`, el `blobKey`, nombres de blob ni el contenido de un documento.
- **CA-20:** Dada la consulta que cuenta los pendientes vigentes de una persona, cuando se ejecuta con `explain`, entonces usa el índice compuesto (sin recorrido completo de la colección).
- **CA-21:** Dados los endpoints y el OpenAPI de Mercado, cuando se regenera `docs/openapi.v1.json`, entonces la única diferencia es la descripción del campo `fotos` de `POST /api/v1/solicitudes` (32 hexadecimales, R-18), y las rutas, cuerpos y códigos existentes no cambian; la prueba de contrato lo verifica.
- **CA-22:** Dado el PR de implementación, cuando se revisa, entonces `docs/endpoints.md` (o el documento del flujo privado), `docs/architecture/CODEMAP.md`, `Memoria.md` y la spec 058 están actualizados y `python scripts/sdd_lint.py --trace 058` no reporta faltantes.
- **CA-23:** Dado el validador de `fotos` de `POST /api/v1/solicitudes`, cuando recibe un `archivoId` de 32 hexadecimales (UUID v7) entonces lo acepta en el formato, y cuando recibe uno de 24 hexadecimales entonces responde 422 `errors.fotos`; la spec 020 (R-04) dice 32. (R-18)

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec058ArchivosScriptTests.Script_Crea_Coleccion_E_Indices_Idempotente_CA_058_01` |
| CA-02 | `Spec058ArchivosStoreTests.Archivo_Id_Duplicado_Se_Rechaza_CA_058_02` |
| CA-03 | `Spec058ArchivosStoreTests.Documento_En_CamelCase_Con_ObjectId_CA_058_03` |
| CA-04 | `Spec058ArchivosScriptTests.Ttl_Borra_Pendientes_Y_Rechazados_No_Listos_CA_058_04` |
| CA-05 | `Spec058ArchivosStoreTests.Reemplazo_Condicionado_Una_Sola_Gana_CA_058_05` |
| CA-06 | `Spec058ArchivosSeguridadTests.Archivo_Ajeno_O_Inexistente_Mismo_Resultado_CA_058_06` |
| CA-07 | `Spec058TopePendientesTests.Onceava_Subida_Se_Rechaza_Sin_Guardar_CA_058_07` |
| CA-08 | `Spec058TopePendientesTests.Veinticinco_Subidas_Simultaneas_No_Superan_El_Tope_CA_058_08` |
| CA-09 | `Spec058TopePendientesTests.Ocho_Mas_Cinco_Simultaneas_No_Superan_El_Tope_CA_058_09` |
| CA-10 | `Spec058TopePendientesTests.Solo_Cuentan_Pendientes_Vigentes_De_La_Persona_CA_058_10` |
| CA-11 | `Spec058NombresDeBlobTests.Nombres_De_Blob_No_Cambian_CA_058_11` |
| CA-12 | `Spec058MigracionTests.Migra_Listo_Rechazado_Y_Pendiente_Vigente_CA_058_12` |
| CA-13 | `Spec058MigracionTests.Migracion_Es_Idempotente_Y_Reanudable_CA_058_13` |
| CA-14 | `Spec058MigracionTests.Contenedor_Vacio_O_Inexistente_CA_058_14` |
| CA-15 | `Spec058MigracionTests.Registro_Invalido_Cuenta_Como_Fallido_CA_058_15` |
| CA-16 | `Spec058RetiroTests.Sin_Blob_Store_Ni_Registros_Temporales_CA_058_16` (ArchitectureTest) y (comando) búsqueda de las tres cadenas |
| CA-17 | `Spec058ArchivosSeguridadTests.Propietario_Solo_Del_Token_CA_058_17` |
| CA-18 | `Spec058ArchivosSeguridadTests.Archivo_Id_Uuid_V7_No_Enumerable_CA_058_18` |
| CA-19 | `Spec058ArchivosSeguridadTests.Registros_Y_Metricas_Sin_Datos_CA_058_19` |
| CA-20 | `Spec058ArchivosScriptTests.Conteo_Usa_Indice_Compuesto_CA_058_20` |
| CA-21 | `Spec058ContractTests.Contrato_Http_Sin_Cambios_CA_058_21` (ContractTest) |
| CA-22 | (comando) `python scripts/sdd_lint.py --trace 058`, `python scripts/codemap.py --check` y revisión del PR |
| CA-23 | `Spec058ContractTests.Fotos_De_Solicitud_Acepta_32_Hexadecimales_CA_058_23` (ContractTest) y regresión de `Spec020FotosTests` |

## 5. Contrato
**HTTP: sin cambios.** El módulo `Media` de Mercado hoy no tiene controller: la spec 013 OI-2 dejó solo servicios detrás de interfaces (`Memoria.md`, 2026-11-09) y `docs/openapi.v1.json` no contiene rutas de archivos privados (solo el campo `fotos` de la solicitud, que la 020 pasa a 32 hexadecimales por D4). La spec 032 define las rutas del flujo **público** (`/api/v1/imagenes/subidas`); las del flujo privado todavía no existen en ninguna spec aprobada. Por eso «no cambiar el contrato HTTP» significa: esta tarjeta no agrega, quita ni modifica ninguna ruta, cuerpo ni nombre de blob; lo único que cambia es el formato aceptado de cada `archivoId` en `fotos` de la solicitud (R-18).

**Efecto nuevo documentable:** cuando existan las rutas del flujo privado, pedir una subida puede responder **429** `limit_exceeded` con `Retry-After` (R-11). 429 es un estado estándar de la spec 002 (R-11) y el código `limit_exceeded` ya existe (spec 032). Se decidió no crear un código propio (D3, 2026-11-20).

**Interfaz interna (no REST), `IMediaFileStore` (camelCase en el documento):**

| Método | Comportamiento |
|---|---|
| `AddAsync(MediaFile)` | inserta; `InvalidOperationException` si el `archivoId` ya existe |
| `TryAddPendingWithinLimitAsync(MediaFile, int maxPending, DateTimeOffset now)` (nuevo) | R-10: inserta, cuenta y se retira si hay exceso; `false` si se alcanzó el tope |
| `FindAsync(Guid)` | devuelve `StoredMediaFile(MediaFile, Version)` o `null` |
| `TryReplaceAsync(MediaFile, expectedVersion)` | reemplazo condicionado por `archivoId` y `version`; `false` si otra petición ganó |

`IMediaApi.IsReadyAndOwnedByAsync(Guid fileId, string ownerId, CancellationToken)` (spec 020) no cambia.

## 6. Modelo de datos
**Colección `archivos` (base `market`)**

| Campo | Tipo | Regla |
|---|---|---|
| `_id` | `ObjectId` | generado al insertar; no sale de la API |
| `archivoId` | string | UUID v7, 32 hexadecimales en minúscula (el `MediaFile.Id` de hoy); identificador de negocio; índice único |
| `blobKey` | string | 32 hexadecimales aleatorios (128 bits), independiente de `archivoId`; nombre del blob de cuarentena |
| `propietarioId` | string | `sub` del JWT de quien subió; nunca viene del cliente |
| `uso` | string | `perfil`, `solicitud` o `trabajo` (los valores que acepta hoy `MediaRules.ParseUsage`; ver §7) |
| `estado` | string | `pendiente` → `listo` o `rechazado` |
| `contentType` | string | `image/jpeg`, `image/png` o `image/webp` (el declarado) |
| `tamanoBytes` | long | 1 a 5 242 880 (el declarado) |
| `blobOriginal`, `blobReducido` | string? | solo en `listo`: `{blobKey}/original.jpg` y `{blobKey}/reducida.jpg` |
| `ancho`, `alto` | int? | solo en `listo` |
| `creadoEn` | fecha UTC | creación |
| `confirmarAntesDe` | fecha UTC | `creadoEn + 30 min` (D2) |
| `confirmadoEn` | fecha UTC? | al pasar a `listo` o `rechazado` |
| `expiraEn` | fecha UTC | `creadoEn + 7 días` en `pendiente` y `rechazado` (D2); `9999-12-31T00:00:00Z` en `listo` |
| `version` | int | empieza en 1; sube en 1 en cada reemplazo; concurrencia optimista (spec 003 R-03) |

**Índices** (script nuevo de `db/`; su número es «el siguiente libre en `db/` al implementar», porque en `db/` hay números repetidos entre servicios y otras ramas pueden tomar el siguiente; nombre sugerido `NNN-market-archivos.js`)

| Índice | Opciones | Motivo |
|---|---|---|
| `{ archivoId: 1 }` | único | buscar por el identificador de negocio y garantizar que no haya dos registros del mismo archivo; es lo que hace idempotente la migración (R-08) y evita duplicados por reintentos |
| `{ propietarioId: 1, estado: 1, confirmarAntesDe: 1 }` | normal | conteo de pendientes vigentes por persona (R-09, R-10), listados «de una persona» y «de una persona en un estado»; su prefijo `propietarioId` cubre la consulta por dueño |
| `{ expiraEn: 1 }` | TTL, `expireAfterSeconds: 0` | borra solo los pendientes y rechazados vencidos (R-07); el mismo mecanismo que `sesiones.venceEn` y `subidas_imagen.expiraEn` |

La tarjeta pide índices para `propietarioId` y `estado`. Esta spec los cubre con **un solo índice compuesto** en lugar de dos sueltos (el de `estado` solo tiene tres valores y ninguna consulta lo usa sin el dueño); decisión D7, 2026-11-20.

**Qué caduca y qué no:** caducan `pendiente` y `rechazado` (7 días, D2); no caduca `listo`. El TTL de DocumentDB corre en segundo plano y puede tardar: por eso el código nunca confía en él para decidir si un pendiente es vigente (R-09 usa `confirmarAntesDe`).

**Documento antiguo (origen de la migración):** JSON por archivo en `registros-temporales/{id:N}.json`, con claves en `snake_case` minúscula (`id`, `blob_key`, `propietario_id`, `uso`, `estado`, `content_type`, `tamano_bytes`, `blob_original`, `blob_reducido`, `ancho`, `alto`, `creado_en`, `confirmado_en`). Traducción: `id` → `archivoId`; cada clave pasa a camelCase; `confirmarAntesDe = creado_en + 30 min`; `expiraEn` según el estado (R-07); `version = 1`.

**Invariantes:** nunca hay más de 10 `pendiente` vigentes por persona; un `archivoId` no se repite; un `listo` nunca caduca ni vuelve a `pendiente`; los blobs conservan sus nombres.

## 7. Arquitectura y relación con la spec 032
- Se agrega `Data/MongoMediaFileStore.cs` en `src/Knock.Market.Api/Modules/Media/`; usa el cliente de MongoDB y la base `market` que ya usan los demás módulos de Mercado (la conexión sale de la configuración y de Key Vault, nunca del código ni de un archivo del repositorio). El módulo sigue exponiendo solo `AddMediaModule` e `IMediaApi`; el resto es `internal`. `MediaService` solo cambia en `CreateUploadAsync` (R-09 a R-11) y en el mapa de resultados (`LimitExceeded`).
- La migración vive en una clase interna del módulo, sin tarea programada ni arranque automático obligatorio: se invoca de forma explícita (comando o bandera de configuración) y la corre una persona en Azure si la revisión previa (R-17) muestra registros.
- **Relación explícita con la spec 032 (no se mezclan):**

| | `archivos` (esta spec, flujo **privado** de la 013) | `subidas_imagen` (spec 032, flujo **público**) |
|---|---|---|
| Para qué | fotos privadas (hoy: fotos de una solicitud, spec 020) | imágenes públicas (foto de perfil y fotos de un trabajo terminado) |
| Dueño | `propietarioId` | `sub` |
| Identificador | `_id` ObjectId + `archivoId` UUID v7 | `_id` string UUID v7 |
| Estados | `pendiente`, `listo`, `rechazado` | `pendiente`, `publicada`, `rechazada`, `invalida` |
| Cuenta de blobs | privada: `cuarentena` y `archivos`, siempre con SAS de 5 minutos | cuarentena privada (`imagenes-cuarentena`) y publicación en la cuenta pública (`imagenes-publicas`) |
| Qué devuelve | `archivoId`; nunca una dirección pública | una dirección pública `url` |
| Tope de pendientes | 10 (esta spec, MongoDB) | 10 (spec 032 R-14, índice por `sub`) |
| Caducidad | `pendiente` y `rechazado` a 7 días; `listo` no | `pendiente`, `rechazada` e `invalida` a 7 días; `publicada` no |

  **No se mezcla:** las dos colecciones, sus estados, sus contenedores de blob, sus códigos de estado y sus nombres de campo siguen separados; una imagen pública nunca se registra en `archivos` y una foto privada nunca en `subidas_imagen`; ningún módulo lee `archivos` fuera de `IMediaApi`; el tope de una no cuenta pendientes de la otra. **Decisión D6 (2026-11-20):** se mantienen separadas; si más adelante conviene una sola colección con un campo de visibilidad, queda para una spec propia después de implementar la 032 y la 020.
- **Por qué no se reutiliza `subidas_imagen` ahora:** la spec 032 está aprobada y en implementación con sus propios nombres; cambiar su colección rompería su script `db/044` y sus casos, y la 013 exige que lo privado y lo público no compartan el camino.
- Caché (constitución, principio 9): `archivos` no se cachea, es dato que cambia en cada confirmación y se lee por `archivoId` con proyección.
- Observabilidad: contadores de R-12 con el estilo de la spec 011.
- Terraform: `registros-temporales` no está en Terraform (se crea desde el código al usarse); retirar el código ya deja de crearlo y quita la opción. Si al implementar se encuentra algún recurso de Terraform que lo declare, se retira en el mismo PR sin ejecutarlo. Los contenedores `cuarentena` y `archivos` tampoco están en Terraform hoy (el módulo compartido declara `imagenes`, `respaldos` e `imagenes-cuarentena`); la regla de ciclo de vida de `cuarentena` se agrega por R-19 (D8) y declarar los contenedores en Terraform lo decide el mismo PR de `deploy/**`.

## 8. Dependencias
- **Base:** specs 002 (estructura, 429 y códigos), 003 (MongoDB, camelCase, ObjectId, `version`, scripts idempotentes), 009 (el `sub` del JWT y datos sensibles), 011 (registros) y **013** (flujo privado). Se coordina con la **032** (flujo público) y la **020** (consume `IMediaApi`).
- **Código existente:** `MediaService`, `MediaFile`, `IMediaFileStore`, `BlobMediaFileStore`, `InMemoryMediaFileStore`, `MediaRules`, `ImageProcessor`, `IMediaApi`, `MediaOptions` y las pruebas `MediaServiceTests` y `MediaConfirmationBlobTests`.
- **Pruebas:** Testcontainers con una imagen compatible con la API de MongoDB (la que ya usa `tests/DatabaseTest`) y Azurite (ya usado por `MediaConfirmationBlobTests`).
- **Sin dependencias nuevas** de NuGet: el cliente de MongoDB ya está en Mercado.
- **Spec 020 (Aprobada), enmendada por D4:** su R-04 decía «24 caracteres hexadecimales» (parecía un `ObjectId`), pero el código de Media y la tarjeta usan un UUID v7 (32 hexadecimales). Se enmendó a 32 en este mismo PR (R-18).

## 9. Amenazas y controles
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Subir sin parar para llenar el almacenamiento y la base (abuso) | Tope de 10 pendientes vigentes por persona, caducidad de pendientes y de su blob de cuarentena | CA-07, CA-08 |
| Dos subidas a la vez que se saltan el tope (condición de carrera) | Insertar y verificar con conteo en MongoDB; nunca quedan más del tope aunque se rechace alguna de más | CA-08, CA-09 |
| Una persona pone el dueño de otra (suplantar el `propietarioId`) | El dueño sale solo del `sub` del token; el DTO no tiene campo de dueño | CA-17 |
| Leer o confirmar archivos ajenos adivinando el `archivoId` (BOLA/IDOR) | UUID v7 generado por el servidor, `blobKey` aparte de 128 bits, 404 uniforme para ajeno o inexistente | CA-06, CA-18 |
| Duplicar un archivo por reintento o por migración repetida | Índice único por `archivoId` y migración que nunca sobrescribe | CA-02, CA-13 |
| Dos confirmaciones simultáneas que dejan el registro inconsistente | Reemplazo condicionado por `version` | CA-05 |
| Perder registros al migrar | Migración por `archivoId`, reanudable, con conteos y sin borrar el contenedor si hubo fallidos | CA-12, CA-13, CA-15 |
| Fuga de datos en registros o métricas | Solo códigos, conteos y duraciones; sin `sub`, `blobKey` ni nombres de blob | CA-19 |
| Datos personales en el documento (ubicación de la foto, nombre de archivo) | El documento no guarda metadatos de la imagen ni nombre del cliente; los bytes ya van limpios (spec 013) | CA-03 |
| Dejar vivo el contenedor temporal y su lectura con una llave | Se retira el código y el contenedor; no queda ruta de lectura de registros en Blob | CA-16 |
| Consulta del conteo lenta bajo carga | Índice compuesto verificado con `explain` | CA-20 |
| Cambio accidental del contrato HTTP | Prueba de contrato y OpenAPI sin diferencias | CA-21 |

## 10. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Unitarias | Sí | Mapeo del documento, cálculo de `expiraEn` y `confirmarAntesDe` con `TimeProvider` falso, regla de vigencia y `LimitExceeded` con `InMemoryMediaFileStore` |
| Base de datos (`DatabaseTest`) | Sí | Script, índices, único, TTL, `explain` y el conteo con MongoDB real en contenedor |
| Integración | Sí | `MongoMediaFileStore` con Azurite y MongoDB; migración desde JSON reales en Azurite; `MediaConfirmationBlobTests` sigue en verde con el nuevo almacén |
| Concurrencia | Sí | 25 subidas simultáneas y 8 + 5 (CA-08, CA-09) repetidas varias veces para que una condición de carrera aparezca; dos confirmaciones simultáneas (CA-05) |
| Contrato (`ContractTest`) | Sí | OpenAPI sin cambios y nombres de blob iguales (CA-11, CA-21) |
| Seguridad negativa | Sí | Dueño solo del token, archivo ajeno, `archivoId` no enumerable y registros sin datos (CA-06, CA-17 a CA-19) |
| Arquitectura | Sí | Sin `BlobMediaFileStore` ni `registros-temporales`; solo `MongoMediaFileStore` registrado; el controller (cuando exista) no toca MongoDB (CA-16) |
| Mutación | Sí | El conteo y la comparación contra el tope, la vigencia y el reemplazo condicionado (meta de la spec 008) |
| Infraestructura | No | No hay Terraform nuevo; si se encuentra un recurso del contenedor, se retira sin ejecutar |
| Rendimiento | No | Se cubre con `explain` de CA-20; sin volumen que justifique una prueba de carga |
| Humo en Azure | Parcial | Solo la revisión manual de si hay registros en el contenedor (R-17) y, si los hay, la migración; la ejecuta una persona |

## 11. Escenarios
**Positivos**
- Una persona pide la subida de una foto de su solicitud: se crea el registro `pendiente`, sube, confirma y queda `listo`; el contrato y los blobs son los de siempre.
- Dos personas suben a la vez: cada una tiene su propio tope.
- Se reintenta la migración después de un corte: no hay duplicados.

**Negativos**
- Alguien con 10 pendientes pide otra subida: 429 `limit_exceeded` con `Retry-After`.
- Un script lanza 25 subidas en paralelo: quedan a lo más 10.
- Alguien intenta confirmar un `archivoId` de otra persona: 404 como si no existiera.
- Un JSON antiguo está corrupto: la migración continúa y lo reporta.

**Casos límite**
- Exactamente 9, 10 y 11 pendientes; un pendiente que vence justo al pedir otra subida.
- Un pendiente cuyo TTL aún no corrió pero con `confirmarAntesDe` vencido: no cuenta.
- El migrador encuentra un `archivoId` que ya existe con otro estado: no lo toca.
- El contenedor `registros-temporales` no existe: la migración termina sin error.
- Reloj de una réplica desfasado unos segundos: el conteo usa la hora del servidor de aplicación vía `TimeProvider`; el peor caso es contar un pendiente unos segundos de más o de menos.

## 12. Definición de terminado
- CA-01 a CA-23 con prueba o comando en verde y la tabla 4.1 igual a los nombres reales; `python scripts/sdd_lint.py --trace 058` sin faltantes.
- Los registros viven en MongoDB, `BlobMediaFileStore` y `Media:RecordsContainer` están retirados, y el contenedor `registros-temporales` está borrado en Azure por una persona tras la verificación de R-16 (o confirmado que no existía); la regla de ciclo de vida de `cuarentena` (R-19) está aplicada por una persona.
- Contrato HTTP sin cambios (CA-21), pruebas de índices (CA-01, CA-04, CA-20), de migración (CA-12 a CA-15) y del tope por usuario (CA-07 a CA-10) en verde.
- `Memoria.md` con la decisión, el porqué y qué evitar; `docs/architecture/CODEMAP.md` regenerado con `python scripts/codemap.py`; la spec 020 enmendada (R-18, hecho en el PR de esta spec) y su validador, DTO y OpenAPI ajustados a 32 hexadecimales.
- PR con «Uso de IA», etiqueta de puntos y revisión de par.

## 13. Decisiones
- **Colección propia para lo privado, separada de `subidas_imagen`:** la 013 exige que lo público y lo privado no compartan camino; cada flujo tiene estados, dueño y caducidad distintos.
- **`_id` ObjectId y `archivoId` UUID v7:** la spec 003 pide `ObjectId`; el UUID v7 ya existe en el código, en los nombres internos y en la spec 020, y no se puede adivinar ni revela la hora de forma útil.
- **Insertar y verificar para el tope:** funciona sin contador aparte, sin transacciones y sin depender de índices parciales; el costo es rechazar de más en choques simultáneos, aceptable porque la persona reintenta con `Retry-After`. **Alternativas descartadas:** (a) contar y luego insertar (no es seguro con peticiones simultáneas); (b) un documento contador por persona con `$inc` condicionado (se desfasa cuando un pendiente caduca por TTL y necesita reparación); (c) «ranuras» numeradas con índice único parcial por persona (es atómico y se libera solo, pero depende de que Azure DocumentDB soporte índices parciales, que no se verificó: puede evaluarse en T-01 como mejora).
- **Contar en MongoDB y no en Redis:** la cuenta debe coincidir con los registros reales y sobrevivir reinicios; Redis queda para los límites por hora de la spec 032.
- **Un índice compuesto en lugar de dos sueltos.**
- **La migración es explícita y reanudable, no automática:** correr algo que escribe datos al arrancar el servicio en todas las réplicas es más riesgoso que una ejecución controlada, y se espera que no haya datos (R-17).
- **No sobrescribir en la migración:** un documento que ya existe puede haber avanzado de estado; el índice único decide.

## 14. Decisiones del Tech Lead (contestadas el 2026-11-20)
Las ocho se contestaron «sí» con la recomendación del autor y ya están aplicadas en el texto de la spec. No queda ninguna decisión abierta.

| N.º | Decisión (sí, con la recomendación) | Dónde se aplicó |
|---|---|---|
| D1 | **El tope es de 10 subidas pendientes vigentes por persona**, igual que el de la spec 032 (una solicitud admite hasta 5 fotos, spec 020, con margen para reintentos). | R-09, R-10, CA-07 a CA-10 |
| D2 | **Una subida privada se confirma antes de 30 minutos y, si queda pendiente o rechazada, su registro se borra solo a los 7 días**, como en las specs 013 (R-08) y 032. | R-07, R-09, §6 |
| D3 | **El 429 usa el código que ya existe, `limit_exceeded`, con `Retry-After`**, sin crear un código nuevo. | R-11, §5 |
| D4 | **`archivoId` es el UUID v7 de 32 hexadecimales que ya usa el código, y se enmienda la spec 020 (R-04)** para que coincida; la 020 aún no puede usar fotos, así que no rompe nada en uso. | R-18, CA-23, spec 020 R-04 |
| D5 | **Si la revisión manual del contenedor `registros-temporales` en Azure muestra cero JSON, no se ejecuta la migración y solo se borra el contenedor**; si hay registros, se ejecuta el migrador y se borra tras verificar. | R-16, R-17 |
| D6 | **`archivos` y `subidas_imagen` siguen separadas**; la unificación se estudia en una spec propia después de implementar la 032 y la 020. | §7 |
| D7 | **Un solo índice compuesto** (`propietarioId`, `estado`, `confirmarAntesDe`) en lugar de dos sueltos. | R-03, §6 |
| D8 | **Se agrega una regla de ciclo de vida de 1 día al contenedor `cuarentena`** de la cuenta privada; es un cambio de infraestructura que aplica una persona y no se ejecuta aquí. | R-19 |

## 15. Riesgos
- **No verificado: datos reales en Azure.** La documentación del repositorio no inventaría lo que hay en el contenedor `registros-temporales` del entorno de evaluación. La conclusión de «no hay datos» (R-17) sale de que no existe ruta HTTP que cree archivos, no de haberlo revisado.
- **No verificado: soporte de DocumentDB.** El comportamiento exacto del TTL (frecuencia, documentos sin el campo), de `explain` y de índices parciales en Azure DocumentDB se confirma con el contenedor de pruebas y, si es posible, en Azure; por eso `expiraEn` se fija en 9999 en lugar de quitarse y el conteo usa `confirmarAntesDe` en lugar del TTL.
- **Rechazos de más bajo contención simultánea:** el tope puede rechazar una subida legítima cuando muchas llegan a la vez; es seguro y reintentable, y poco probable con un solo teléfono por persona.
- **Blobs de cuarentena huérfanos:** el TTL borra el registro pero no el blob; hasta que una persona aplique la regla de ciclo de vida de 1 día (R-19, D8) puede quedar un blob sin registro.
- **Dependencia con la spec 020:** el formato de `archivoId` quedó alineado en las dos specs (D4); el validador y el OpenAPI de la 020 cambian al implementar (R-18, T-08).
- **Las rutas HTTP del flujo privado todavía no están definidas** en ninguna spec aprobada; el tope y su 429 se documentan cuando existan.
- **`uso` acepta hoy `perfil`, `solicitud` y `trabajo` en el flujo privado**, aunque la spec 032 manda perfil y trabajo al flujo público; restringirlo a `solicitud` no se hace aquí.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-20
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Implementada (tras el merge)
