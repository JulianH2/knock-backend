# Spec 020 — Solicitudes y cotizaciones (pedir y enviar)

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Claves de trabajo (verificadas en Jira el 2026-11-09):** KNOCK-59 «HU-024 · Pedir una cotización (categoría, descripción, 1 a 5 fotos, día y franja)» y KNOCK-74 «HU-031 · Enviar una cotización (precio fijo o rango, día y hora, nota)»; datos de demo en KNOCK-246 «Datos de demo: solicitudes y cotizaciones ficticias en las semillas»
> **Responsables sugeridos:** Guillermo (HU-024, solicitudes) · Alexis (HU-031, cotizaciones; KNOCK-246, semillas) · **Ola:** A · **Sprint:** 4 · **Prioridad:** P1
> **Servicio:** Mercado · **Módulo:** Requests · **Colecciones:** `solicitudes`, `cotizaciones`

## 1. Objetivo
Que una persona describa su problema con fotos y reciba cotizaciones de técnicos, y que un técnico pueda responder con un precio, un día y una hora. Es el primer eslabón de la cadena de la demo: sin solicitud no hay cotización, comparación, elección ni visita.

## 2. Historia y alcance
**2.1 Historia**
- **HU-024** — Como cliente, quiero describir mi problema con fotos y pedir cotizaciones, para recibir varias opciones sin llamar a nadie.
- **HU-031** — Como técnico, quiero enviar una cotización con precio fijo o rango, día y hora, para ofrecer mi trabajo con claridad.

**2.2 Dentro**
- Crear una solicitud con oficio, descripción, de 1 a 5 fotos privadas, día, franja y lugar.
- Ver el detalle de una solicitud propia (cliente) y la vista acotada del técnico.
- Listar las solicitudes propias con paginación por cursor.
- Enviar una cotización con precio fijo, rango o visita previa, día, hora y nota, con tope calculado en el servidor.
- Calcular y guardar el tope de cada cotización y si requiere aprobación.
- Publicar los eventos `SolicitudCreada` y `CotizacionEnviada` en el buzón de salida (`outbox`).

**2.3 Fuera**
- Repartir la solicitud por oleadas y elegir a qué técnicos llega (HU-025, otra spec).
- Vencer la solicitud automáticamente a las 48 h (HU-028): aquí solo se guarda `venceEn` y se rechazan acciones sobre una solicitud vencida.
- Comparar y elegir cotización (spec 021), visita y código (spec 022).
- Editar o retirar una cotización (HU-035).
- Asistente con IA y borrador de cotización (spec 024 y HU-059).
- Subir, confirmar y moderar las fotos: se hace con el flujo de archivos privados de TT-API-05 (KNOCK-42) y la spec 013 (`POST /api/v1/archivos/subidas` y `POST /api/v1/archivos/{id}/confirmar`, `uso: "solicitud"`); aquí solo se guardan los identificadores de archivos ya confirmados.
- App del técnico y pantalla de solicitudes nuevas.

## 3. Requisitos
- **R-01:** `POST /api/v1/solicitudes` exige JWT de persona y cuerpo `{ oficioClave, descripcion, fotos, dia, franja, coloniaId, direccion? }`. Responde 201 con la solicitud creada en estado `enviada` y `venceEn` igual a `creadoEn + 48 h`.
- **R-02:** `oficioClave` debe existir y estar activo en el catálogo de oficios; la `modalidad` (`domicilio` o `taller`) la dicta el oficio y el cliente no puede cambiarla. `coloniaId` debe existir y estar activa en el catálogo de colonias.
- **R-03:** `descripcion` tiene de 8 a 500 caracteres y pasa por el cliente de moderación antes de guardarse (spec 013). Si la moderación marca el texto, responde 422 `content_rejected`; si no responde, 503 `moderation_unavailable` y no se guarda nada.
- **R-04:** `fotos` es un arreglo de 1 a 5 identificadores de archivo (`archivoId`, cadena de 24 caracteres hexadecimales), sin repetidos. Cada uno debe existir en el módulo de archivos, ser del `sub` del token, tener `uso = solicitud` y estar en estado `listo` (confirmado, sin metadatos y moderado); si no, responde 422 `photo_invalid` sin decir cuál condición falló y no guarda nada. Las fotos viven en el contenedor **privado** (TT-API-05): nunca se aceptan ni se devuelven direcciones públicas; las vistas devuelven `fotos: [{ id, url, venceEn }]` con enlaces de lectura de 5 minutos (versión reducida) emitidos por `IMediaApi` solo para quien tiene permiso según R-08.
- **R-05:** `dia` es una fecha `YYYY-MM-DD` en la zona `America/Mexico_City`, de hoy a hoy + 5 días. `franja` es `manana`, `tarde` o `noche`.
- **R-06:** Si la modalidad es `domicilio`, `direccion` es obligatoria (8 a 200 caracteres) y se guarda cifrada a nivel de campo (AES-256-GCM); si es `taller`, `direccion` no se acepta y su presencia responde 422 `address_not_applicable`. La dirección nunca se devuelve en listados ni en la vista del técnico antes de que sea elegido (spec 021).
- **R-07:** La creación de una solicitud no usa `Idempotency-Key` por ahora (ver §15): cada petición válida crea una solicitud y responde 201.
- **R-08:** `GET /api/v1/solicitudes/{id}` exige JWT. Responde la **vista del cliente** solo al cliente dueño (incluye la dirección descifrada). Responde la **vista del técnico** a un técnico activo que ya cotizó esa solicitud, o a un técnico que cumple la elegibilidad base (R-23) mientras esta sea cotizable (`enviada` o `cotizando`, no vencida), y el detalle la vuelve a verificar en cada petición; la vista del técnico trae oficio, colonia, descripción, fotos, día y franja, y **nunca** la dirección, el nombre ni el teléfono del cliente, ni siquiera después de ser elegido (después de elegir, el lugar se consulta en la visita, spec 022). Cualquier otra persona recibe 404 uniforme. Un `id` que no es `ObjectId` responde 400.
- **R-09:** `GET /api/v1/solicitudes` devuelve solo las solicitudes del `sub` del token, de la más reciente a la más antigua (`creadoEn` descendente y `_id` descendente como desempate), con paginación por cursor (spec 002): `limit` de 1 a 50 (20 por omisión), `cursor` opaco (Base64url de `creadoEn` y `_id` de la última fila) y filtro opcional `estado` con uno de los valores de §6. La respuesta es `{ items, nextCursor }`; `nextCursor` es `null` en la última página. `limit` o `estado` fuera de rango responde 422 con `errors`; un cursor que no se puede decodificar responde 400 `invalid_cursor`. No se cachea.
- **Enmienda (2026-11-19, KNOCK-69):** el orden de R-09 pasa a «activas primero» cuando no hay filtro `estado`; con filtro rige el orden anterior dentro de ese estado. R-09 queda así (el texto de arriba se conserva como historial):
  - **Grupos.** *Activas* (siguen en curso): `enviada`, `cotizando` y `cita_confirmada`. *Terminadas*: `terminada`, `calificada`, `cancelada` y `vencida`. La pertenencia sale de `estado` (§6) y no se guarda en otro campo.
  - **Orden.** Primero todas las activas y después todas las terminadas; dentro de cada grupo, `creadoEn` descendente y `_id` descendente como desempate. Con `estado=<valor>` solo hay un grupo y el orden es el de siempre.
  - **Cursor.** Sigue siendo opaco (Base64url) y ahora lleva tres datos: el grupo de la última fila (`0` activas, `1` terminadas), su `creadoEn` en milisegundos UTC y su `_id`. La página siguiente continúa en ese grupo con la condición `creadoEn < c` o (`creadoEn = c` y `_id < id`); cuando el grupo `0` se agota, la misma página se completa con las primeras filas del grupo `1` (sin repetir ni saltar filas) y el cursor pasa a `1`. `nextCursor` es `null` cuando no quedan filas en ninguno de los dos grupos. Un cursor de dos datos (el formato anterior), con un grupo distinto de `0` o `1`, o que no se pueda decodificar responde 400 `invalid_cursor`.
  - **Consulta.** Dos lecturas acotadas por `clienteId` y por el grupo (`estado` en la lista del grupo), con `limit + 1` filas en total; el índice `{ clienteId: 1, estado: 1, creadoEn: -1 }` (spec 048, §6, «Lista Actividad del cliente») las sirve sin recorrer solicitudes de otro cliente. No cambian la forma de la respuesta `{ items, nextCursor }`, `limit`, la validación de `estado` ni «no se cachea».
  - **Estabilidad.** Dentro de un grupo el orden no cambia entre páginas. Si una solicitud cambia de grupo mientras la persona pagina (por ejemplo, de `cita_confirmada` a `terminada`), puede verse una vez en cada grupo o no verse en esa recorrida; la app la corrige al refrescar la lista. No se garantiza una instantánea.
- **R-10:** `POST /api/v1/solicitudes/{id}/cotizaciones` exige JWT de técnico y cuerpo `{ tipoPrecio, precio, precioMin?, precioMax?, dia, hora, nota? }`; responde 201 con la cotización en estado `enviada`.
- **R-11:** El técnico se resuelve por el `sub` del token en la colección `tecnicos`. Si no es técnico responde 403 `not_a_technician`; si no cumple la elegibilidad base (R-23: oficio, zona y estado), 404 (no se confirma que la solicitud exista).
- **R-12:** Solo se cotiza una solicitud en estado `enviada` o `cotizando`, no vencida y no cancelada. Cualquier otro estado responde 409 `request_not_quotable`. Una segunda cotización del mismo técnico en la misma solicitud responde 409 `quote_duplicate`.
- **R-13:** `tipoPrecio` es `fijo`, `rango` o `visita`. `fijo` usa `precio`; `rango` usa `precioMin` y `precioMax` con `precioMin < precioMax`; `visita` usa `precio` como cuota de visita. Todos los montos son enteros en pesos mexicanos, de 1 a 200 000.
- **R-14:** El servidor calcula el `tope`: para `fijo`, `precio + 10 %`; para `rango`, `precioMax + 20 %` en electricidad y `+ 25 %` en plomería y mecánica; para `visita`, el mismo `precio`. El porcentaje, la cuota máxima de visita (250) y el piso de 100 pesos salen de configuración; el cliente no los envía. `requiereAprobacion` es verdadero solo si la diferencia entre el tope y el precio base es de 100 pesos o más.
- **R-15:** `dia` de la cotización va de la fecha de la solicitud a esa fecha + 7 días; `hora` es `HH:mm` de 00:00 a 23:59. `nota` es opcional, de hasta 300 caracteres, y pasa por moderación.
- **R-16:** La primera cotización de una solicitud mueve la solicitud de `enviada` a `cotizando` en una sola operación condicionada al estado; si otra cotización gana la carrera, la solicitud queda en `cotizando` una sola vez y se emite un solo evento.
- **R-17:** La cotización no usa `Idempotency-Key` por ahora (ver §15). El índice único `{ solicitudId, tecnicoId }` ya impide dos cotizaciones del mismo técnico en la misma solicitud: la segunda responde 409 `quote_duplicate` (R-12).
- **R-18:** Los listados y los detalles responden `Cache-Control: private, no-store`: son datos personales que cambian al crear. Las validaciones de oficios y colonias sí usan la caché de catálogo (24 h).
- **R-19:** Los errores usan ProblemDetails con `correlationId`, `traceId` y, cuando hay regla de negocio, `code` (convención de errores de §5). Las respuestas 429 llevan `Retry-After` (spec 009). Los registros no incluyen descripción, dirección, identificadores de fotos, enlaces firmados, nombre ni celular.
- **R-20:** Una solicitud admite como máximo 5 cotizaciones vivas (`enviada` o `aceptada`); la sexta responde 409 `quote_quota_full`. El conteo y la inserción se hacen en la misma transacción que R-16, para que dos técnicos simultáneos no pasen de 5.
- **R-21:** El tamaño del cuerpo JSON se limita a 8 KB por petición; uno mayor responde 413 antes de llegar al controller (`RequestBodyLimitMiddleware`).
- **R-22:** Privacidad: el teléfono del cliente nunca se entrega al técnico por estas rutas y la dirección del cliente solo sale en la vista del cliente dueño; la revelación al técnico elegido ocurre únicamente en la visita (spec 022) después de elegir (spec 021).
- **R-23:** **Elegibilidad base.** Un técnico puede ver el detalle de una solicitud (R-08) y cotizarla (R-10) solo si, al momento de la petición, tiene `estado = activo` y `oculto = false` (spec 027, R-07: los suspendidos, dados de baja o en eliminación programada no lo son), tiene el oficio de la solicitud y atiende su colonia (spec 027, R-08). No exige nivel de verificación (HU-021 la deja opcional). El filtro se aplica en el servidor, en la consulta, y el detalle lo vuelve a verificar; quien no la cumple recibe 404 uniforme. La spec 028 (oleadas) solo puede restringir más esta regla (por ejemplo, pedir un envío activo) y nunca ensancharla: con `Oleadas:Habilitado` apagada rige esta regla tal cual. Antes de ser elegido, el técnico no recibe dirección exacta ni datos del cliente (R-06 y R-22).

## 4. Criterios de aceptación

> Numeración: CA-03 y CA-20 se retiraron el 2026-11-10 junto con `Idempotency-Key`. Sus números no se reutilizan, para no cambiar los nombres de las pruebas previstas.

- **CA-01:** Dado un cliente con sesión, cuando crea una solicitud válida, entonces responde 201, queda en `enviada`, vence en 48 h y hay un `SolicitudCreada` en el buzón.
- **CA-02:** Dada una solicitud sin fotos, con 6 fotos, con fotos repetidas o con descripción de 7 caracteres, cuando se crea, entonces responde 422 con `errors` por campo y no guarda nada.
- **CA-04:** Dado un oficio inactivo o una colonia que no existe, cuando se crea, entonces responde 422 y no guarda nada.
- **CA-05:** Dada una solicitud de modalidad taller con dirección, cuando se crea, entonces responde 422 `address_not_applicable`; dada una de domicilio sin dirección, responde 422 con `errors.direccion`.
- **CA-06:** Dado un técnico que cumple la elegibilidad base (R-23), cuando envía una cotización válida, entonces responde 201, la solicitud pasa a `cotizando`, se emite `CotizacionEnviada` y el cliente recibe el aviso por evento.
- **CA-07:** Dado un técnico que ya cotizó esa solicitud, cuando lo intenta de nuevo, entonces responde 409 `quote_duplicate`.
- **CA-08:** Dada una solicitud vencida, cancelada o ya elegida, cuando un técnico intenta cotizar, entonces responde 409 `request_not_quotable`.
- **CA-09:** Dado un técnico de otro oficio o una solicitud inexistente, cuando intenta cotizar, entonces responde 404.
- **CA-10:** Dado un rango con el mínimo mayor o igual al máximo, o un monto negativo, de 0, mayor a 200 000 o con decimales, cuando se cotiza, entonces responde 422 con `errors` y no guarda nada.
- **CA-11:** Dado un precio fijo de 1 000, cuando se cotiza, entonces el tope guardado es 1 100 y `requiereAprobacion` es verdadero; con un precio fijo de 50 el tope es 55 y no requiere aprobación.
- **CA-12:** Dada una solicitud de otra persona, cuando la pide un cliente que no es el dueño, entonces responde 404; cuando la pide un técnico de otro oficio, o uno del mismo oficio con la solicitud vencida o cancelada sin haberla cotizado, también 404.
- **CA-13:** Dado un técnico que cumple la elegibilidad base (R-23) con una solicitud cotizable, o uno que ya la cotizó (elegido o no), cuando pide el detalle, entonces ve oficio, colonia, descripción, fotos con enlaces de 5 minutos, día y franja, y la respuesta no contiene la dirección ni el nombre ni el teléfono del cliente.
- **CA-14:** Dado un cliente con 25 solicitudes, cuando pide la lista, entonces recibe 20, ordenadas de la más reciente a la más antigua, con `nextCursor`; con `limit=50` recibe hasta 50.
- **Enmienda (2026-11-19, KNOCK-69):** CA-14 sigue valiendo para el tamaño de página y `limit`; el orden «de la más reciente a la más antigua» rige dentro de cada grupo y el orden entre grupos lo fija CA-24.
- **CA-15:** Dado el servicio de moderación caído, cuando se crea una solicitud, entonces responde 503 y no se guarda; si el texto se marca como inapropiado, responde 422.
- **CA-16:** Dada una petición sin token, cuando se llama cualquier ruta, entonces responde 401; con un token de cliente en la ruta de cotizar, 403.
- **CA-17:** Dado el registro de una creación o cotización, cuando se inspecciona, entonces no contiene descripción, dirección, identificadores de fotos, enlaces firmados ni nombre.
- **CA-18:** Dada una foto que no existe, que es de otra persona, que tiene otro `uso` o que no está `listo`, cuando se crea la solicitud, entonces responde 422 `photo_invalid` sin decir cuál y no guarda nada.
- **CA-19:** Dada una solicitud con 5 cotizaciones vivas, cuando un sexto técnico cotiza (también si dos lo intentan a la vez con 4), entonces responde 409 `quote_quota_full` y nunca hay más de 5.
- **CA-21:** Dado un cursor alterado o un `limit` de 0 o 51, cuando se pide la lista, entonces responde 400 `invalid_cursor` o 422 con `errors.limit`, respectivamente.
- **CA-22:** Dado un cuerpo JSON de más de 8 KB, cuando se envía a cualquier `POST` de esta spec, entonces responde 413 y no llega al servicio.
- **CA-23:** Dados un técnico activo con el oficio que atiende la colonia, otro de otro oficio, otro que no atiende la colonia, otro suspendido y otro oculto, cuando cada uno pide el detalle de una solicitud cotizable y cuando intenta cotizarla, entonces solo el primero accede y los demás reciben 404; el resultado es idéntico con `Oleadas:Habilitado` encendida y apagada.
- **CA-24 (agregado el 2026-11-19, KNOCK-69, HU-026 CA1):** Dado un cliente con solicitudes `terminada` y `cancelada` más recientes que otras `enviada`, `cotizando` y `cita_confirmada`, cuando pide la lista sin filtro, entonces primero vienen las activas y después las terminadas, cada grupo de la más reciente a la más antigua; con `limit` menor que el número de activas, el `nextCursor` de la primera página lleva a las activas que faltan y luego a las terminadas, sin repetir ni omitir ninguna; un cursor del formato anterior responde 400 `invalid_cursor`; con `estado=terminada` el orden es el de CA-14.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec020SolicitudesTests.Crea_Solicitud_Valida_CA_020_01` |
| CA-02 | `Spec020ValidacionTests.Rechaza_Fotos_Y_Descripcion_CA_020_02` |
| CA-04 | `Spec020ValidacionTests.Rechaza_Catalogo_Invalido_CA_020_04` |
| CA-05 | `Spec020ValidacionTests.Rechaza_Direccion_En_Taller_CA_020_05` |
| CA-06 | `Spec020CotizacionesTests.Envia_Cotizacion_CA_020_06` |
| CA-07 | `Spec020CotizacionesTests.Rechaza_Cotizacion_Duplicada_CA_020_07` |
| CA-08 | `Spec020CotizacionesTests.Rechaza_Solicitud_No_Cotizable_CA_020_08` |
| CA-09 | `Spec020CotizacionesTests.Oculta_Solicitud_Ajena_CA_020_09` |
| CA-10 | `Spec020CotizacionesTests.Rechaza_Montos_Invalidos_CA_020_10` |
| CA-11 | `Spec020TopeTests.Calcula_Tope_CA_020_11` |
| CA-12 | `Spec020SolicitudesTests.Oculta_Detalle_Ajeno_CA_020_12` |
| CA-13 | `Spec020SolicitudesTests.Vista_Tecnico_Sin_Direccion_CA_020_13` |
| CA-14 | `Spec020ListadoTests.Pagina_Solicitudes_Propias_CA_020_14` |
| CA-15 | `Spec020ModeracionTests.Falla_Cerrado_CA_020_15` |
| CA-16 | `Spec020SeguridadTests.Exige_Sesion_Y_Rol_CA_020_16` |
| CA-17 | `Spec020RegistrosTests.No_Registra_Datos_Personales_CA_020_17` |
| CA-18 | `Spec020FotosTests.Rechaza_Foto_No_Valida_CA_020_18` |
| CA-19 | `Spec020ConcurrenciaTests.Cupo_De_Cinco_Cotizaciones_CA_020_19` |
| CA-21 | `Spec020ListadoTests.Cursor_Y_Limite_Invalidos_CA_020_21` |
| CA-22 | `Spec020SeguridadTests.Cuerpo_Demasiado_Grande_CA_020_22` |
| CA-23 | `Spec020ElegibilidadTests.Base_Con_Y_Sin_Oleadas_CA_020_23` |
| CA-24 | `Spec020ListadoTests.Activas_Primero_Y_Cursor_Estable_CA_020_24` |

## 5. Contrato
| Método y ruta | Quién | Cuerpo / consulta | Respuestas |
|---|---|---|---|
| `POST /api/v1/solicitudes` | persona | `{ oficioClave, descripcion, fotos[] (archivoId), dia, franja, coloniaId, direccion? }` | 201 (solicitud) con `Location`, 400, 401, 413, 422, 429, 503 |
| `GET /api/v1/solicitudes` | persona (dueña) | `cursor?`, `limit?` (1–50, 20), `estado?` | 200 `{ items: SolicitudResumen[], nextCursor }`, 400 (`invalid_cursor`), 401, 422, 429 |
| `GET /api/v1/solicitudes/{id}` | dueña, técnico del oficio mientras sea cotizable o técnico que cotizó | — | 200 (vista según rol), 400 (id mal formado), 401, 404, 429 |
| `POST /api/v1/solicitudes/{id}/cotizaciones` | técnico | `{ tipoPrecio, precio?, precioMin?, precioMax?, dia, hora, nota? }` | 201 (cotización), 400, 401, 403, 404, 409, 413, 422, 429, 503 |

**Convención de errores (común a las specs 020 a 026):** 400 = cuerpo ilegible, identificador de ruta o cursor mal formado (`ObjectId` inválido, `invalid_cursor`; spec 002, R-10); 401 = sin sesión válida; 403 = rol equivocado (`not_a_technician`); 404 = recurso inexistente o ajeno, sin distinguir; 409 = estado o conflicto (con `code`); 413 = cuerpo mayor al límite; 422 = cuerpo o consulta con formato inválido (`errors` por campo, filtro de validación de `docs/MIDDLEWARES.md`) o regla de contenido (con `code`); 429 = límite, con `Retry-After`; 503 = dependencia caída, se falla cerrado. El código de negocio viaja en la extensión `code` del ProblemDetails.

**Cuerpos de respuesta**
- `SolicitudResumen` (lista de `GET /solicitudes`): la misma forma sin `descripcion` ni `direccion`. El campo `visitaId?` que consume «Ver visita» de frontend 020 lo agrega la spec 021 con su propio criterio; esta spec no lo define.
- Solicitud: `{ id, estado, modalidad, oficio: { clave, nombre }, colonia: { id, nombre, cp }, descripcion, fotos: [{ id, url, venceEn }], dia, franja, direccion?, totalCotizaciones, venceEn, creadoEn }`. `direccion` solo aparece en la vista del cliente dueño; `totalCotizaciones` solo en la del cliente.
- Resumen: `{ id, estado, oficioNombre, coloniaNombre, dia, franja, totalFotos, totalCotizaciones, venceEn, creadoEn }` (sin enlaces de fotos ni dirección).
- Cotización: `{ id, solicitudId, estado, tipoPrecio, precio, precioMin?, precioMax?, tope, requiereAprobacion, dia, hora, nota?, creadoEn }`.

**Códigos de error:** `trade_invalid` (422), `colonia_not_found` (422), `address_not_applicable` (422), `photo_invalid` (422), `content_rejected` (422), `moderation_unavailable` (503), `not_a_technician` (403), `quote_duplicate` (409), `request_not_quotable` (409), `quote_quota_full` (409), `invalid_cursor` (400), `moderation_limit` (429, cuota diaria de moderación de la spec 031, R-13).

**Límites:** cuerpo de 8 KB; `descripcion` 500; `nota` 300; `direccion` 200; 5 fotos; montos 200 000; 5 cotizaciones vivas por solicitud; 10 solicitudes por hora por persona; 3 cotizaciones por minuto por técnico (además del límite global y del de usuario del gateway; se registran en `docs/rate-limit-matrix.md`).

**Caché:** ninguna respuesta de esta spec se cachea (`Cache-Control: private, no-store`, sin `ETag`); los enlaces de fotos vencen a los 5 minutos y la app los vuelve a pedir con el detalle.

## 6. Modelo de datos
**`solicitudes`** (base `market`, módulo Requests)
| Campo | Tipo | Nota |
|---|---|---|
| `_id` | ObjectId | |
| `clienteId` | string | `sub` del JWT |
| `oficioClave`, `modalidad` | string | copiados del catálogo al crear |
| `descripcion` | string | 8–500, moderada |
| `fotos` | string[] | 1–5 `archivoId` del módulo de archivos (privados) |
| `dia` | string | `YYYY-MM-DD` |
| `franja` | string | `manana`, `tarde`, `noche` |
| `coloniaId`, `coloniaNombre`, `coloniaCp` | string | copia del catálogo |
| `direccionCifrada` | string? | AES-256-GCM; solo domicilio |
| `estado` | string | `enviada`, `cotizando`, `cita_confirmada`, `terminada`, `calificada`, `cancelada`, `vencida` |
| `cotizacionElegidaId` | ObjectId? | la escribe la spec 021 |
| `venceEn`, `creadoEn`, `actualizadoEn` | date | UTC |

Índices: `{ clienteId: 1, creadoEn: -1 }`; `{ estado: 1, venceEn: 1 }` (para HU-028); `{ oficioClave: 1, estado: 1 }`.
**Enmienda (2026-11-19, KNOCK-69):** se agrega el índice `{ clienteId: 1, estado: 1, creadoEn: -1 }` a `solicitudes`, el que la spec 048 (§6, «Lista Actividad del cliente») ya prevé; el de `{ clienteId: 1, creadoEn: -1 }` se conserva (no se borra ni cambia). El script `db/` lo crea de forma idempotente (tarea T-16).


**`cotizaciones`**
| Campo | Tipo | Nota |
|---|---|---|
| `_id` | ObjectId | |
| `solicitudId` | ObjectId | |
| `tecnicoId` | ObjectId | `_id` de `tecnicos` |
| `tecnicoUsuarioId` | string | `sub` del JWT |
| `tipoPrecio` | string | `fijo`, `rango`, `visita` |
| `precio`, `precioMin`, `precioMax`, `tope` | int | MXN enteros |
| `requiereAprobacion` | bool | calculado |
| `dia`, `hora`, `nota` | string | `nota` ≤ 300 |
| `estado` | string | `enviada`, `aceptada`, `rechazada`, `retirada` |
| `creadoEn`, `actualizadoEn` | date | UTC |

Índices: `{ solicitudId: 1, tecnicoId: 1 }` único; `{ solicitudId: 1, estado: 1, creadoEn: 1 }`; `{ tecnicoUsuarioId: 1, creadoEn: -1 }`.

El script `db/` de esta spec crea las colecciones y los índices; no borra ni cambia las existentes.

## 7. Arquitectura y componentes
- Módulo nuevo `src/Knock.Market.Api/Modules/Requests/` con la estructura de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Domain`, `Data`, `Events`.
- `SolicitudesController` y `CotizacionesController` (uno por recurso, por la regla S6960) reciben, validan el formato y delegan; no tocan MongoDB.
- `SolicitudService` y `CotizacionService` aplican las reglas y coordinan catálogo, moderación, cifrado y buzón.
- `RequestsStore` es el único punto con acceso a MongoDB del módulo; usa el cliente compartido de la spec 003 y la base `Mongo:Databases:Requests` (hoy `market`).
- `ICatalogoOficiosYColonias` del módulo consulta el catálogo con caché de 24 h; no lee las colecciones del módulo Catalog directamente.
- `CifradoDeCampo` (AES-256-GCM, llave versionada en Key Vault) cifra la dirección; el identificador de la versión de la llave se guarda junto al valor.
- `CotizacionTopeCalculator` es una función pura con la configuración de precios; es el punto que mutan las pruebas de mutación.
- El evento se escribe en la misma transacción que la solicitud o la cotización (outbox de la spec 003); el `_id` del evento es determinista para no duplicarlo.
- Sin estado en el proceso: todo vive en MongoDB; la caché solo guarda catálogos.

## 8. Dependencias
- Spec 002 (estructura y estilo de API), 003 (MongoDB, Redis, outbox), 009 (JWT, límites), 010 (paginación, caché), 013 (fotos y moderación), 017 (la persona y su colonia).
- TT-API-05 (KNOCK-42) y spec 013: módulo de archivos privados (`IMediaApi`) que confirma que un `archivoId` es del dueño, está `listo` y emite enlaces de lectura de 5 minutos; la moderación de la imagen ocurre al confirmarla (spec 013 y TT-IA-02, KNOCK-171).
- Spec 021 consume estas colecciones (elección y visita) y no las redefine.
- Catálogo de oficios y colonias del módulo Catalog de Mercado.
- Azure AI Content Safety para moderar la descripción y la nota (spec 013).
- Key Vault para la llave de cifrado de la dirección.
- **No depende** de la app del técnico: para la demo, las cotizaciones se envían con semillas y con el contrato de esta spec.

## 9. Amenazas y controles
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Ver o cotizar solicitudes ajenas | Todo se filtra por el `sub` del token; la solicitud ajena responde 404 | CA-12, CA-09 |
| Un técnico fuera de la zona, suspendido u oculto ve o cotiza una solicitud | Elegibilidad base (R-23) en la consulta y en el detalle, igual con las oleadas encendidas o apagadas | CA-23 |
| Suplantar a un técnico | El técnico se resuelve por `sub` en `tecnicos`; no se recibe su identificador | CA-16 |
| Manipular el precio o el tope | El tope y el porcentaje se calculan en el servidor; el cliente no los envía | CA-11 |
| Doble envío por reintento de la red | Sin clave de idempotencia por ahora (riesgo aceptado, §15); el límite de 10 solicitudes por hora acota el daño; en las cotizaciones, el índice único y la 409 | CA-07 (solo cotizaciones; la creación repetida es riesgo aceptado y no tiene criterio propio) |
| Fuga de la dirección | Se guarda cifrada y solo se descifra para el dueño o el técnico elegido | CA-13 |
| Contenido inapropiado | Moderación previa que falla cerrada | CA-15 |
| Usar fotos ajenas o sin moderar (IDOR sobre archivos) | Solo `archivoId` del mismo `sub`, `uso = solicitud` y `listo`; error uniforme | CA-18 |
| Fuga de las fotos (pueden mostrar el interior de la casa) | Contenedor privado, enlaces de lectura de 5 minutos solo para dueño y técnicos con permiso, nunca CDN | CA-13, CA-18 |
| Enumerar solicitudes probando identificadores | 404 uniforme, límite por usuario y vista del técnico sin datos de contacto | CA-12 |
| Saturar una solicitud con cotizaciones | Cupo de 5 cotizaciones vivas en transacción | CA-19 |
| Fuerza bruta o abuso de creación | Límite global y por usuario, más 3 cotizaciones por minuto | CA-16 |
| Datos personales en registros | Filtro de datos personales; no se registran cuerpos | CA-17 |
| Inflar la petición con muchas fotos o textos enormes | Tope de 5 fotos, 500 caracteres y 8 KB por petición (413) | CA-02, CA-22 |

## 10. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Unitarias | Sí | Validadores, cálculo del tope y transición de estados |
| Integración | Sí | MongoDB y Redis reales con `Testcontainers`: crear, cotizar, listar y leer el buzón |
| Seguridad negativa | Sí | Solicitud ajena, técnico de otro oficio, sin token, montos y fotos hostiles |
| Contrato | Sí | OpenAPI del servicio de Mercado regenerado y comparado |
| Arquitectura | Sí | El controller no accede a MongoDB ni a Key Vault; el módulo no lee colecciones de Catalog |
| Mutación | Sí | `CotizacionTopeCalculator` y la transición `enviada → cotizando` |
| Rendimiento | No | El volumen del piloto no lo exige; el listado ya está paginado |
| Aceptación | Sí | CA-01 a CA-23 (salvo CA-03 y CA-20, retirados) con la tabla 4.1 |
| Base de datos | Sí | `DatabaseTest`: el script `db/` crea colecciones e índices (incluidos los únicos parciales) de forma idempotente |
| Regresión | Sí | Las suites de las specs 015 a 019 y de Catalog siguen en verde; `dotnet test Knock.slnx -c Release` completo en cada PR |
| CI/CD | Sí | Job `gate` de `ci-pr.yml` (build-test, openapi-diff con `oasdiff`, sast, sca, secrets) en verde; tras el despliegue, smoke de `/health/ready` y de `GET /api/v1/solicitudes` con 401 sin token |
| Análisis estático | Sí | Semgrep (job `sast`, `.semgrep/semgrep.yaml`) sin hallazgos nuevos; analizadores de .NET con advertencias como errores y complejidad máxima 10 |
| Vulnerabilidades y licencias | Sí | NuGetAudit, `dotnet list package --vulnerable`, OSV-Scanner y dependency-review sin altas ni críticas; sin dependencias nuevas previstas |
| DAST | Sí (nocturno) | ZAP de API con token de prueba contra las cuatro rutas, fuera de la ventana de evaluación; no bloquea el PR (`docs/QA_DEVSECOPS.md` §8) |

**Gates:** los de `AGENTS.md`; el contrato de Mercado se regenera con `python scripts/export_openapi.py` y `oasdiff` no acepta cambios incompatibles. La prueba de arquitectura del módulo debe comprobar que el conjunto evaluado no está vacío.

## 11. Escenarios
**Positivos**
- Crear una solicitud de plomería a domicilio con tres fotos y recibir tres cotizaciones.
- Crear una solicitud de mecánica en taller sin dirección y recibir una cotización con rango.
- Reintentar el envío tras un corte de red: la app consulta `GET /api/v1/solicitudes` antes de volver a enviar (§15).

**Negativos**
- Persona sin sesión, o con sesión de otra persona, pidiendo el detalle.
- Técnico de electricidad cotizando una solicitud de plomería.
- Técnico cotizando dos veces la misma solicitud.
- Fotos de otra persona, sin confirmar, de otro `uso` o repetidas.
- Sexta cotización en una solicitud con cinco.

**Errores esperados**
- 400 por cuerpo ilegible o identificador o cursor mal formado, 401 sin sesión, 403 por rol, 404 por recurso ajeno, 409 por estado o duplicado, 422 por formato de campos, catálogo o moderación, 429 por límite, 503 por moderación o base no disponible.

**Casos límite**
- 1 y 5 fotos; 8 y 500 caracteres; día de hoy y día + 5; cotización el mismo día y + 7 días; precio 1 y 200 000; tope con diferencia exacta de 100 pesos; dos cotizaciones simultáneas de técnicos distintos; dos creaciones simultáneas de la misma persona (crean dos solicitudes; las acota el límite de 10 por hora); solicitud que vence mientras se cotiza.

## 12. Definición de terminado
- Los 23 criterios tienen prueba en verde y la tabla 4.1 coincide con los nombres reales (`python scripts/sdd_lint.py --trace 020` sin faltantes).
- La suite de arquitectura, la de seguridad negativa y el contrato de Mercado están en verde.
- `db/` crea las colecciones y los índices de forma idempotente y la prueba de base de datos los compara.
- OpenAPI regenerado, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados y `Memoria.md` con la entrada del cambio.
- Semillas de KNOCK-246 con al menos una solicitud por modalidad y tres cotizaciones de técnicos ficticios.
- Job `gate` en verde, Semgrep y análisis de dependencias sin hallazgos altos ni críticos.
- Revisión de par hecha y sin hallazgos abiertos de seguridad.

## 13. Decisiones
- **Un módulo, dos recursos:** `solicitudes` y `cotizaciones` viven juntos porque comparten estado y transacción; separarlos obligaría a una llamada entre módulos para cada cotización.
- **Tope en el servidor:** el porcentaje y la cuota de visita son configuración, no datos del cliente; así no se puede inflar el precio a voluntad.
- **Dirección cifrada:** es un dato personal de alto riesgo; se guarda con la misma protección que la celular de Identity.
- **Sin caché de solicitudes ni cotizaciones:** cambian al crear y una lista cacheada mostraría datos viejos (constitución §2.9 y spec 010).
- **El cliente no envía la modalidad:** la dicta el oficio; evita solicitudes de taller para un plomero.

## 14. Decisiones tomadas sin consulta
Se tomaron de noche, con el criterio de la constitución; quien apruebe puede cambiarlas antes de firmar.

1. **Se agrega `GET /api/v1/solicitudes`.** El insumo no lo lista, pero la app necesita la lista de “Actividad” y la spec 010 exige paginación. Alternativa: dejarla para otra spec y bloquear la pantalla.
2. **`Idempotency-Key` queda fuera por ahora (decisión del 2026-11-10).** La spec 010 la dejó fuera de su alcance y aquí se retira de los requisitos de creación; ver §15.
3. **La elegibilidad base se aplica siempre, con o sin oleadas (decisión del 2026-11-12).** Un técnico activo y no suspendido, con el oficio y en la zona de la solicitud, puede verla y cotizarla; no se exige nivel de verificación porque HU-021 la deja opcional. Las oleadas (spec 028) solo pueden restringir más, nunca ensanchar esta regla.
4. **422 para todo campo inválido (formato o catálogo) y 400 solo para cuerpo ilegible, identificador de ruta o cursor mal formado.** Es la regla de la spec 002 (R-09 y R-10) y del filtro de validación existente; el borrador separaba formato (400) de catálogo (422). Alternativa: 400 para formato, que mezcla la validación de campos en dos códigos.
5. **La dirección se guarda en `solicitudes`, cifrada, en vez de pedirla a Identity.** El modelo del proyecto dice “colonia o dirección” en la solicitud y así Mercado no depende de una interfaz de Identidad que aún no existe. Alternativa: consultar `direcciones` de Identity por servicio, que agrega una llamada entre servicios y un permiso nuevo.
6. **El teléfono del cliente no se revela al técnico en esta etapa.** La conversación va por el chat (spec 026); el teléfono del técnico sí se revela al cliente al elegir (spec 021). Alternativa: exponer el celular cifrado de Identity con una interfaz entre servicios, que se pospone.
7. **Una creación repetida crea otra solicitud.** Sin `Idempotency-Key` no hay respuesta repetida: cada petición válida responde 201 (ver §15).
8. **La cuota de visita no se resta aquí.** El descuento de la visita al contratar es una regla de cobro (HU posterior); esta spec solo guarda el precio de la visita.

**Agregadas en la revisión del 9 de noviembre**

9. **Fotos privadas por `archivoId`, no direcciones públicas.** La ficha de KNOCK-59 depende de TT-API-05 (fotos privadas con enlaces de 5 minutos) y el insumo pide «archivos privados»; una foto del problema puede mostrar el interior de una casa. Alternativa: contenedor público con CDN, más simple pero expone la foto a cualquiera con la dirección.
10. **El técnico del oficio puede ver la solicitud cotizable antes de cotizar, sin datos de contacto.** Sin esto el técnico cotizaría a ciegas, porque la lista de solicitudes nuevas depende de las oleadas (HU-025, Sprint 5). La spec de oleadas restringirá la vista a los técnicos de la oleada. Alternativa: solo el técnico que ya cotizó, que deja la cadena sin cotizaciones informadas.
11. **Cupo de 5 cotizaciones vivas por solicitud.** La comparación (spec 021) muestra hasta 5 y no pagina; sin cupo habría cotizaciones invisibles. Alternativa: paginar la comparación, que la pantalla no contempla.
12. **Convención de errores común (400, 422, 409) alineada con el filtro de validación existente (422 con `errors`).** El borrador usaba 400 para formato; el código vigente y `GET /colonias/*` responden 422. Se unifican las specs 020 a 026. Alternativa: 400 para formato como en la spec 017, que obligaría a cambiar el filtro común.
13. **Códigos de negocio en inglés y `snake_case`** (`request_not_quotable`), como las specs 015 a 018 (`account_conflict`) y la regla R-08 de la spec 002 (KNOCK-253). El borrador los tenía en español; las pantallas de frontend deben usar los mismos nombres.
14. **Retirada:** ya no hay clave de idempotencia que vencer ni huella del cuerpo; ver la decisión 2.

**Agregada el 2026-11-19 (KNOCK-69, HU-026)**

15. **El listado del cliente ordena las solicitudes activas primero (enmienda a R-09, CA-14 y §6).** El Tech Lead decidió la opción A de KNOCK-69: no se crea `/me/actividad`; la app arma «Actividad» con `GET /api/v1/solicitudes` y `GET /api/v1/conversaciones` (spec 030). Activas: `enviada`, `cotizando`, `cita_confirmada`; terminadas: `terminada`, `calificada`, `cancelada`, `vencida`. Alternativas: ordenar en la app (rompe la paginación: las activas antiguas quedarían en páginas lejanas); un campo `grupo` guardado (duplica el estado y hay que mantenerlo en cada transición).

## 15. Riesgos y dudas abiertas
- **Las oleadas (HU-025) no existen:** puede cotizar cualquier técnico que cumpla la elegibilidad base (R-23: activo, con el oficio y en la zona) si conoce el identificador. El riesgo se acota con esa regla y el límite de 3 por minuto, pero no es el reparto previsto. Un identificador `ObjectId` no es secreto: la zona y el estado se verifican siempre.
- **El vencimiento de 48 h no se ejecuta:** `venceEn` se guarda y se valida al cotizar y al elegir, pero la tarea programada que cambia el estado es HU-028. Una solicitud vencida puede quedarse en `enviada` hasta que alguien la toque.
- **Mercado no tiene host de integración:** la spec 017 lo dejó anotado; esta spec exige crear `MarketApiFactory` con `Testcontainers` para poder probar los 17 criterios. Es trabajo de la tarea T-01 y no se puede saltar.
- **La moderación consume cuota gratuita:** las pruebas usan un moderador falso; la prueba de integración con el servicio real se corre a mano.
- **El tope depende de configuración que aún no está aprobada:** 20 %, 25 %, 10 %, 250 y 100 salen de la descripción del proyecto; si el equipo cambia las reglas de precio, se cambia la configuración y no el código.
- **Los técnicos de la demo son semillas:** sin app de técnico, las cotizaciones se cargan con `db/`; el contrato es el mismo que usará la app.
- **El módulo de archivos no está en este repositorio:** TT-API-05 (KNOCK-42, Listo) vive en `legacy/src/Knock.Api/Modules/Media` y la fase de Blob y enlaces de la spec 013 (KNOCK-208) sigue en revisión. Sin `IMediaApi` en Mercado no se puede crear una solicitud con fotos: es la dependencia que bloquea la cadena y la primera tarea del plan.
- **`Idempotency-Key` retirada por ahora (decisión del 2026-11-10).** Se quitó de los requisitos de creación de solicitud y de cotización. Sin ella, un reintento de red o un doble toque puede crear dos solicitudes; las cotizaciones del mismo técnico en la misma solicitud siguen acotadas por el índice único `{ solicitudId, tecnicoId }` y la respuesta 409 `quote_duplicate`. Mitigaciones: la app desactiva el botón mientras envía, el límite de 10 solicitudes por hora por persona acota el daño y, si no llegó la respuesta, la app consulta `GET /api/v1/solicitudes` antes de reintentar. Se retoma con una spec propia si se decide volver a exigirla.
- **Etiquetas de Jira:** KNOCK-74 tiene `servicio-mensajeria`, pero la cotización vive en Mercado (módulo Requests) por compartir transacción con la solicitud; conviene corregir la etiqueta.
- **Transacciones de DocumentDB:** la creación de la cotización y el cambio de estado de la solicitud usan una transacción; si la instancia no las admite, todo falla (el mismo riesgo que la spec 015).

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Implementada (tras el merge)
