# Spec 048 — Solicitudes y cotizaciones: ampliar, archivos, referencia de mercado y cotizaciones del técnico

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints SOL-05, COT-03, COT-05.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Mercado (`Knock.Market.Api`) · **Módulo:** `Requests` · **Colecciones:** `solicitudes`, `cotizaciones`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 038)

## 1. Objetivo
Definir y especificar los endpoints SOL-05, COT-03, COT-05 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como cliente quiero ampliar mi solicitud y ver sus archivos, y como técnico ver mis cotizaciones y una referencia de mercado, para negociar con información clara.

## 2. Historia y alcance
**2.1 Historia**
- Como cliente quiero ampliar mi solicitud y ver sus archivos, y como técnico ver mis cotizaciones y una referencia de mercado, para negociar con información clara.

**2.2 Dentro**
- `POST /api/v1/solicitudes/{id}/ampliar` (SOL-05): Adelanta la siguiente oleada de la spec 028 (Pedir también a otros).
- `GET /api/v1/cotizaciones/referencia-mercado` (COT-03): Rango de lo que cobran otros técnicos por trabajos parecidos (cotizaciones aceptadas en 90 días, percentil 25 al 75 y mediana). Solo con al menos 10 cotizaciones; si no,….
- `GET /api/v1/tecnicos/me/cotizaciones` (COT-05): Cotizaciones enviadas por el técnico con su estado (esperando respuesta, aceptada).
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 038.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `POST /api/v1/solicitudes/{id}/ampliar` (actualizar (acción)): Adelanta la siguiente oleada de la spec 028 (Pedir también a otros). Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 200 cuando sale bien.
- **R-02:** Entrada de `POST /api/v1/solicitudes/{id}/ampliar`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-03:** Estados de `POST /api/v1/solicitudes/{id}/ampliar`: 404; 409 `request_not_extendable`. Los códigos de negocio van en inglés y en snake_case.
- **R-04:** `GET /api/v1/cotizaciones/referencia-mercado` (búsqueda específica): Rango de lo que cobran otros técnicos por trabajos parecidos (cotizaciones aceptadas en 90 días, percentil 25 al 75 y mediana). Solo con al menos 10 cotizaciones; si no, rango general de la categoría. Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-05:** Entrada de `GET /api/v1/cotizaciones/referencia-mercado`: Query: oficio, tipoTrabajo. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-06:** `GET /api/v1/cotizaciones/referencia-mercado` se cachea (Sí, 1 hora); la clave nunca incluye datos personales de otra persona y se invalida al escribir.
- **R-07:** `GET /api/v1/tecnicos/me/cotizaciones` (leer todos / filtro): Cotizaciones enviadas por el técnico con su estado (esperando respuesta, aceptada). Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-08:** Entrada de `GET /api/v1/tecnicos/me/cotizaciones`: Query: estado (enviada|aceptada|rechazada|retirada), cursor, limite. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo. Reglas: `estado`: Solo una elegida por solicitud.
- **R-09:** `GET /api/v1/tecnicos/me/cotizaciones` pagina por cursor (Por cursor, 20): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Filtro por estado.
- **R-10:** **Ampliar (SOL-05).** «Pedir también a otros» adelanta la **siguiente oleada de la spec 028**: crea la tarea `RepartirOleada` de la oleada siguiente con `programadaPara = ahora` en lugar de esperar los 15 minutos, solo si la solicitud sigue `enviada` o `cotizando`, no venció y no se alcanzó el máximo de 3 oleadas (15 técnicos) de la 028; en otro caso responde 409 `request_not_extendable`.
- **R-11:** **Archivos de la solicitud (SOL-08) no se implementa aparte.** El detalle de la spec 020 ya entrega a la dueña y al técnico incluido los enlaces temporales de las fotos (5 minutos, sin caché ni registro) y la app los vuelve a pedir al reabrir la pantalla; una ruta separada duplicaría esa responsabilidad.
- **R-12:** **Referencia de mercado (COT-03).** Calcula el percentil 25, la mediana y el percentil 75 de las cotizaciones **aceptadas** del mismo oficio y tipo de trabajo en los últimos 90 días y solo si hay al menos 10; si no, devuelve el rango general de la categoría con `origen: categoria`. No revela precios individuales ni identidades; se cachea 1 hora por oficio y tipo.
- **R-13:** **Mis cotizaciones (COT-05).** Lista solo las cotizaciones del técnico, de la más reciente a la más antigua, filtradas por `estado` con los estados de la spec 020 (`enviada`, `aceptada`, `rechazada`, `retirada`); el cursor es opaco y el tamaño por defecto es 20 (máximo 50).
- **R-14:** **Cierre de conversaciones al elegir (CNV-05, evento interno, sin ruta pública).** Cuando el cliente elige una cotización (spec 021), Mercado escribe el evento `CotizacionElegida` en su bandeja de salida (spec 054) y esta lo entrega a Mensajería por `POST /api/v1/interno/eventos`; Mensajería cierra las demás conversaciones de esa solicitud, de forma idempotente por `eventoId` y sin borrar mensajes.
- **R-15:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-16:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-17:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-18:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/solicitudes/{id}/ampliar` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/solicitudes/{id}/ampliar`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/solicitudes/{id}/ampliar`, entonces responde 403 y no cambia nada.
- **CA-04:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/solicitudes/{id}/ampliar`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-05:** Dado el caso «recurso no disponible», cuando llama `POST /api/v1/solicitudes/{id}/ampliar`, entonces responde 404 con ProblemDetails y no cambia el estado.
- **CA-06:** Dado el caso «la solicitud no está abierta (vencida, cancelada o con cotización elegida) o ya fue ampliada una vez», cuando llama `POST /api/v1/solicitudes/{id}/ampliar`, entonces responde 409 `request_not_extendable` con ProblemDetails y no cambia el estado.
- **CA-07:** Dado el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/cotizaciones/referencia-mercado` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-08:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/cotizaciones/referencia-mercado`, entonces responde 401 y no ejecuta la operación.
- **CA-09:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/cotizaciones/referencia-mercado`, entonces responde 403 y no cambia nada.
- **CA-10:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/cotizaciones/referencia-mercado`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-11:** Dada una consulta repetida dentro del tiempo de caché, cuando llega la segunda, entonces se responde desde la caché y la base recibe una sola consulta.
- **CA-12:** Dado el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/tecnicos/me/cotizaciones` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-13:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/tecnicos/me/cotizaciones`, entonces responde 401 y no ejecuta la operación.
- **CA-14:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/tecnicos/me/cotizaciones`, entonces responde 403 y no cambia nada.
- **CA-15:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/tecnicos/me/cotizaciones`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-16:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-17:** Dada una solicitud abierta con menos de 3 oleadas, cuando su dueña la amplía, entonces se reparte de inmediato la oleada siguiente sin repetir técnicos; con 3 oleadas ya repartidas o una solicitud que no está abierta responde 409 `request_not_extendable`.
- **CA-18:** Dado el detalle de una solicitud, cuando lo pide la dueña o un técnico incluido, entonces recibe enlaces de fotos de 5 minutos; no existe una ruta separada de archivos de la solicitud.
- **CA-19:** Dadas al menos 10 cotizaciones aceptadas en 90 días, cuando el técnico consulta la referencia, entonces recibe el rango P25 a P75 y la mediana con `origen: mercado`; con menos de 10, el rango general con `origen: categoria`, sin precios individuales.
- **CA-20:** Dado un técnico con cotizaciones en varios estados, cuando filtra por `estado`, entonces ve solo las suyas de ese estado en orden descendente; un valor fuera de `enviada`, `aceptada`, `rechazada` y `retirada` responde 422.
- **CA-21:** Dada una cotización elegida, cuando Mensajería recibe el evento, entonces cierra las demás conversaciones de la solicitud sin borrar mensajes, y recibir el mismo evento otra vez no cambia nada.
- **CA-22:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-23:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-24:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-25:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec048Tests.Criterio01_CA_048_01` |
| CA-02 | `Spec048Tests.Criterio02_CA_048_02` |
| CA-03 | `Spec048Tests.Criterio03_CA_048_03` |
| CA-04 | `Spec048Tests.Criterio04_CA_048_04` |
| CA-05 | `Spec048Tests.Criterio05_CA_048_05` |
| CA-06 | `Spec048Tests.Criterio06_CA_048_06` |
| CA-07 | `Spec048Tests.Criterio07_CA_048_07` |
| CA-08 | `Spec048Tests.Criterio08_CA_048_08` |
| CA-09 | `Spec048Tests.Criterio09_CA_048_09` |
| CA-10 | `Spec048Tests.Criterio10_CA_048_10` |
| CA-11 | `Spec048Tests.Criterio11_CA_048_11` |
| CA-12 | `Spec048Tests.Criterio12_CA_048_12` |
| CA-13 | `Spec048Tests.Criterio13_CA_048_13` |
| CA-14 | `Spec048Tests.Criterio14_CA_048_14` |
| CA-15 | `Spec048Tests.Criterio15_CA_048_15` |
| CA-16 | `Spec048Tests.Criterio16_CA_048_16` |
| CA-17 | `Spec048Tests.Criterio17_CA_048_17` |
| CA-18 | `Spec048Tests.Criterio18_CA_048_18` |
| CA-19 | `Spec048Tests.Criterio19_CA_048_19` |
| CA-20 | `Spec048Tests.Criterio20_CA_048_20` |
| CA-21 | `Spec048Tests.Criterio21_CA_048_21` |
| CA-22 | `Spec048Tests.Criterio22_CA_048_22` |
| CA-23 | `Spec048Tests.Criterio23_CA_048_23` |
| CA-24 | `Spec048Tests.Criterio24_CA_048_24` |
| CA-25 | `Spec048Tests.Criterio25_CA_048_25` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| SOL-05 | `POST /api/v1/solicitudes/{id}/ampliar` | persona con sesión que sea dueña del recurso | Ruta: id | 200; 401; 403; 404; 409 |
| COT-03 | `GET /api/v1/cotizaciones/referencia-mercado` | cuenta con perfil de técnico | Query: oficio, tipoTrabajo | 200; 401; 403 |
| COT-05 | `GET /api/v1/tecnicos/me/cotizaciones` | cuenta con perfil de técnico | Query: estado (enviada/aceptada/rechazada/retirada), cursor, limite | 200; 401; 403 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| SOL-05 | No aplica | No | No |
| COT-03 | No aplica | No | Sí, 1 hora |
| COT-05 | Filtro por estado | Por cursor, 20 | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| SOL-05 | 409 | `request_not_extendable` | la solicitud no está abierta (vencida, cancelada o con cotización elegida) o ya fue ampliada una vez |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `solicitudes`.** Es lo que el cliente envía describiendo su problema: categoría, texto, de una a cinco fotos obligatorias, día y franja, y la dirección si es a domicilio. Se reparte por oleadas a hasta cinco técnicos disponibles y, si en 15 minutos hay menos de dos cotizaciones, se amplía. Vence a las 48 horas. Si nace en el perfil de un técnico va solo a esa persona. El técnico solo ve la colonia hasta que su co…

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| clienteId | ObjectId | Sí | Debe existir en usuarios | Compuesto |
| oficio | Texto | Sí | Debe existir en oficios | Compuesto |
| descripcion | Texto | Sí | De 8 a 500 caracteres. Moderado antes de guardar | No |
| fotos | Lista de textos | Sí | De 1 a 5. JPG o PNG, hasta 8 MB, reducidas a 900 px | No |
| datosVehiculo | Documento | No | Solo para mecánica | No |
| dia | Fecha | Sí | Entre hoy y los próximos 5 días | No |
| franja | Texto | Sí | Solo esos valores | No |
| atencion | Texto | Sí | Según el oficio | No |
| direccionId | ObjectId | No | Obligatoria si es a domicilio | No |
| colonia | Texto | Sí | Debe existir en colonias | Compuesto |
| modo | Texto | Sí | uno si nace en un perfil o en Volver a contratar | No |
| tecnicoDirigidoId | ObjectId | No | Obligatorio si modo es uno | No |
| oleadas | Lista de documentos | No | Máximo 5 técnicos en la primera | No |
| estado | Texto | Sí | Solo transiciones válidas | Compuesto |
| cotizacionElegidaId | ObjectId | No | Solo una | No |
| idempotencyKey | Texto | Sí | Evita duplicados si la app repite la petición | Único |
| venceEn | Fecha y hora UTC | Sí | Lo calcula el servidor | Sí |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | Compuesto |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Compuesto | clienteId + estado + creadoEn (descendente) | Lista Actividad del cliente |
| Compuesto | estado + venceEn | Proceso que vence las solicitudes a las 48 horas |
| Compuesto | colonia + oficio + estado | Solicitudes nuevas que ve un técnico de su zona |
| Único | idempotencyKey | Idempotencia |

**Colección `cotizaciones`.** Es la respuesta de un técnico a una solicitud: tipo de precio, monto, tope, día y hora propuestos y una nota. Hay tres tipos: precio fijo (el arreglo completo, con refacciones aparte), rango (un estimado) y visita previa (Ver primero). El cliente compara hasta cinco y elige una; al elegir, la cita queda confirmada. Cada tarjeta explica qué cubre su precio, y Mejor precio nunca se asigna a una vis…

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| solicitudId | ObjectId | Sí | Debe existir y estar abierta | Compuesto único |
| tecnicoId | ObjectId | Sí | Debe estar en la oleada o ser el dirigido | Compuesto único |
| tipo | Texto | Sí | Solo esos valores | No |
| precioMin | Entero | Sí | Mayor que 0. Visita: máximo 250 | No |
| precioMax | Entero | No | Mayor o igual que precioMin | No |
| tope | Entero | Sí | Lo calcula el servidor según el oficio | No |
| dia | Fecha | Sí | Hoy o posterior | No |
| hora | Texto | Sí | Formato HH:MM | No |
| nota | Texto | No | Máximo 300 caracteres. Moderada | No |
| queCubre | Texto | Sí | Lo asigna el servidor según el tipo | No |
| estado | Texto | Sí | Solo una elegida por solicitud | Compuesto |
| cotizacionFinal | Documento | No | Solo si el tipo es visita | No |
| idempotencyKey | Texto | Sí | Idempotencia | Único |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Único compuesto | solicitudId + tecnicoId | Un técnico solo cotiza una vez por solicitud |
| Compuesto | tecnicoId + estado + creadoEn | Cotizaciones enviadas del técnico |
| Normal | solicitudId + estado | Comparar las cotizaciones de una solicitud |
| Único | idempotencyKey | Idempotencia |

## 7. Arquitectura y componentes
- Proyecto `Knock.Market.Api`, módulo `Requests` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 020 de este repositorio.
- Spec 021 de este repositorio.
- Spec 030 de este repositorio.
- Spec 032 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 038 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Oleadas y tarea `RepartirOleada` de la spec 028 (hoy no implementadas)
  - Bandeja de salida de la spec 054 para el evento `CotizacionElegida` (hoy no implementada)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-03, CA-09, CA-14, CA-22 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-03, CA-09, CA-14, CA-22 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-04, CA-10, CA-15 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-08, CA-13 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-24 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-23 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-25 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_048_NN` en el nombre (para `python scripts/sdd_lint.py --trace 048`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-04, CA-08 a CA-10, CA-13 a CA-15, CA-22, CA-24) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Ampliar:** el cliente ve pocas cotizaciones a los 20 minutos → toca «Pedir también a otros» → se reparte la siguiente oleada → si ya la había ampliado, ve que no se puede otra vez.
- **Cotizar con referencia:** el técnico abre el formulario → ve el rango de mercado de su oficio → envía su precio.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 048` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** «Ampliar» no inventa un límite propio: usa las oleadas de la spec 028 (máximo 3), con lo que el reparto automático y el manual siguen una sola regla.
- **D-05:** SOL-08 se elimina porque el detalle de la spec 020 ya entrega los enlaces de 5 minutos; evita dos rutas para lo mismo.
- **D-06:** La referencia de mercado exige 10 cotizaciones aceptadas en 90 días, como dice el Excel; con menos, muestra el rango general de la categoría.
- **D-07:** CNV-05 no es una ruta: se cubre con el evento de la bandeja de salida (spec 054).

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Ampliar: antes «una sola vez»; ahora adelanta la siguiente oleada de la spec 028 (máximo 3).
- **C-02:** Archivos (SOL-08): antes una ruta aparte; ahora se cubre con el detalle de la spec 020.
- **C-03:** Estados de COT-05: antes nombres propios; ahora los de la spec 020.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: oleadas de la spec 028, enlaces de la spec 020 y el evento `CotizacionElegida` por la bandeja de salida (spec 054, H-1).

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
