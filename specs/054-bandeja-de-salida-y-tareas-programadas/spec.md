# Spec 054 — Bandeja de salida y tareas programadas (INT-01)

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02), hoja `outbox_y_tareas`, endpoint interno INT-01 (sin ruta pública).
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** transversal (`Knock.Identity.Api`, `Knock.Market.Api`, `Knock.Messaging.Api`) · **Módulo:** `Common/TareasProgramadas` y `Common/Outbox` de cada servicio · **Colecciones:** `outbox` y `tareas_programadas` (técnicas, spec 003)
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · No tiene frontend: no hay pantallas ni rutas públicas.
> **Relacionada con:** specs 003 (colecciones técnicas), 028 (ejecutor de tareas y forma de `tareas_programadas`), 029, 037 (entrega interna de avisos), 025, 034.

## 1. Objetivo
Fijar en una sola spec cómo funcionan los dos procesos de fondo que sostienen los plazos y los avisos de Knock: la **bandeja de salida** (`outbox`), que entrega eventos entre servicios sin perderlos aunque un servicio se reinicie, y las **tareas programadas** (`tareas_programadas`), que vigilan los plazos (ampliar la búsqueda, vencer una solicitud, borrar fotos de verificación, eliminar una cuenta, cerrar el periodo de opinión). Hoy las specs 028 y 037 definen cada una su parte; esta spec las junta, completa el catálogo de plazos y cubre lo que falta en Identidad.

## 2. Historia y alcance
**2.1 Historia**
- Como equipo, queremos que los avisos y los plazos se cumplan una sola vez, incluso con dos réplicas y reinicios, para que ninguna solicitud, cuenta o foto de verificación se quede sin atender.

**2.2 Dentro**
- La forma de `outbox` y de `tareas_programadas` y sus índices en los tres servicios.
- El procesador de la bandeja de salida de Mercado e Identidad y su entrega a Mensajería (`POST /api/v1/interno/avisos`, spec 037 R-11).
- El ejecutor de tareas programadas de Identidad (el de Mercado ya lo fija la spec 028 R-09) y el registro de manejadores por tipo.
- El catálogo cerrado de tipos de tarea con su plazo.
- Reintentos, reserva atómica entre réplicas, limpieza, métricas, registros y pruebas con tiempo simulado.

**2.3 Fuera**
- Ninguna ruta pública ni del gateway: este endpoint es interno (INT-01).
- El contenido de cada manejador de negocio (reparto de oleadas, vencimiento, borrado de fotos, eliminación de cuenta, cierre de opiniones): lo define la spec de cada funcionalidad; aquí solo se fija el marco.
- El transporte de avisos a los teléfonos (spec 037) y el texto de los avisos (frontend 024).
- Una cola externa (Service Bus, Hangfire o similar): se decidió usar MongoDB por simplicidad y costo (spec 003).

## 3. Requisitos
- **R-01:** `outbox` y `tareas_programadas` existen en la base de cada servicio que las usa (Identidad, Mercado y Mensajería) con la forma de §6 y no tienen ruta HTTP pública ni se enrutan por el gateway.
- **R-02:** Todo evento se escribe en `outbox` **en la misma transacción** que el cambio que lo provoca. Su `_id` es determinista (`{tipo}:{recursoId}:{version}`), de modo que reintentar la operación no lo duplica. La `carga` lleva solo identificadores, nunca teléfono, dirección, nombre ni texto de la persona.
- **R-03:** El procesador de la bandeja de salida (Mercado e Identidad) reserva un evento a la vez con un solo `findOneAndUpdate` condicionado (`estado = pendiente` y `programadaPara <= ahora`, o `estado = en_proceso` con `reservadaHasta < ahora`), lo pasa a `en_proceso` con `reservadaHasta = ahora + 2 min` y lo entrega al servicio indicado en `destino`: los eventos de aviso van a Mensajería por `POST /api/v1/interno/avisos` (spec 037) y los demás (`CotizacionElegida`, `BloqueoCreado`, `BloqueoEliminado`) a `POST /api/v1/interno/eventos` del servicio destino, siempre con el token de R-15. Al recibir 202 lo marca `entregado`.
- **R-04:** Si la entrega falla (red, 5xx, 429), suma `intentos` y reprograma con espera exponencial de 5 s, 30 s, 2 min y 10 min; al quinto fallo queda `fallido` y se registra el tipo y el identificador, sin datos personales. Un 401 o 403 no se reintenta: queda `fallido` de inmediato.
- **R-05:** La entrega es **al menos una vez**: quien recibe es idempotente por `eventoId` (spec 037: 202 también si ya lo recibió) y el procesador nunca marca `entregado` sin haber recibido 202.
- **R-06:** `tareas_programadas` tiene la forma de la spec 028 (`tipo`, `claveNegocio`, `programadaPara`, `estado`, `intentos`, `reservadaHasta`) y un índice único `{ tipo, claveNegocio }`: programar dos veces la misma tarea no crea un duplicado y responde con la existente.
- **R-07:** El ejecutor de Identidad cumple las mismas reglas que el de Mercado (spec 028 R-09): consulta cada `TareasProgramadas:IntervaloSegundos` (15 por omisión), reserva con arrendamiento de 2 min, reintenta con 5 s, 30 s, 2 min y 10 min y al quinto fallo deja `fallida`. Los dos comparten el contrato `IManejadorDeTarea`; mientras no exista la biblioteca técnica compartida (KNOCK-251), su copia en Identidad se declara en la excepción de duplicados vigente.
- **R-08:** El catálogo de tipos es **cerrado**; cada tipo tiene un plazo: `RepartirOleada` y `RevisarOleada` (15 min, spec 028; la ampliación manual de la spec 048 programa la siguiente al instante), `VencerSolicitud` (48 h, specs 020 y 029), `AvisarAlVerificador` (24 h, spec 025), `BorrarFotosDeVerificacion` (7 días, spec 025), `EliminarCuenta` (7 días de gracia, spec 039), `PrepararExportacion` (inmediata, con entrega en un máximo de 48 horas, spec 039), `VencerRecuperacion` (7 días, spec 039), `VencerCotizacionFinal` (48 h, spec 049), `VencerAjuste` (24 h, spec 049), `VencerPropuestaDeReprogramacion` (24 h o 2 h antes de la cita, lo que ocurra primero, spec 050) y `CerrarPeriodoDeOpinion` (30 días, spec 051) y `CerrarReporteDeNoTerminado` (7 días, spec 050). Un `tipo` que no está en el catálogo no se ejecuta: pasa a `fallida` y se registra.
- **R-09:** Cada manejador es **idempotente**: comprueba el estado vigente antes de actuar, y ejecutar dos veces la misma tarea deja el mismo resultado. Si el hecho que la originó dejó de aplicar (por ejemplo se eligió una cotización o se canceló la eliminación), la tarea termina como `hecha` sin efecto.
- **R-10:** Con dos réplicas, ninguna tarea ni evento se ejecuta dos veces a la vez: la reserva atómica de R-03 y R-07 lo impide, y una reserva vencida (réplica caída a medias) la retoma otra réplica (spec 034).
- **R-11:** Limpieza: las tareas `hecha` y los eventos `entregado` se eliminan a los 14 días con un índice TTL sobre `actualizadoEn`; `fallida` y `fallido` se conservan 90 días para revisión.
- **R-12:** Todas las fechas son UTC y el ejecutor y el procesador usan `TimeProvider`, para probar plazos largos con tiempo simulado.
- **R-13:** Los registros y las métricas de ambos procesos llevan tipo, identificador, intento y resultado, sin datos personales. Hay una alerta cuando exista una tarea o un evento `fallida`/`fallido` o uno con más de 10 minutos de retraso (spec 011).
- **R-14:** `/health/ready` de cada servicio no depende de estos procesos (spec 006): una cola atrasada no saca al servicio del balanceo; su estado se ve en métricas, no en una ruta pública.
- **R-15:** **Llamadas internas entre servicios.** Toda llamada interna usa el mecanismo de la spec 037 R-11: HTTP interno con ingress privado, fuera del gateway, token HMAC de vida corta firmado por el emisor, con `iss`, `aud`, `exp` de 5 minutos como máximo, `jti` único y el claim `permisos` con las rutas que ese emisor puede llamar. Las rutas nuevas son `/api/v1/interno/eventos` (en Mercado y Mensajería), `/api/v1/interno/pendientes` y `/api/v1/interno/exportacion` (spec 039) y `/api/v1/interno/historial` (spec 041). **Claves:** lo que ya cubre la spec 037 (Mercado e Identidad hacia Mensajería) usa `avisos-key-market` y `avisos-key-identity`, que Mensajería ya lee; para **Identidad hacia Mercado** se agrega **un secreto nuevo, `interno-key-identity`**, que solo Identidad puede leer para firmar y solo Mercado para verificar, de modo que no se rompe el aislamiento por secreto de la 037 (cada servicio lee solo la clave que necesita). La spec 037 se amplía por esta spec (no se contradice).

## 4. Criterios de aceptación
- **CA-01:** Dada la configuración de rutas del gateway y de cada servicio, cuando se inspeccionan, entonces ninguna ruta pública expone `outbox` ni `tareas_programadas` y el gateway no enruta la llamada interna de avisos.
- **CA-02:** Dado un cambio de negocio que genera un evento, cuando falla la transacción, entonces no queda evento en `outbox`; y cuando sale bien, entonces cambio y evento quedan juntos.
- **CA-03:** Dada la misma operación repetida, cuando se reintenta, entonces el evento conserva el mismo `_id` y no se duplica.
- **CA-04:** Dado un evento, cuando se inspecciona su `carga`, entonces solo tiene identificadores, sin teléfono, dirección, nombre ni texto de la persona.
- **CA-05:** Dado un evento pendiente, cuando el procesador lo toma y el servicio destino responde 202, entonces queda `entregado`.
- **CA-06:** Dada una falla de red, cuando falla la entrega, entonces `intentos` aumenta y se reprograma con 5 s, 30 s, 2 min y 10 min; al quinto fallo queda `fallido` y se registra sin datos personales.
- **CA-07:** Dada una respuesta 401 o 403 de Mensajería, cuando el procesador la recibe, entonces el evento queda `fallido` sin reintentos.
- **CA-08:** Dado un evento entregado dos veces por un reintento, cuando Mensajería lo recibe, entonces responde 202 en ambas y el aviso se crea una sola vez.
- **CA-09:** Dada la misma tarea programada dos veces con el mismo `tipo` y `claveNegocio`, cuando se intenta crear, entonces queda un solo documento.
- **CA-10:** Dado el ejecutor de Identidad con tiempo simulado, cuando llega el plazo de una tarea, entonces se ejecuta una vez y queda `hecha`.
- **CA-11:** Dado un manejador que falla, cuando se reintenta, entonces sigue las esperas de R-07 y al quinto fallo queda `fallida` con registro sin datos personales.
- **CA-12:** Dado un `tipo` que no está en el catálogo, cuando el ejecutor lo toma, entonces no ejecuta nada y queda `fallida` con registro.
- **CA-13:** Dado cada tipo del catálogo, cuando se programa, entonces su `programadaPara` respeta el plazo que le fija R-08 (por ejemplo 15 minutos, 24 y 48 horas, 7 y 30 días).
- **CA-14:** Dada una tarea ejecutada dos veces, cuando termina la segunda, entonces el resultado es el mismo que tras la primera.
- **CA-15:** Dado un hecho que dejó de aplicar (cotización elegida, eliminación cancelada), cuando llega el plazo, entonces la tarea queda `hecha` sin efecto.
- **CA-16:** Dadas dos réplicas sobre la misma base, cuando vencen varias tareas y eventos, entonces cada uno se ejecuta una sola vez.
- **CA-17:** Dada una réplica que se cae con una reserva activa, cuando vence el arrendamiento de 2 min, entonces otra réplica lo retoma y lo termina.
- **CA-18:** Dadas tareas `hecha` y eventos `entregado` de más de 14 días y `fallida`/`fallido` de más de 90, cuando corre la limpieza, entonces se eliminan; los más recientes se conservan.
- **CA-19:** Dado un plazo de 30 días, cuando se prueba, entonces se usa tiempo simulado y no se espera tiempo real.
- **CA-20:** Dado un fallo o un atraso de más de 10 minutos, cuando se evalúa la métrica, entonces se emite la alerta con tipo e identificador y sin datos personales.
- **CA-21:** Dado un servicio con la cola atrasada, cuando se consulta `/health/ready`, entonces sigue respondiendo según sus dependencias y no por la cola.
- **CA-22:** Dada una llamada interna entre servicios, cuando el token no trae el `permiso` de esa ruta, está vencido o lo firmó otro emisor, entonces responde 401 o 403 y no ejecuta nada; con el permiso correcto responde con normalidad.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec054RutasTests.Sin_Rutas_Publicas_CA_054_01` |
| CA-02 | `Spec054OutboxTests.Evento_En_La_Misma_Transaccion_CA_054_02` |
| CA-03 | `Spec054OutboxTests.Id_Determinista_CA_054_03` |
| CA-04 | `Spec054OutboxTests.Carga_Sin_Datos_Personales_CA_054_04` |
| CA-05 | `Spec054ProcesadorTests.Entrega_Y_Marca_Entregado_CA_054_05` |
| CA-06 | `Spec054ProcesadorTests.Reintentos_Y_Fallo_CA_054_06` |
| CA-07 | `Spec054ProcesadorTests.Sin_Reintento_En_401_403_CA_054_07` |
| CA-08 | `Spec054ProcesadorTests.Entrega_Repetida_Idempotente_CA_054_08` |
| CA-09 | `Spec054TareasTests.Unica_Por_Tipo_Y_Clave_CA_054_09` |
| CA-10 | `Spec054EjecutorTests.Ejecuta_Una_Vez_CA_054_10` |
| CA-11 | `Spec054EjecutorTests.Reintentos_Y_Fallida_CA_054_11` |
| CA-12 | `Spec054EjecutorTests.Tipo_Desconocido_CA_054_12` |
| CA-13 | `Spec054CatalogoTests.Plazo_De_Cada_Tipo_CA_054_13` |
| CA-14 | `Spec054EjecutorTests.Manejador_Idempotente_CA_054_14` |
| CA-15 | `Spec054EjecutorTests.Hecho_Ya_No_Aplica_CA_054_15` |
| CA-16 | `Spec054ConcurrenciaTests.Dos_Replicas_Una_Ejecucion_CA_054_16` |
| CA-17 | `Spec054ConcurrenciaTests.Reserva_Vencida_Se_Retoma_CA_054_17` |
| CA-18 | `Spec054LimpiezaTests.Ttl_De_Hechas_Y_Fallidas_CA_054_18` |
| CA-19 | `Spec054EjecutorTests.Plazos_Largos_Con_Tiempo_Simulado_CA_054_19` |
| CA-20 | `Spec054ObservabilidadTests.Alerta_Sin_Datos_Personales_CA_054_20` |
| CA-21 | `Spec054SaludTests.Salud_No_Depende_De_La_Cola_CA_054_21` |
| CA-22 | `Spec054InternoTests.Token_Con_Permiso_Por_Ruta_CA_054_22` |

## 5. Contrato
**No hay ruta pública.** El contrato es interno:

| Elemento | Detalle |
|---|---|
| Endpoint | INT-01: proceso de fondo de cada servicio; no se expone a internet ni se enruta por el gateway |
| Entrega de eventos | `POST /api/v1/interno/avisos` del servicio de Mensajería (spec 037 R-11), con ingress privado, token HMAC por emisor y respuesta 202 (también si ya recibió el `eventoId`) |
| Manejadores | `IManejadorDeTarea` registrado por `tipo` (catálogo cerrado de R-08) |
| Configuración | `TareasProgramadas:IntervaloSegundos` (15), arrendamiento de 2 min, 5 intentos con esperas de 5 s, 30 s, 2 min y 10 min |

**Códigos de error:** no hay respuestas a personas. Los motivos de fallo se registran con un código interno en inglés y snake_case (`delivery_failed`, `unknown_task_type`, `handler_failed`).

## 6. Modelo de datos
**`outbox`** (en cada servicio emisor)

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| `_id` | string | Sí | Determinista: `{tipo}:{recursoId}:{version}` | Llave primaria |
| `tipo` | string | Sí | Lista cerrada: los ocho eventos de aviso de la spec 037, `sesion_revocada`, `CotizacionElegida`, `BloqueoCreado` y `BloqueoEliminado` | No |
| `destino` | string | Sí | Servicio que lo recibe: `messaging`, `market` o `identity` | No |
| `carga` | documento | Sí | Solo identificadores; sin datos personales | No |
| `estado` | string | Sí | `pendiente`, `en_proceso`, `entregado`, `fallido` | Compuesto |
| `programadaPara` | fecha UTC | Sí | Cuándo debe entregarse | Compuesto |
| `reservadaHasta` | fecha UTC | No | Arrendamiento de 2 min | No |
| `intentos` | entero | Sí | Máximo 5 | No |
| `creadoEn`, `actualizadoEn` | fecha UTC | Sí | | TTL sobre `actualizadoEn` |

**`tareas_programadas`** (forma de la spec 028)

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| `_id` | ObjectId | Sí | Lo genera la base | Llave primaria |
| `tipo` | string | Sí | Catálogo cerrado de R-08 | Único con `claveNegocio` |
| `claveNegocio` | string | Sí | Por ejemplo `{solicitudId}:{oleada}` | Único con `tipo` |
| `programadaPara` | fecha UTC | Sí | | Compuesto con `estado` |
| `estado` | string | Sí | `pendiente`, `en_proceso`, `hecha`, `fallida` | Compuesto |
| `intentos` | entero | Sí | Máximo 5 | No |
| `reservadaHasta` | fecha UTC | No | Arrendamiento de 2 min | No |
| `creadoEn`, `actualizadoEn` | fecha UTC | Sí | | TTL sobre `actualizadoEn` |

**Índices:** `outbox { estado, programadaPara }`; `tareas_programadas { estado, programadaPara }` y único `{ tipo, claveNegocio }`; TTL de 14 días para `hecha` y `entregado`, y de 90 días para `fallida` y `fallido` (dos índices parciales).

## 7. Arquitectura y componentes
- `Common/Outbox/` en Mercado e Identidad: `OutboxStore`, `ProcesadorDeOutbox` (servicio en segundo plano con `PeriodicTimer` y `TimeProvider`) y el cliente interno de avisos con el token de la spec 037.
- `Common/TareasProgramadas/` en Mercado (spec 028) e Identidad: `EjecutorDeTareasProgramadas`, `IManejadorDeTarea` y el registro por `tipo`; los manejadores de negocio viven en el módulo de cada spec.
- Los dos procesos no referencian módulos de negocio (solo `IManejadorDeTarea` y el contrato del evento) y no abren puertos.
- Mensajería solo recibe (spec 037) y no tiene ejecutor propio salvo que una spec futura lo pida.

## 8. Dependencias
- Specs 003 (colecciones técnicas y transacciones), 009 (token interno), 011 (registros y alertas), 006 (salud) y 034 (dos réplicas).
- Specs 028 y 029 (ejecutor de Mercado y vencimiento), 037 (entrega de avisos), 025 (fotos de verificación y aviso al verificador), 039 (eliminación de cuenta), 050 y 051 (cierre de periodos).
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Entrega interna de avisos y claves por emisor de la spec 037, más el secreto nuevo `interno-key-identity` (hoy no implementados ni creados en Key Vault y Terraform).
  - Ejecutor de tareas de la spec 028 en Mercado y las colecciones `outbox` y `tareas_programadas` (hoy no existen en `db/`).
  - Biblioteca técnica compartida (KNOCK-251) para quitar la copia del ejecutor en Identidad.

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Ejecución doble por dos réplicas | Reserva atómica con arrendamiento y manejadores idempotentes | CA-14, CA-16, CA-17 |
| Pérdida de un evento al reiniciar | Evento en la misma transacción y reintentos al menos una vez | CA-02, CA-05, CA-06, CA-08 |
| Datos personales en la cola o en los registros | Solo identificadores en `carga`; registros sin datos personales | CA-04, CA-11, CA-20 |
| Tarea de un tipo falso o manipulado | Catálogo cerrado; tipo desconocido no se ejecuta | CA-12 |
| Alcance desde internet | Sin ruta pública ni en el gateway; ingress privado | CA-01 |
| Bucle de reintentos que satura | Máximo 5 intentos con espera exponencial | CA-06, CA-11 |
| Servicio fuera del balanceo por una cola atrasada | La salud no depende de la cola | CA-21 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_054_NN` en el nombre (para `python scripts/sdd_lint.py --trace 054`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Catálogo de plazos, esperas de reintento, forma del `_id` y de la carga |
| Integración | Sí | MongoDB real con `Testcontainers`: transacción de cambio y evento, reserva atómica con dos instancias y tiempo simulado |
| Seguridad negativa | Sí | Sin rutas públicas, 401 y 403 sin reintentos, sin datos personales (CA-01, CA-04, CA-07, CA-20) |
| Base de datos | Sí | Índices, índice único y TTL |
| Concurrencia | Sí | Dos réplicas y reserva vencida (CA-16, CA-17) |
| Arquitectura | Sí | El ejecutor y el procesador no referencian módulos de negocio |
| Contrato / OpenAPI | No aplica | No hay rutas públicas |
| Rendimiento | No aplica | Consultas indexadas por `estado` y `programadaPara` |

## 11. Escenarios
- **Aviso de cotización recibida:** Mercado escribe la cotización y su evento en la misma transacción → el procesador lo toma, lo envía a Mensajería con el token de Mercado y recibe 202 → queda `entregado`; si Mensajería está caída, reintenta 5 s, 30 s, 2 min y 10 min.
- **Solicitud sin respuesta:** al crearla se programa `VencerSolicitud` a las 48 h → una sola réplica la reserva → si ya se eligió una cotización, termina `hecha` sin efecto.
- **Eliminación de cuenta:** al pedirla se programa `EliminarCuenta` a 7 días → si la persona la cancela antes, la tarea termina `hecha` sin efecto; si no, Identidad ejecuta la eliminación una sola vez.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 054` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- Scripts de `db/` con índices y TTL aplicados en las tres bases; métricas y alerta documentadas en `docs/observabilidad.md`.

## 13. Decisiones
- Una cola en MongoDB con reserva atómica, sin servicio externo (spec 003).
- El ejecutor de Mercado es el de la spec 028; Identidad agrega el suyo con las mismas reglas hasta que exista la biblioteca compartida.
- La entrega entre servicios usa HTTP interno con token HMAC por emisor (spec 037), y la bandeja de salida es el origen de los eventos.

## 14. Decisiones tomadas sin consulta
- **D-01:** Los estados siguen a la spec 028 (`pendiente`, `en_proceso`, `hecha`, `fallida`) y no los del Excel (`pendiente`, `tomada`, `hecha`, `fallida`); los campos `programadaPara` y `reservadaHasta` también (en el Excel se llaman `ejecutarEn` y `reservaVence`). Manda la spec numerada.
- **D-02:** Retención de 14 días para lo terminado y de 90 para lo fallido: el Excel no fija plazos de limpieza; 14 días alcanzan para investigar una incidencia reciente y 90 para revisar fallos, y son el valor de los índices de vencimiento, no una configuración.
- **D-03:** Las alertas por fallo o atraso de más de 10 minutos salen por las métricas de la spec 011, sin pantalla ni ruta.
- **D-04:** Las llamadas que la spec 037 ya cubre reutilizan sus dos claves por emisor; solo Identidad hacia Mercado necesita un secreto nuevo (`interno-key-identity`), el mínimo para no dar a Mercado la clave con la que Identidad firma sus avisos. Los `permisos` limitan lo que cada emisor puede llamar.
- **D-05:** Cada vigencia de las specs 039, 049 y 050 es una tarea del catálogo, para tener un solo mecanismo de plazos.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Catálogo de tareas: antes tenía siete tipos; ahora 12, para cubrir la exportación, la recuperación de acceso y las vigencias de las specs 049 y 050.
- **C-02:** Llamadas internas: antes solo se definía la entrega de avisos; ahora todas las llamadas internas siguen el esquema de la spec 037 con `permisos` por ruta y `destino` en cada evento (R-03 y R-15), con un único secreto nuevo para Identidad hacia Mercado.
- **C-03:** Primera propuesta de R-15: reutilizar las dos claves de la 037 para todo; se corrigió porque obligaba a Mercado a leer la clave de Identidad, contra el aislamiento por secreto de la spec 037.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- **Dependencias:** la spec 037 (entrega de avisos y claves por emisor, H-1), la biblioteca técnica compartida (KNOCK-251) para quitar la copia del ejecutor en Identidad y el costo de las consultas cada 15 segundos con dos réplicas (spec 034), que es bajo por estar indexadas.
- **Riesgo aceptado:** si Mensajería necesita tareas propias, esta spec se amplía.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
