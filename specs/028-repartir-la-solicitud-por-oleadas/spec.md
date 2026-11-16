# Spec 028 — Repartir la solicitud por oleadas

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Clave de trabajo:** KNOCK-68 (HU-025) · **Sprint:** 5 (la spec se deja lista en el Sprint 4)
> **Servicio:** Mercado · **Módulo:** Requests · **Colecciones:** `envios` (nueva), `solicitudes` y `cotizaciones` (se amplían), `tareas_programadas` (técnica, spec 003), `tecnicos` y `colonias` (lectura)
> **Otra parte en:** knock-frontend, spec 019 «Pestaña Trabajo» (consume la bandeja y el descarte)

## 1. Objetivo
Que cada solicitud llegue a pocos técnicos adecuados y no a todos: una primera oleada de hasta cinco técnicos disponibles que atienden la colonia (tres mejor ordenados, uno nuevo y uno cercano) y, si a los 15 minutos hay menos de dos cotizaciones, una ampliación a los siguientes. Así el cliente recibe cotizaciones rápidas y de calidad, los técnicos no se saturan y solo cotiza quien recibió la solicitud.

## 2. Historia y alcance
**2.1 Historia**
- **HU-025 (KNOCK-68)** — Como cliente que envió una solicitud, quiero que llegue a los técnicos adecuados y no a todos, para recibir cotizaciones rápidas y de calidad sin que los técnicos se saturen.

**2.2 Dentro**
- Selección de la oleada 1 al crearse la solicitud y revisión programada a los 15 minutos con ampliación.
- Registro de a quién se envió cada solicitud y en qué oleada (`envios`).
- El ejecutor de tareas programadas sobre `tareas_programadas` (spec 003), con reserva atómica entre réplicas; lo reutiliza la spec 029.
- La bandeja del técnico (`GET /api/v1/tecnicos/me/solicitudes`) y el descarte (`POST /api/v1/solicitudes/{id}/descartar`).
- Restringir la cotización a los técnicos que recibieron la solicitud y el tope de cinco cotizaciones por solicitud (restringe más la elegibilidad base de la spec 020, R-23).
- Ampliar la vista del técnico del detalle de la spec 020 a quien recibió la solicitud.
- Los eventos para avisar a los técnicos elegidos (la entrega del aviso es de HU-050).

**2.3 Fuera**
- Entregar el aviso push al técnico (HU-050 y la spec de notificaciones de Mensajería): aquí solo se escribe el evento en el buzón.
- El interruptor «Disponible hoy» y su apagado a medianoche (HU-019): aquí solo se lee `disponibleHasta`.
- El orden con aprendizaje automático (HU-060): mientras tanto se ordena por calificación.
- Cancelar y vencer solicitudes (spec 029), que sí cierran los envíos.
- La pestaña Trabajo completa (`GET /api/v1/tecnicos/me/trabajo`, frontend 019): la fija la spec 036 (Borrador).
- La app del técnico.

## 3. Requisitos
- **R-01:** Al crear una solicitud (spec 020), en la misma transacción se escribe la tarea `RepartirOleada` con `claveNegocio = {solicitudId}:1` y `programadaPara = creadoEn`. La creación no espera a la selección: responde como en la 020.
- **R-02:** Un técnico es **elegible** para una solicitud si cumple todo: `estado = activo`; `oculto = false`; tiene el `oficioClave` de la solicitud; `disponibleHasta > ahora`; atiende la colonia de la solicitud (misma regla de cobertura que la spec 027, R-08); su `usuarioId` no es el `clienteId` de la solicitud; no tiene ya un envío de esa solicitud (en cualquier estado, incluido `descartado`). Las condiciones de estado, oculto, oficio y zona son la elegibilidad base de la spec 020 (R-23) y rigen aunque las oleadas estén apagadas; las demás son propias del reparto.
- **R-03:** La oleada tiene como máximo `Oleadas:TamanoOleada` técnicos (5 por omisión) y se arma en este orden: 1) hasta `Oleadas:MejorOrdenados` (3) elegibles con mayor `calificacion`, desempate por más `trabajosTerminados` y luego por `_id`; 2) un técnico **nuevo** (`trabajosTerminados < Oleadas:UmbralNuevo`, 3 por omisión) entre los restantes, el de mayor calificación; 3) el elegible restante más cercano al centro de la colonia; 4) si falta alguno de los tipos anteriores, se completa con los siguientes mejor ordenados. Cada envío guarda su `motivo`: `mejor_ordenado`, `nuevo`, `cercano` o `relleno`.
- **R-04:** La selección es una función pura y determinista: con los mismos técnicos, la misma solicitud y la misma hora da el mismo resultado. Con 200 técnicos elegibles termina en menos de 200 ms.
- **R-05:** Al ejecutar `RepartirOleada` para la oleada `n`, en una sola transacción: se cambia `solicitudes.oleadaActual` de `n − 1` a `n` solo si la solicitud sigue en `enviada` o `cotizando` y no venció; se insertan los envíos (índice único `{ solicitudId, tecnicoId }`); se escribe un evento `SolicitudEnviadaATecnico` por envío; y se programa `RevisarOleada` con `claveNegocio = {solicitudId}:{n}` a `ahora + Oleadas:MinutosRevision` (15 por omisión). Si la condición de estado no se cumple, la tarea termina sin efectos.
- **R-06:** `RevisarOleada` de la oleada `n` cuenta las cotizaciones en estado `enviada` o `aceptada` de la solicitud. Si son menos de `Oleadas:MinimoCotizaciones` (2), la solicitud sigue abierta y `n < Oleadas:MaximoOleadas` (3), programa `RepartirOleada` de la oleada `n + 1` para ahora. Si hay 2 o más, si se llegó al máximo o si la solicitud ya no está abierta, no amplía.
- **R-07:** Si una oleada no encuentra ningún técnico elegible, no se crean envíos, `oleadaActual` sí avanza y se programa igual la revisión; al llegar al máximo sin envíos se escribe `SolicitudSinTecnicos` para avisar al cliente. La solicitud no cambia de estado por esto (la vence la spec 029).
- **R-08:** Todos los valores de R-03 a R-06 salen de configuración (`Oleadas:*`) y se validan al arrancar (enteros positivos, `MejorOrdenados + 2 <= TamanoOleada`); un valor inválido impide arrancar. `Oleadas:Habilitado` en falso desactiva el reparto: no se programan tareas y la cotización conserva la elegibilidad base de la spec 020 (R-23: técnico activo, con el oficio y en la zona), para las semillas de la demo. Las oleadas solo restringen más esa regla y nunca la ensanchan.
- **R-09:** **Ejecutor de tareas programadas.** Un servicio en segundo plano consulta `tareas_programadas` cada `TareasProgramadas:IntervaloSegundos` (15 por omisión) y reserva una tarea a la vez con un solo `findOneAndUpdate` condicionado (`estado = pendiente` y `programadaPara <= ahora`, o `estado = en_proceso` con `reservadaHasta < ahora`), que la pasa a `en_proceso` con `reservadaHasta = ahora + 2 min`. Al terminar la marca `hecha`; si falla, suma `intentos` y la reprograma con espera exponencial (5 s, 30 s, 2 min, 10 min); al quinto fallo queda `fallida` y se registra el tipo y el identificador de la tarea, sin datos personales.
- **R-10:** Las tareas son idempotentes: el índice único `{ tipo, claveNegocio }` impide programar dos veces la misma, y cada manejador comprueba el estado actual antes de actuar. Dos réplicas nunca ejecutan la misma tarea a la vez y una oleada nunca se reparte dos veces.
- **R-11:** **Cotizar exige envío.** Con `Oleadas:Habilitado`, `POST /api/v1/solicitudes/{id}/cotizaciones` (spec 020) solo acepta al técnico con un envío `activo` de esa solicitud; cualquier otro técnico recibe 404, igual que una solicitud inexistente. Al cotizar, el envío pasa a `cotizado`.
- **R-12:** **Tope de cotizaciones.** Una solicitud admite como máximo `Oleadas:MaximoCotizaciones` (5) cotizaciones en estado `enviada` o `aceptada`. La sexta responde 409 `quote_quota_full` con el detalle «Esta solicitud ya tiene suficientes cotizaciones». La comprobación es parte de la transacción de la cotización con un contador `solicitudes.cotizacionesActivas` incrementado de forma condicionada (`< 5`), para que dos cotizaciones simultáneas no pasen del tope.
- **R-13:** `GET /api/v1/tecnicos/me/solicitudes` exige JWT de técnico (403 `not_a_technician` si el `sub` no está en `tecnicos`) y devuelve sus envíos `activos` de solicitudes abiertas y no vencidas, de la más reciente a la más antigua, con paginación por cursor (`limit` de 1 a 20, 20 por omisión). Cada elemento es la vista acotada `{ solicitudId, oficio: { clave, nombre }, colonia: { nombre, cp }, descripcion, fotos[], dia, franja, modalidad, venceEn, oleada, recibidaEn, yaCotizo }`, **sin** dirección, nombre ni teléfono del cliente. Responde `Cache-Control: private, no-store`.
- **R-14:** `POST /api/v1/solicitudes/{id}/descartar` exige JWT de técnico y cuerpo vacío. Si el técnico tiene un envío `activo` de esa solicitud, lo marca `descartado` con `descartadoEn` y responde 204; repetirlo responde 204 sin cambios. Si no tiene envío, responde 404; si ya cotizó, 409 `request_not_dismissable`. Un técnico que descartó no vuelve a recibir esa solicitud. El descarte tiene un límite de 30 por hora por técnico (contador en Redis por `sub`, como los del gateway); al pasarse responde 429 con `Retry-After`.
- **R-15:** La vista del técnico de `GET /api/v1/solicitudes/{id}` (spec 020, R-08) se amplía a quien tiene un envío `activo` o `cotizado` de la solicitud, con las mismas reglas: colonia sí, dirección solo si fue elegido. Un técnico con el envío `descartado` o `cerrado` recibe 404.
- **R-16:** Los eventos `SolicitudEnviadaATecnico` (`{ solicitudId, tecnicoId, oleada }`) y `SolicitudSinTecnicos` (`{ solicitudId }`) se escriben en el buzón de salida en la misma transacción y no llevan descripción, dirección, nombres ni teléfonos. Se registran en `docs/events.md`.
- **R-17:** Los registros llevan solo conteos (elegibles, enviados, oleada) y tiempos; nunca el `sub`, el `tecnicoId` en texto libre, la colonia ni la descripción. Los errores usan ProblemDetails con `correlationId` y `traceId`.

## 4. Criterios de aceptación
- **CA-01:** Dados 10 técnicos elegibles que atienden la colonia, cuando se crea la solicitud, entonces existe una tarea `RepartirOleada` pendiente con `claveNegocio = {solicitudId}:1` y, al ejecutarla, hay 5 envíos: 3 `mejor_ordenado`, 1 `nuevo` y 1 `cercano`, y 5 eventos `SolicitudEnviadaATecnico`.
- **CA-02:** Dada una oleada 1 con menos de 2 cotizaciones a los 15 minutos, cuando corre la revisión, entonces se reparte la oleada 2 solo a técnicos que no tenían envío.
- **CA-03:** Dada una solicitud con 2 cotizaciones a los 15 minutos, cuando corre la revisión, entonces no se amplía.
- **CA-04:** Dado un técnico que descartó la solicitud, otro oculto, otro suspendido, otro con `disponibleHasta` vencido y el propio cliente registrado como técnico, cuando se arma una oleada, entonces ninguno la recibe.
- **CA-05:** Dadas dos réplicas del ejecutor sobre la misma base, cuando vence la revisión, entonces la oleada siguiente se reparte una sola vez y no hay envíos repetidos.
- **CA-06:** Dada una solicitud con 5 cotizaciones activas, cuando llega una sexta, entonces responde 409 `quote_quota_full`; y dadas dos cotizaciones simultáneas con 4 activas, cuando terminan, entonces una responde 201 y la otra 409.
- **CA-07:** Dados 10 técnicos elegibles sin ningún nuevo, cuando se reparte, entonces hay 3 `mejor_ordenado`, 1 `cercano` y 1 `relleno`, y nunca más de 5.
- **CA-08:** Dados los mismos técnicos, cuando se calcula la selección dos veces, entonces el resultado es idéntico; y con 200 elegibles tarda menos de 200 ms.
- **CA-09:** Dada una solicitud cancelada o vencida antes de la revisión, cuando corre `RevisarOleada` o `RepartirOleada`, entonces no hay envíos nuevos ni eventos.
- **CA-10:** Dadas 3 oleadas sin cotizaciones suficientes, cuando corre la tercera revisión, entonces no se programa una cuarta; y si ninguna oleada encontró técnicos, entonces hay un `SolicitudSinTecnicos`.
- **CA-11:** Dado un técnico con el oficio pero sin envío de la solicitud, cuando intenta cotizar, entonces responde 404; y con el envío activo, responde 201 y el envío queda `cotizado`.
- **CA-12:** Dado un técnico con tres envíos activos, cuando pide su bandeja, entonces recibe tres elementos sin dirección, nombre ni teléfono del cliente, con `Cache-Control: private, no-store`; y un cliente que pide la bandeja recibe 403 `not_a_technician`.
- **CA-13:** Dado un envío activo, cuando el técnico descarta, entonces responde 204, el envío queda `descartado` y desaparece de la bandeja; repetirlo responde 204; descartar una solicitud que no recibió responde 404 y una que ya cotizó, 409 `request_not_dismissable`.
- **CA-14:** Dado un técnico con envío activo, cuando pide el detalle de la solicitud, entonces ve la colonia y no la dirección; con el envío descartado recibe 404.
- **CA-15:** Dada una tarea que falla, cuando se reintenta, entonces se reprograma con espera creciente y al quinto fallo queda `fallida`; y dada una tarea reservada por una réplica que se cayó, cuando pasa `reservadaHasta`, entonces otra réplica la toma.
- **CA-16:** Dado `Oleadas:Habilitado` en falso, cuando se crea una solicitud, entonces no hay tareas de reparto y solo cotiza un técnico que cumple la elegibilidad base de la spec 020 (R-23), con el mismo resultado que con la bandera encendida para los técnicos fuera de zona; y dado `MejorOrdenados = 4` con `TamanoOleada = 5`, cuando arranca el servicio, entonces falla.
- **CA-17:** Dados los eventos y los registros de un reparto, cuando se inspeccionan, entonces no contienen descripción, dirección, nombres, teléfonos ni `sub`.
- **CA-18:** Dada una petición sin token a la bandeja o al descarte, cuando se llama, entonces responde 401; y el descarte 31 de un técnico en una hora responde 429 con `Retry-After`.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec028SeleccionTests.Compone_Oleada_Tres_Uno_Uno_CA_028_01` |
| CA-02 | `Spec028OleadasTests.Amplia_Con_Menos_De_Dos_CA_028_02` |
| CA-03 | `Spec028OleadasTests.No_Amplia_Con_Dos_O_Mas_CA_028_03` |
| CA-04 | `Spec028SeleccionTests.Excluye_No_Elegibles_CA_028_04` |
| CA-05 | `Spec028ConcurrenciaTests.Una_Sola_Ampliacion_Con_Dos_Replicas_CA_028_05` |
| CA-06 | `Spec028TopeTests.Rechaza_Sexta_Cotizacion_CA_028_06` |
| CA-07 | `Spec028SeleccionTests.Completa_Con_Relleno_CA_028_07` |
| CA-08 | `Spec028SeleccionTests.Determinista_Y_Rapida_CA_028_08` |
| CA-09 | `Spec028OleadasTests.Ignora_Solicitud_Cerrada_CA_028_09` |
| CA-10 | `Spec028OleadasTests.Tope_De_Oleadas_Y_Sin_Tecnicos_CA_028_10` |
| CA-11 | `Spec028CotizarTests.Cotizar_Exige_Envio_CA_028_11` |
| CA-12 | `Spec028BandejaTests.Bandeja_Sin_Datos_Del_Cliente_CA_028_12` |
| CA-13 | `Spec028DescarteTests.Descarta_Idempotente_CA_028_13` |
| CA-14 | `Spec028DetalleTests.Vista_Del_Destinatario_CA_028_14` |
| CA-15 | `Spec028EjecutorTests.Reintentos_Y_Reserva_Vencida_CA_028_15` |
| CA-16 | `Spec028ConfiguracionTests.Interruptor_Y_Validacion_CA_028_16` |
| CA-17 | `Spec028RegistrosTests.Sin_Datos_Personales_CA_028_17` |
| CA-18 | `Spec028SeguridadTests.Exige_Sesion_Y_Limita_Descartes_CA_028_18` |

## 5. Contrato
| Método y ruta | Quién | Cuerpo / consulta | Respuestas |
|---|---|---|---|
| `GET /api/v1/tecnicos/me/solicitudes` | técnico | `cursor?`, `limit?` (1–20) | 200 `CursorPage<SolicitudRecibida>`, 400, 401, 403, 429 |
| `POST /api/v1/solicitudes/{id}/descartar` | técnico con envío | vacío | 204, 400, 401, 403, 404, 409, 429 |
| `POST /api/v1/solicitudes/{id}/cotizaciones` (spec 020) | técnico con envío activo | sin cambios | se agregan 404 (sin envío) y 409 `quote_quota_full` |
| `GET /api/v1/solicitudes/{id}` (spec 020) | dueña, técnico cotizante o con envío activo | sin cambios | sin cambios |

**`SolicitudRecibida`:** `{ solicitudId, oficio: { clave, nombre }, colonia: { nombre, cp }, descripcion, fotos[], dia, franja, modalidad, venceEn, oleada, recibidaEn, yaCotizo }`.

**Códigos de error nuevos:** `quote_quota_full` (409), `request_not_dismissable` (409). Se reutiliza `not_a_technician` (403) de las specs 020 y 022.

**Eventos (buzón de Mercado):** `SolicitudEnviadaATecnico { solicitudId, tecnicoId, oleada }`, `SolicitudSinTecnicos { solicitudId }`. Tareas: `RepartirOleada`, `RevisarOleada`.

**Límites:** 5 técnicos por oleada, 3 oleadas, 15 minutos entre revisiones, 5 cotizaciones por solicitud, 20 elementos por página de la bandeja, 30 descartes por hora por técnico.

**Encabezados:** `Cache-Control: private, no-store` en la bandeja; `Retry-After` en 429.

## 6. Modelo de datos
**`envios`** (nueva, base `market`, módulo Requests)
| Campo | Tipo | Nota |
|---|---|---|
| `_id` | ObjectId | |
| `solicitudId` | ObjectId | |
| `tecnicoId` | ObjectId | `tecnicos._id` |
| `tecnicoUsuarioId` | string | `sub` del técnico, para la bandeja |
| `oleada` | int | 1 a 3 |
| `motivo` | string | `mejor_ordenado`, `nuevo`, `cercano`, `relleno` |
| `estado` | string | `activo`, `cotizado`, `descartado`, `cerrado` |
| `cerradoMotivo` | string? | `request_cancelled`, `request_expired`, `other_quote_chosen` (lo escriben las specs 021 y 029) |
| `enviadoEn`, `descartadoEn?`, `actualizadoEn` | date | UTC |

Índices: `{ solicitudId: 1, tecnicoId: 1 }` único; `{ tecnicoUsuarioId: 1, estado: 1, enviadoEn: -1 }` (bandeja); `{ solicitudId: 1, estado: 1 }`.

**`solicitudes`** (spec 020; campos nuevos): `oleadaActual` (int, 0 al crear), `cotizacionesActivas` (int, 0 al crear).

**`tareas_programadas`** (técnica, spec 003; esta spec fija su forma)
| Campo | Tipo | Nota |
|---|---|---|
| `_id` | ObjectId | |
| `tipo` | string | `RepartirOleada`, `RevisarOleada` (y `VencerSolicitud` de la spec 029) |
| `claveNegocio` | string | `{solicitudId}:{oleada}` |
| `programadaPara` | date | UTC |
| `estado` | string | `pendiente`, `en_proceso`, `hecha`, `fallida` |
| `intentos` | int | |
| `reservadaHasta` | date? | arrendamiento de 2 min |
| `creadoEn`, `actualizadoEn` | date | |

Índices: `{ estado: 1, programadaPara: 1 }` (ya existe en `db/001_indices.js`); `{ tipo: 1, claveNegocio: 1 }` único (nuevo); `{ actualizadoEn: 1 }` TTL de 30 días solo para `hecha` (índice parcial).

**`tecnicos`** (lectura; además de los campos de la spec 027): `usuarioId`, `disponibleHasta` (date), `trabajosTerminados` (int). Índice nuevo: `{ oficios: 1, estado: 1, oculto: 1, disponibleHasta: 1 }`.

El script `db/` de esta spec crea `envios`, los índices nuevos y no borra ni cambia documentos.

## 7. Arquitectura y componentes
- `SeleccionDeOleada` (función pura, `Domain/`): recibe la solicitud, los candidatos ya filtrados con su distancia y la configuración, y devuelve los envíos con su motivo. Es el punto de mutación y de la prueba de rendimiento.
- `RepartoService` (`Services/`): ejecuta `RepartirOleada` y `RevisarOleada` con la transacción de R-05; consulta candidatos con `CandidatosStore` (`$geoNear` desde el centro de la colonia, filtros de R-02 y proyección mínima).
- `EnviosStore`: único punto con `envios`; `RequestsStore` (spec 020) gana el incremento condicionado de `cotizacionesActivas` y el avance de `oleadaActual`.
- `EjecutorDeTareasProgramadas` (`Common/TareasProgramadas/` del servicio Mercado): servicio en segundo plano con `PeriodicTimer` y `TimeProvider`, reserva atómica, reintentos y un registro de manejadores por `tipo` (`IManejadorDeTarea`). No contiene reglas de negocio; la spec 029 registra `VencerSolicitud`. Es compartible con otros servicios por copia con su excepción de duplicación si llega a hacer falta.
- `BandejaController` y `DescarteController` separados (S6960); no tocan MongoDB.
- `CotizacionService` (spec 020) consulta `EnviosStore` antes de aceptar la cotización y actualiza el envío en la misma transacción.
- `OleadasOptions` valida la configuración al arrancar.
- Sin estado en el proceso: la oleada vigente, los envíos y las tareas viven en MongoDB.

## 8. Dependencias
- Spec 003 (outbox, `tareas_programadas`, transacciones), 009 (JWT y límites), 010 (cursor, sin caché personal), 020 (solicitudes, cotizaciones, detalle), 021 (al elegir, cierra los envíos restantes con `other_quote_chosen`; ver §15), 027 (regla de cobertura y campos de `tecnicos`).
- Spec 029 reutiliza el ejecutor y cierra los envíos al cancelar o vencer.
- Spec 030 usa los envíos para permitir el chat antes de elegir.
- HU-019 («Disponible hoy») sin spec: `disponibleHasta` se llena por semillas mientras tanto.
- La entrega de avisos al técnico (HU-050) consume `SolicitudEnviadaATecnico`.
- Frontend 019 consume la bandeja y el descarte.

## 9. Amenazas y controles
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Un técnico cotiza solicitudes que no recibió (adivinando el identificador) | Cotizar exige envío activo; 404 sin envío | CA-11 |
| Ver datos del cliente desde la bandeja | Vista acotada sin dirección, nombre ni teléfono; dirección solo al ser elegido | CA-12, CA-14 |
| Saturar al cliente con cotizaciones | Tope de 5 con contador condicionado en la transacción | CA-06 |
| Doble reparto con dos réplicas | Reserva atómica de la tarea, índice único de tareas y envíos, avance condicionado de `oleadaActual` | CA-05, CA-15 |
| Repartir a técnicos suspendidos, ocultos o no disponibles | Filtros de elegibilidad en la consulta | CA-04 |
| El cliente se envía su propia solicitud como técnico | Exclusión por `usuarioId = clienteId` | CA-04 |
| Configuración que rompe el reparto | Validación al arrancar; el servicio no arranca con valores inválidos | CA-16 |
| Tareas atascadas por una réplica caída | Arrendamiento de 2 minutos y reintentos con tope | CA-15 |
| Enumerar solicitudes por el descarte | 404 uniforme sin envío; límite de 30 descartes por hora | CA-13 |
| Datos personales en eventos y registros | Solo identificadores y conteos | CA-17 |

## 10. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Unitarias | Sí | `SeleccionDeOleada` (composición, relleno, determinismo), reglas de revisión, validación de opciones y política de reintentos |
| Integración | Sí | MongoDB y Redis reales con `Testcontainers`: transacción de reparto, `$geoNear`, ejecutor con dos instancias sobre la misma base y tiempo simulado (`TimeProvider` falso) |
| Seguridad negativa | Sí | Cotizar sin envío, bandeja con token de cliente, descarte ajeno, sin token |
| Contrato | Sí | OpenAPI de Mercado regenerado con las dos rutas y los errores nuevos; `oasdiff` sin cambios incompatibles en las rutas de la 020 |
| Base de datos | Sí | Script idempotente de `envios` e índices; comparación de índices |
| Arquitectura | Sí | Controllers sin MongoDB; el ejecutor no referencia módulos de negocio (solo `IManejadorDeTarea`) |
| Mutación | Sí | `SeleccionDeOleada` y la condición de ampliación (meta 70 %) |
| Rendimiento | Sí | Selección con 200 técnicos en menos de 200 ms (CA-08), sin cobertura |
| Humo y extremo a extremo | Sí | Flujo en el entorno de evaluación con dos instancias del ejecutor: solicitud → oleada 1 → descarte → oleada 2 → cotización; la bandeja sin token responde 401 |
| Regresión | Sí | Las pruebas de la spec 020 siguen en verde con `Oleadas:Habilitado` en falso |
| CI/CD y análisis | Sí | Semgrep, Gitleaks, Trivy y dependencias sin hallazgos nuevos; sin paquetes nuevos |
| Aceptación | Sí | CA-01 a CA-18 con la tabla 4.1 |

**Gates:** los de `AGENTS.md`; la prueba de dos réplicas usa dos ejecutores en el mismo proceso sobre la misma base y comprueba un solo reparto por oleada.

## 11. Escenarios
**Positivos**
- Solicitud de plomería con 10 plomeros disponibles: 5 reciben, 2 cotizan antes de 15 minutos, no se amplía.
- Solicitud de mecánica con 1 cotización a los 15 minutos: se amplía a 5 más.
- Un técnico descarta desde la bandeja y no la vuelve a ver.

**Negativos**
- Técnico sin envío que cotiza con el identificador: 404.
- Sexta cotización: 409.
- Cliente que pide la bandeja: 403.

**Errores esperados**
- 400 (cursor inválido), 401, 403 `not_a_technician`, 404, 409 (`quote_quota_full`, `request_not_dismissable`), 429.

**Casos límite**
- Exactamente 5 elegibles; 0 elegibles en las 3 oleadas; 4 cotizaciones y dos simultáneas; técnico con `disponibleHasta` igual a la hora del reparto (no elegible); réplica que se cae con la tarea reservada; solicitud que vence entre la reserva y la ejecución; técnico que descarta mientras cotiza (gana el primero que confirma; el otro recibe 409 o 404).

## 12. Definición de terminado
- Los 18 criterios tienen prueba en verde y la tabla 4.1 coincide con los nombres reales.
- El ejecutor demuestra con dos instancias una sola ejecución por tarea.
- `db/` crea `envios` e índices de forma idempotente; `docs/events.md` registra los eventos y tareas; `docs/endpoints.md` las rutas nuevas y las respuestas nuevas de la 020.
- OpenAPI regenerado, `Memoria.md` con la entrada del cambio y la forma de `tareas_programadas`.
- Revisión de par y sin hallazgos de seguridad abiertos.

## 13. Decisiones
- **Reparto asíncrono con tarea programada, no dentro de la creación:** la creación de la solicitud no depende de cuánto tarde la selección y el reintento queda resuelto por el ejecutor.
- **Ejecutor propio sobre `tareas_programadas`:** la constitución pide plazos fiables con varias réplicas; un bus de mensajes administrado (Azure Service Bus) tiene costo y TT-API-04 no tiene spec. El ejecutor usa solo MongoDB, que ya existe.
- **Cotizar exige envío:** sin esta regla las oleadas no limitan nada; con `Oleadas:Habilitado` en falso se conserva la elegibilidad base de la spec 020 (R-23), no un acceso más amplio.
- **Contador condicionado para el tope de 5:** contar con una consulta y luego insertar dejaría pasar dos cotizaciones simultáneas.

## 14. Decisiones tomadas sin consulta
Se tomaron de noche, con el criterio de la constitución; quien apruebe puede cambiarlas antes de firmar.

1. **Máximo de 3 oleadas (15 técnicos).** La ficha dice «se amplía a los siguientes» sin tope; sin tope, una solicitud sin cotizaciones llegaría a todo el padrón en pocas horas, que es lo que la historia quiere evitar. Alternativa: ampliar hasta el vencimiento.
2. **«Técnico nuevo» = menos de 3 trabajos terminados.** La ficha habla de un cupo de arranque sin definirlo. Alternativa: antigüedad de la cuenta menor a 30 días, que no mide experiencia.
3. **Un técnico que descartó nunca vuelve a recibir la solicitud,** ni en oleadas posteriores. Alternativa: permitir que la reciba en la ampliación, que la ficha excluye (CA4).
4. **El ejecutor revisa cada 15 segundos.** El retraso máximo de una oleada es de 15 s más la duración de la tarea; menos intervalo aumenta las consultas a MongoDB sin beneficio visible. Alternativa: 5 s.
5. **El aviso al técnico es un evento, no un push directo.** El servicio de Mensajería es el dueño de las notificaciones (constitución §4); Mercado no conoce los tokens de dispositivo.
6. **La bandeja es una ruta propia (`/tecnicos/me/solicitudes`) y no la pestaña Trabajo completa.** Frontend 019 pide `GET /tecnicos/me/trabajo`, que agrega disponibilidad, verificación, visitas y resumen de otras historias sin spec; la bandeja es lo único que esta historia puede comprometer. Alternativa: escribir el agregado aquí, mezclando cinco historias.
7. **El descarte responde 204 y es idempotente por estado,** como lo consume frontend 019.
8. **Se agrega 409 `quote_quota_full` a la ruta de la spec 020** en vez de crear otra ruta: el tope es una regla de la misma operación.

## 15. Riesgos y dudas abiertas
- **`Idempotency-Key` retirada por ahora (decisión del 2026-11-10).** Se quitó del descarte, que ya es idempotente por estado: repetirlo responde 204 sin cambios. Se retoma con una spec propia si se decide volver a exigirla.
- **Conflicto con la spec 020 (borrador):** R-11, R-12 y R-15 cambian su comportamiento (cotizar exige envío, tope de 5, vista del destinatario) y agregan los campos `oleadaActual` y `cotizacionesActivas`. La 020 ya no depende de esta spec: trae la elegibilidad base (R-23) y esta solo puede restringir más.
- **Conflicto con la spec 021 (borrador):** al elegir, los envíos restantes deben pasar a `cerrado` con `other_quote_chosen`; la 021 no lo menciona. Mientras no se agregue, la bandeja filtra por estado de la solicitud y no muestra solicitudes ya elegidas.
- **Hueco:** `tecnicos` sin spec dueña (ver spec 027 §15) y HU-019 sin spec: `disponibleHasta` se siembra a mano para la demo.
- **Resuelto:** `GET /api/v1/tecnicos/me/trabajo` (frontend 019) tiene spec de backend 036 (Borrador).
- **Hueco:** la entrega de avisos push (HU-050) y el transporte de eventos entre servicios no tienen spec; los eventos quedan en el buzón de Mercado.
- **Transacciones de DocumentDB:** el reparto usa transacción; si la instancia no las admite, falla completo (mismo riesgo que las specs 015 y 020).
- **El contador `cotizacionesActivas` no baja si una cotización se retira** (HU-035 sin spec): cuando exista el retiro, debe decrementarlo en la misma transacción.
- **Sin app del técnico:** la bandeja y el descarte se prueban con semillas y llamadas directas.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Implementada (tras el merge)
