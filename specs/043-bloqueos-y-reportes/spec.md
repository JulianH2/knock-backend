# Spec 043 — Bloqueos entre personas y reportes con apelación

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints BLO-01, BLO-02, BLO-03, REP-01, REP-02, REP-03.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Identidad (`Knock.Identity.Api`) · **Módulo:** `Safety` · **Colecciones:** `bloqueos`, `reportes`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 033)

## 1. Objetivo
Definir y especificar los endpoints BLO-01, BLO-02, BLO-03, REP-01, REP-02, REP-03 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como persona, quiero bloquear a alguien y reportar un abuso, para sentirme segura usando Knock.

## 2. Historia y alcance
**2.1 Historia**
- Como persona, quiero bloquear a alguien y reportar un abuso, para sentirme segura usando Knock.

**2.2 Dentro**
- `POST /api/v1/usuarios/{id}/bloqueo` (BLO-01): Bloquea a una persona.
- `DELETE /api/v1/usuarios/{id}/bloqueo` (BLO-02): Desbloquea (Deshacer).
- `GET /api/v1/me/bloqueos` (BLO-03): Lista a quién tiene bloqueado la persona.
- `POST /api/v1/reportes` (REP-01): Crea un reporte con motivo, texto y evidencia. Avisa de llamar al 911 si hay peligro.
- `GET /api/v1/reportes/me` (REP-02): Lista los reportes hechos por la persona.
- `POST /api/v1/reportes/{id}/apelacion` (REP-03): Apela una decisión (una sola vez).
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 033.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `POST /api/v1/usuarios/{id}/bloqueo` (crear): Bloquea a una persona. Lo puede llamar: persona con sesión (cliente o técnico). Responde 201 cuando sale bien.
- **R-02:** Entrada de `POST /api/v1/usuarios/{id}/bloqueo`: Ruta: id de quien se bloquea. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-03:** Estados de `POST /api/v1/usuarios/{id}/bloqueo`: 404 `user_not_found`; 409 `already_blocked`. Los códigos de negocio van en inglés y en snake_case.
- **R-04:** `DELETE /api/v1/usuarios/{id}/bloqueo` (borrar): Desbloquea (Deshacer). Lo puede llamar: persona con sesión (cliente o técnico). Responde 204 cuando sale bien.
- **R-05:** Entrada de `DELETE /api/v1/usuarios/{id}/bloqueo`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-06:** Estados de `DELETE /api/v1/usuarios/{id}/bloqueo`: 404 `block_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-07:** `GET /api/v1/me/bloqueos` (leer todos): Lista a quién tiene bloqueado la persona. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-08:** `GET /api/v1/me/bloqueos` pagina por cursor (Por cursor, 20): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: No aplica.
- **R-09:** `POST /api/v1/reportes` (crear): Crea un reporte con motivo, texto y evidencia. Avisa de llamar al 911 si hay peligro. Lo puede llamar: persona con sesión (cliente o técnico). Responde 201 cuando sale bien.
- **R-10:** Entrada de `POST /api/v1/reportes`: Cuerpo: reportadoId, referencia, motivo, descripcion, evidencia. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `reportadoId`: Distinto de reportaId; `motivo`: Solo esos valores; `descripcion`: Pasa por la moderación de contenido; `evidencia`: Máximo 5.
- **R-11:** `GET /api/v1/reportes/me` (leer todos): Lista los reportes hechos por la persona. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-12:** `GET /api/v1/reportes/me` pagina por cursor (Por cursor, 20): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Orden por fecha descendente.
- **R-13:** `POST /api/v1/reportes/{id}/apelacion` (actualizar (acción)): Apela una decisión (una sola vez). Lo puede llamar: persona con sesión que sea afectada del recurso. Responde 200 cuando sale bien.
- **R-14:** Entrada de `POST /api/v1/reportes/{id}/apelacion`: Ruta: id. Cuerpo: texto. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-15:** Estados de `POST /api/v1/reportes/{id}/apelacion`: 409 `appeal_not_allowed`. Los códigos de negocio van en inglés y en snake_case.
- **R-16:** **Bloquear (BLO-01 y BLO-02).** Una persona no puede bloquearse a sí misma (422); bloquear dos veces responde 409 `already_blocked`; desbloquear es posible en cualquier momento («Deshacer»). El bloqueo es en un solo sentido pero **corta en ambos**: ninguna de las dos puede enviarse mensajes, pedirse cotización ni verse en búsquedas.
- **R-17:** **No se revela el bloqueo.** A la persona bloqueada nunca se le dice que fue bloqueada: sus intentos de mensaje o solicitud responden igual que si la otra no estuviera disponible.
- **R-18:** **Efecto sobre lo abierto.** Al bloquear se cierra la conversación abierta entre ambas (queda de solo lectura, sin borrar mensajes); desde ese momento no se reparten solicitudes a la persona bloqueada (condición adicional de la elegibilidad de la spec 028, que solo la restringe y no la contradice) y las cotizaciones de la persona bloqueada no se muestran a quien bloqueó. Las visitas ya confirmadas **no se cancelan**: se completan con el código y la llegada de siempre (spec 022).
- **R-19:** **Lista (BLO-03).** Paginada por cursor (20 por defecto, máximo 50), de más reciente a más antigua, solo con identificador, nombre público y fecha, sin teléfono.
- **R-20:** **Crear un reporte (REP-01).** `motivo` de una lista cerrada; `descripcion` de 8 a 500 caracteres y moderada (spec 031); `evidencia` de 0 a 5 imágenes (spec 032); `referencia` opcional a una solicitud, visita u opinión; no se puede reportar a uno mismo (422). Si el motivo implica peligro, la respuesta incluye el aviso de llamar al 911.
- **R-21:** **Lista y apelación (REP-02 y REP-03).** La persona ve solo sus reportes, por cursor (20 por defecto); puede apelar **una sola vez** una decisión ya tomada, con `texto` de 8 a 500 caracteres. La apelación se asigna con prioridad a una persona del panel distinta de la que decidió; si no hay otra con el permiso, la revisa quien lo tenga y queda marcada «misma persona». Sin decisión o con una apelación previa responde 409 `appeal_not_allowed`; quien no es la persona afectada, 403.
- **R-22:** **Privacidad del reporte.** La persona reportada nunca ve quién la reportó ni el texto del reporte; la evidencia solo la ve el panel con enlaces firmados de vida corta (spec 025); los registros no llevan el texto ni los identificadores de las dos personas juntos.
- **R-23:** **Aplicar el bloqueo en otros servicios.** Los bloqueos viven en Identidad. Para aplicarlos sin leer su base (spec 001 R-03), Identidad publica `BloqueoCreado` y `BloqueoEliminado` por la bandeja de salida (spec 054) y Mercado y Mensajería mantienen una proyección mínima `bloqueos` (`bloqueaId`, `bloqueadoId`), idempotente por `eventoId`.
- **R-24:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-25:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-26:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-27:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/usuarios/{id}/bloqueo` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/usuarios/{id}/bloqueo`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/usuarios/{id}/bloqueo`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-04:** Dado el caso «la persona a bloquear no existe», cuando llama `POST /api/v1/usuarios/{id}/bloqueo`, entonces responde 404 `user_not_found` con ProblemDetails y no cambia el estado.
- **CA-05:** Dado el caso «ya la tiene bloqueada», cuando llama `POST /api/v1/usuarios/{id}/bloqueo`, entonces responde 409 `already_blocked` con ProblemDetails y no cambia el estado.
- **CA-06:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `DELETE /api/v1/usuarios/{id}/bloqueo` con datos válidos, entonces responde 204 con lo descrito en §5.
- **CA-07:** Dada una petición sin token o con token vencido, cuando llama `DELETE /api/v1/usuarios/{id}/bloqueo`, entonces responde 401 y no ejecuta la operación.
- **CA-08:** Dado un valor inválido según las reglas de §6, cuando llama `DELETE /api/v1/usuarios/{id}/bloqueo`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-09:** Dado el caso «no hay un bloqueo de esa persona», cuando llama `DELETE /api/v1/usuarios/{id}/bloqueo`, entonces responde 404 `block_not_found` con ProblemDetails y no cambia el estado.
- **CA-10:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/me/bloqueos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-11:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/me/bloqueos`, entonces responde 401 y no ejecuta la operación.
- **CA-12:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-13:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/reportes` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-14:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/reportes`, entonces responde 401 y no ejecuta la operación.
- **CA-15:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/reportes`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-16:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/reportes`, entonces responde 400 y no guarda nada.
- **CA-17:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/reportes/me` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-18:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/reportes/me`, entonces responde 401 y no ejecuta la operación.
- **CA-19:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-20:** Dado el rol «persona con sesión que sea afectada del recurso», cuando llama `POST /api/v1/reportes/{id}/apelacion` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-21:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/reportes/{id}/apelacion`, entonces responde 401 y no ejecuta la operación.
- **CA-22:** Dada una persona que no cumple el rol «persona con sesión que sea afectada del recurso», cuando llama `POST /api/v1/reportes/{id}/apelacion`, entonces responde 403 y no cambia nada.
- **CA-23:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/reportes/{id}/apelacion`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-24:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/reportes/{id}/apelacion`, entonces responde 400 y no guarda nada.
- **CA-25:** Dado el caso «ya apeló ese reporte o la decisión aún no existe», cuando llama `POST /api/v1/reportes/{id}/apelacion`, entonces responde 409 `appeal_not_allowed` con ProblemDetails y no cambia el estado.
- **CA-26:** Dada una persona que bloquea a otra, cuando ambas intentan escribirse o cotizar, entonces no pueden y la bloqueada no sabe que lo está; al desbloquear, se restablece.
- **CA-27:** Dada una persona bloqueada, cuando intenta escribir o cotizar con quien la bloqueó, entonces la respuesta es la misma que la de un técnico no disponible.
- **CA-28:** Dado un bloqueo con una conversación abierta y una visita confirmada, cuando se crea, entonces la conversación pasa a solo lectura, no se reparten solicitudes a la persona bloqueada y la visita confirmada sigue su curso normal.
- **CA-29:** Dada una persona con bloqueos, cuando consulta su lista, entonces recibe solo nombre público e identificador, paginados.
- **CA-30:** Dado un reporte válido, cuando se crea, entonces queda `abierto` y llega a revisión (spec 053); con más de 5 evidencias, una descripción corta o a sí misma responde 422; con motivo de peligro la respuesta incluye el aviso de emergencia.
- **CA-31:** Dada una decisión tomada, cuando la persona afectada apela, entonces el reporte pasa a `en_apelacion` y se asigna a otra persona del panel si la hay; una segunda apelación o apelar sin decisión responde 409 `appeal_not_allowed`, y la de otra persona, 403.
- **CA-32:** Dado un reporte, cuando la persona reportada consulta lo suyo, entonces no ve quién reportó ni el texto; y los registros no vinculan a ambas personas.
- **CA-33:** Dado un bloqueo creado o eliminado en Identidad, cuando Mercado y Mensajería reciben el evento, entonces su proyección `bloqueos` queda al día y recibir el mismo evento otra vez no cambia nada.
- **CA-34:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-35:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-36:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-37:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec043Tests.Criterio01_CA_043_01` |
| CA-02 | `Spec043Tests.Criterio02_CA_043_02` |
| CA-03 | `Spec043Tests.Criterio03_CA_043_03` |
| CA-04 | `Spec043Tests.Criterio04_CA_043_04` |
| CA-05 | `Spec043Tests.Criterio05_CA_043_05` |
| CA-06 | `Spec043Tests.Criterio06_CA_043_06` |
| CA-07 | `Spec043Tests.Criterio07_CA_043_07` |
| CA-08 | `Spec043Tests.Criterio08_CA_043_08` |
| CA-09 | `Spec043Tests.Criterio09_CA_043_09` |
| CA-10 | `Spec043Tests.Criterio10_CA_043_10` |
| CA-11 | `Spec043Tests.Criterio11_CA_043_11` |
| CA-12 | `Spec043Tests.Criterio12_CA_043_12` |
| CA-13 | `Spec043Tests.Criterio13_CA_043_13` |
| CA-14 | `Spec043Tests.Criterio14_CA_043_14` |
| CA-15 | `Spec043Tests.Criterio15_CA_043_15` |
| CA-16 | `Spec043Tests.Criterio16_CA_043_16` |
| CA-17 | `Spec043Tests.Criterio17_CA_043_17` |
| CA-18 | `Spec043Tests.Criterio18_CA_043_18` |
| CA-19 | `Spec043Tests.Criterio19_CA_043_19` |
| CA-20 | `Spec043Tests.Criterio20_CA_043_20` |
| CA-21 | `Spec043Tests.Criterio21_CA_043_21` |
| CA-22 | `Spec043Tests.Criterio22_CA_043_22` |
| CA-23 | `Spec043Tests.Criterio23_CA_043_23` |
| CA-24 | `Spec043Tests.Criterio24_CA_043_24` |
| CA-25 | `Spec043Tests.Criterio25_CA_043_25` |
| CA-26 | `Spec043Tests.Criterio26_CA_043_26` |
| CA-27 | `Spec043Tests.Criterio27_CA_043_27` |
| CA-28 | `Spec043Tests.Criterio28_CA_043_28` |
| CA-29 | `Spec043Tests.Criterio29_CA_043_29` |
| CA-30 | `Spec043Tests.Criterio30_CA_043_30` |
| CA-31 | `Spec043Tests.Criterio31_CA_043_31` |
| CA-32 | `Spec043Tests.Criterio32_CA_043_32` |
| CA-33 | `Spec043Tests.Criterio33_CA_043_33` |
| CA-34 | `Spec043Tests.Criterio34_CA_043_34` |
| CA-35 | `Spec043Tests.Criterio35_CA_043_35` |
| CA-36 | `Spec043Tests.Criterio36_CA_043_36` |
| CA-37 | `Spec043Tests.Criterio37_CA_043_37` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| BLO-01 | `POST /api/v1/usuarios/{id}/bloqueo` | persona con sesión (cliente o técnico) | Ruta: id de quien se bloquea | 201; 401; 404; 409 |
| BLO-02 | `DELETE /api/v1/usuarios/{id}/bloqueo` | persona con sesión (cliente o técnico) | Ruta: id | 204; 401; 404 |
| BLO-03 | `GET /api/v1/me/bloqueos` | persona con sesión (cliente o técnico) | Ninguno | 200; 401 |
| REP-01 | `POST /api/v1/reportes` | persona con sesión (cliente o técnico) | Cuerpo: reportadoId, referencia, motivo, descripcion, evidencia | 201; 400; 401; 422 |
| REP-02 | `GET /api/v1/reportes/me` | persona con sesión (cliente o técnico) | Ninguno | 200; 401 |
| REP-03 | `POST /api/v1/reportes/{id}/apelacion` | persona con sesión que sea afectada del recurso | Ruta: id. Cuerpo: texto | 200; 400; 401; 403; 409 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| BLO-01 | No aplica | No | No |
| BLO-02 | No aplica | No | No |
| BLO-03 | No aplica | Por cursor, 20 | No |
| REP-01 | No aplica | No | No |
| REP-02 | Orden por fecha descendente | Por cursor, 20 | No |
| REP-03 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| BLO-01 | 404 | `user_not_found` | la persona a bloquear no existe |
| BLO-01 | 409 | `already_blocked` | ya la tiene bloqueada |
| BLO-02 | 404 | `block_not_found` | no hay un bloqueo de esa persona |
| REP-03 | 409 | `appeal_not_allowed` | ya apeló ese reporte o la decisión aún no existe |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `bloqueos`.** Guarda quién bloqueó a quién. El bloqueo se hace desde el menú del chat y se puede deshacer. Mientras exista, las dos personas no pueden enviarse mensajes ni solicitudes

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| bloqueaId | ObjectId | Sí | Debe existir en usuarios | Compuesto único |
| bloqueadoId | ObjectId | Sí | Distinto de bloqueaId | Compuesto único |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Único compuesto | bloqueaId + bloqueadoId | Evitar bloqueos duplicados y consultar rápido si hay bloqueo |
| Normal | bloqueadoId | Saber quién tiene bloqueada a una persona |

**Colección `reportes`.** Guarda los reportes que una persona hace contra otra: el cliente contra un técnico, o el técnico contra un cliente. La respuesta depende de la gravedad: crítica con suspensión preventiva, alta que oculta al técnico de nuevas solicitudes, media con advertencia y baja con mediación. Cada reporte se puede apelar una vez

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| reportaId | ObjectId | Sí | Debe existir en usuarios | Sí |
| reportadoId | ObjectId | Sí | Distinto de reportaId | Sí |
| referencia | Documento | No |  | No |
| motivo | Texto | Sí | Solo esos valores | No |
| descripcion | Texto | Sí | Pasa por la moderación de contenido | No |
| evidencia | Lista de textos | No | Máximo 5 | No |
| gravedad | Texto | No | La asigna la moderación | Compuesto con estado |
| estado | Texto | Sí | Solo esos valores | Compuesto con gravedad |
| decision | Documento | No |  | No |
| apelacion | Documento | No | Solo una apelación por reporte | No |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | Sí |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Compuesto | gravedad + estado + creadoEn | Cola de moderación ordenada por gravedad y antigüedad, y plazos de respuesta |
| Normal | reportadoId | Contar reportes y advertencias de una cuenta para la suspensión gradual |
| Normal | reportaId | Que la persona vea sus propios reportes |

## 7. Arquitectura y componentes
- Proyecto `Knock.Identity.Api`, módulo `Safety` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 026 de este repositorio.
- Spec 030 de este repositorio.
- Spec 033 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 033 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Mensajería y conversaciones de las specs 026 y 030 y el reparto de solicitudes de la 028 (hoy no implementados)
  - Cola del panel de la spec 053 (hoy no implementada)
  - Eventos `BloqueoCreado` y `BloqueoEliminado` por la bandeja de salida de la spec 054 y la proyección `bloqueos` en Mercado y Mensajería (hoy no existen)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-22, CA-34 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-22, CA-34 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-03, CA-08, CA-15 a CA-16, CA-23 a CA-24 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-07, CA-11, CA-14, CA-18, CA-21 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-36 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-35 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-37 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_043_NN` en el nombre (para `python scripts/sdd_lint.py --trace 043`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-03, CA-07 a CA-08, CA-11, CA-14 a CA-16, CA-18, CA-21 a CA-24, CA-34, CA-36) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Bloquear:** durante un chat, la persona toca «Bloquear» → la conversación se cierra → puede deshacerlo desde Ajustes.
- **Reportar:** la persona reporta a un técnico con motivo, texto y fotos → recibe confirmación → la moderación lo revisa en el panel.
- **Apelar:** el técnico reportado ve que su perfil fue limitado → apela una vez con su explicación → otra persona del panel lo revisa.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 043` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** El bloqueo corta mensajes y solicitudes en ambos sentidos, como pide el Excel, y no se revela a la persona bloqueada.
- **D-05:** Las visitas ya confirmadas no se cancelan por un bloqueo: la persona puede cancelarlas ella misma y, si siente peligro, usar el reporte con el aviso del 911; cancelar automáticamente rompería la agenda de la otra parte.
- **D-06:** La apelación es única y se asigna con prioridad a otra persona del panel, sin bloquearla si el equipo es pequeño.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Efecto del bloqueo: antes cerraba conversaciones, retiraba cotizaciones y pasaba las visitas a soporte; ahora cierra el chat, deja de repartir y ocultar cotizaciones, y no toca visitas confirmadas.
- **C-02:** Apelación: antes obligaba a otra persona; ahora es preferente y no bloquea.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: conversaciones y reparto de solicitudes de las specs 026, 028 y 030, y la cola del panel de la spec 053.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
