# Spec 049 — Cotización final tras la visita y ajustes de precio

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints COT-06, COT-07, AJU-01, AJU-02.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Mercado (`Knock.Market.Api`) · **Módulo:** `FinalQuotes` · **Colecciones:** `cotizaciones`, `ajustes_precio`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 039)

## 1. Objetivo
Definir y especificar los endpoints COT-06, COT-07, AJU-01, AJU-02 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como técnico, quiero enviar la cotización final tras ver el trabajo o pedir un ajuste de precio, y como cliente quiero aceptarlo o rechazarlo, para acordar el costo real sin sorpresas.

## 2. Historia y alcance
**2.1 Historia**
- Como técnico, quiero enviar la cotización final tras ver el trabajo o pedir un ajuste de precio, y como cliente quiero aceptarlo o rechazarlo, para acordar el costo real sin sorpresas.

**2.2 Dentro**
- `POST /api/v1/visitas/{id}/cotizacion-final` (COT-06): Después de la visita previa, el técnico envía la cotización final.
- `POST /api/v1/visitas/{id}/cotizacion-final/decision` (COT-07): El cliente aprueba o rechaza la cotización final. Si rechaza, paga solo la visita.
- `POST /api/v1/cotizaciones/{id}/ajuste-precio` (AJU-01): El técnico propone un cambio sobre el tope, con motivo.
- `POST /api/v1/ajustes/{id}/decision` (AJU-02): El cliente aprueba o rechaza.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 039.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `POST /api/v1/visitas/{id}/cotizacion-final` (crear (acción)): Después de la visita previa, el técnico envía la cotización final. Lo puede llamar: cuenta con perfil de técnico. Responde 201 cuando sale bien.
- **R-02:** Entrada de `POST /api/v1/visitas/{id}/cotizacion-final`: Ruta: id. Cuerpo: precio, descripcion. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-03:** Estados de `POST /api/v1/visitas/{id}/cotizacion-final`: 409 `final_quote_not_allowed`. Los códigos de negocio van en inglés y en snake_case.
- **R-04:** `POST /api/v1/visitas/{id}/cotizacion-final/decision` (actualizar (acción)): El cliente aprueba o rechaza la cotización final. Si rechaza, paga solo la visita. Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 200 cuando sale bien.
- **R-05:** Entrada de `POST /api/v1/visitas/{id}/cotizacion-final/decision`: Ruta: id. Cuerpo: decision. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-06:** Estados de `POST /api/v1/visitas/{id}/cotizacion-final/decision`: 409 `final_quote_already_decided`. Los códigos de negocio van en inglés y en snake_case.
- **R-07:** `POST /api/v1/cotizaciones/{id}/ajuste-precio` (crear): El técnico propone un cambio sobre el tope, con motivo. Lo puede llamar: cuenta con perfil de técnico. Responde 201 cuando sale bien.
- **R-08:** Entrada de `POST /api/v1/cotizaciones/{id}/ajuste-precio`: Ruta: id. Cuerpo: montoNuevo, motivo. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `montoNuevo`: Mayor que el tope en al menos 100 pesos; `motivo`: Mínimo 10 caracteres. Moderado.
- **R-09:** Estados de `POST /api/v1/cotizaciones/{id}/ajuste-precio`: 409 `adjustment_not_allowed`. Los códigos de negocio van en inglés y en snake_case.
- **R-10:** `POST /api/v1/ajustes/{id}/decision` (actualizar (acción)): El cliente aprueba o rechaza. Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 200 cuando sale bien.
- **R-11:** Entrada de `POST /api/v1/ajustes/{id}/decision`: Ruta: id. Cuerpo: decision. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-12:** Estados de `POST /api/v1/ajustes/{id}/decision`: 404; 409 `adjustment_already_decided`. Los códigos de negocio van en inglés y en snake_case.
- **R-13:** **Cotización final (COT-06).** Solo la envía el técnico de la visita, una vez terminada la revisión previa y **una sola** por visita. `precio` es un entero positivo en pesos (sin tope de negocio) y `descripcion` tiene de 8 a 500 caracteres. El cliente la ve para aprobarla o rechazarla.
- **R-14:** **Decisión sobre la cotización final (COT-07).** `decision` es `aprueba` o `rechaza` y es definitiva. Si aprueba, el trabajo sigue con ese precio; si rechaza, la visita queda `terminada` con `resultado = solo_visita` y la cotización final `rechazada`: el cobro de la visita lo define la historia de cobro posterior (spec 020, decisión 8) y esta spec solo registra el resultado. Tiene vigencia de 48 horas (tarea `VencerCotizacionFinal`, spec 054): sin respuesta queda rechazada por vencimiento.
- **R-15:** **Ajuste de precio (AJU-01).** El técnico propone un `montoNuevo` mayor que el tope de la cotización con un `motivo` de 8 a 300 caracteres; solo puede haber un ajuste pendiente por cotización, y el cálculo del tope y su aprobación son los de la spec 020 (`CotizacionTopeCalculator`).
- **R-16:** **Decisión sobre el ajuste (AJU-02).** `decision` es `aprueba` o `rechaza` y es definitiva, con vigencia de 24 horas (tarea `VencerAjuste`, spec 054): sin respuesta queda rechazada por vencimiento y el precio sigue siendo el tope aprobado.
- **R-17:** **Avisos.** Cada cotización final y cada ajuste crea un aviso para el cliente, y cada decisión, uno para el técnico, por la bandeja de salida (specs 037 y 054); los avisos no llevan precios ni textos escritos por las personas.
- **R-18:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-19:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-20:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-21:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/visitas/{id}/cotizacion-final` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/cotizacion-final`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/visitas/{id}/cotizacion-final`, entonces responde 403 y no cambia nada.
- **CA-04:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/cotizacion-final`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-05:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/visitas/{id}/cotizacion-final`, entonces responde 400 y no guarda nada.
- **CA-06:** Dado el caso «la revisión previa no terminó o ya existe una cotización final de esa visita», cuando llama `POST /api/v1/visitas/{id}/cotizacion-final`, entonces responde 409 `final_quote_not_allowed` con ProblemDetails y no cambia el estado.
- **CA-07:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/visitas/{id}/cotizacion-final/decision` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-08:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/cotizacion-final/decision`, entonces responde 401 y no ejecuta la operación.
- **CA-09:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/visitas/{id}/cotizacion-final/decision`, entonces responde 403 y no cambia nada.
- **CA-10:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/cotizacion-final/decision`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-11:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/visitas/{id}/cotizacion-final/decision`, entonces responde 400 y no guarda nada.
- **CA-12:** Dado el caso «no hay una cotización final pendiente o ya fue decidida o venció», cuando llama `POST /api/v1/visitas/{id}/cotizacion-final/decision`, entonces responde 409 `final_quote_already_decided` con ProblemDetails y no cambia el estado.
- **CA-13:** Dado el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/cotizaciones/{id}/ajuste-precio` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-14:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/cotizaciones/{id}/ajuste-precio`, entonces responde 401 y no ejecuta la operación.
- **CA-15:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/cotizaciones/{id}/ajuste-precio`, entonces responde 403 y no cambia nada.
- **CA-16:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/cotizaciones/{id}/ajuste-precio`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-17:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/cotizaciones/{id}/ajuste-precio`, entonces responde 400 y no guarda nada.
- **CA-18:** Dado el caso «ya hay un ajuste pendiente o la cotización no está aceptada», cuando llama `POST /api/v1/cotizaciones/{id}/ajuste-precio`, entonces responde 409 `adjustment_not_allowed` con ProblemDetails y no cambia el estado.
- **CA-19:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/ajustes/{id}/decision` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-20:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/ajustes/{id}/decision`, entonces responde 401 y no ejecuta la operación.
- **CA-21:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/ajustes/{id}/decision`, entonces responde 403 y no cambia nada.
- **CA-22:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/ajustes/{id}/decision`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-23:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/ajustes/{id}/decision`, entonces responde 400 y no guarda nada.
- **CA-24:** Dado el caso «recurso no disponible», cuando llama `POST /api/v1/ajustes/{id}/decision`, entonces responde 404 con ProblemDetails y no cambia el estado.
- **CA-25:** Dado el caso «el ajuste ya fue decidido o venció», cuando llama `POST /api/v1/ajustes/{id}/decision`, entonces responde 409 `adjustment_already_decided` con ProblemDetails y no cambia el estado.
- **CA-26:** Dado el técnico de una visita con revisión previa terminada, cuando envía precio y descripción válidos, entonces se crea la cotización final y el cliente recibe un aviso; una segunda responde 409 `final_quote_not_allowed`.
- **CA-27:** Dada una cotización final pendiente, cuando el cliente aprueba o rechaza, entonces queda decidida y no puede cambiarse; si rechaza, la visita queda `terminada` con `resultado = solo_visita`; y si pasan 48 horas sin respuesta, se trata como rechazada por vencimiento.
- **CA-28:** Dada una cotización aceptada, cuando el técnico propone un monto nuevo con motivo válido, entonces se crea el ajuste pendiente; con un ajuste ya pendiente responde 409 `adjustment_not_allowed`.
- **CA-29:** Dado un ajuste pendiente, cuando el cliente decide o pasan 24 horas, entonces queda aprobado, rechazado o vencido y el precio de la cotización cambia solo si se aprobó.
- **CA-30:** Dada una cotización final, un ajuste o su decisión, cuando se genera el aviso, entonces el evento solo lleva identificadores, sin precios ni texto escrito.
- **CA-31:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-32:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-33:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-34:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec049Tests.Criterio01_CA_049_01` |
| CA-02 | `Spec049Tests.Criterio02_CA_049_02` |
| CA-03 | `Spec049Tests.Criterio03_CA_049_03` |
| CA-04 | `Spec049Tests.Criterio04_CA_049_04` |
| CA-05 | `Spec049Tests.Criterio05_CA_049_05` |
| CA-06 | `Spec049Tests.Criterio06_CA_049_06` |
| CA-07 | `Spec049Tests.Criterio07_CA_049_07` |
| CA-08 | `Spec049Tests.Criterio08_CA_049_08` |
| CA-09 | `Spec049Tests.Criterio09_CA_049_09` |
| CA-10 | `Spec049Tests.Criterio10_CA_049_10` |
| CA-11 | `Spec049Tests.Criterio11_CA_049_11` |
| CA-12 | `Spec049Tests.Criterio12_CA_049_12` |
| CA-13 | `Spec049Tests.Criterio13_CA_049_13` |
| CA-14 | `Spec049Tests.Criterio14_CA_049_14` |
| CA-15 | `Spec049Tests.Criterio15_CA_049_15` |
| CA-16 | `Spec049Tests.Criterio16_CA_049_16` |
| CA-17 | `Spec049Tests.Criterio17_CA_049_17` |
| CA-18 | `Spec049Tests.Criterio18_CA_049_18` |
| CA-19 | `Spec049Tests.Criterio19_CA_049_19` |
| CA-20 | `Spec049Tests.Criterio20_CA_049_20` |
| CA-21 | `Spec049Tests.Criterio21_CA_049_21` |
| CA-22 | `Spec049Tests.Criterio22_CA_049_22` |
| CA-23 | `Spec049Tests.Criterio23_CA_049_23` |
| CA-24 | `Spec049Tests.Criterio24_CA_049_24` |
| CA-25 | `Spec049Tests.Criterio25_CA_049_25` |
| CA-26 | `Spec049Tests.Criterio26_CA_049_26` |
| CA-27 | `Spec049Tests.Criterio27_CA_049_27` |
| CA-28 | `Spec049Tests.Criterio28_CA_049_28` |
| CA-29 | `Spec049Tests.Criterio29_CA_049_29` |
| CA-30 | `Spec049Tests.Criterio30_CA_049_30` |
| CA-31 | `Spec049Tests.Criterio31_CA_049_31` |
| CA-32 | `Spec049Tests.Criterio32_CA_049_32` |
| CA-33 | `Spec049Tests.Criterio33_CA_049_33` |
| CA-34 | `Spec049Tests.Criterio34_CA_049_34` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| COT-06 | `POST /api/v1/visitas/{id}/cotizacion-final` | cuenta con perfil de técnico | Ruta: id. Cuerpo: precio, descripcion | 201; 400; 401; 403; 409 |
| COT-07 | `POST /api/v1/visitas/{id}/cotizacion-final/decision` | persona con sesión que sea dueña del recurso | Ruta: id. Cuerpo: decision | 200; 400; 401; 403; 409 |
| AJU-01 | `POST /api/v1/cotizaciones/{id}/ajuste-precio` | cuenta con perfil de técnico | Ruta: id. Cuerpo: montoNuevo, motivo | 201; 400; 401; 403; 409; 422 |
| AJU-02 | `POST /api/v1/ajustes/{id}/decision` | persona con sesión que sea dueña del recurso | Ruta: id. Cuerpo: decision | 200; 400; 401; 403; 404; 409 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| COT-06 | No aplica | No | No |
| COT-07 | No aplica | No | No |
| AJU-01 | No aplica | No | No |
| AJU-02 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| COT-06 | 409 | `final_quote_not_allowed` | la revisión previa no terminó o ya existe una cotización final de esa visita |
| COT-07 | 409 | `final_quote_already_decided` | no hay una cotización final pendiente o ya fue decidida o venció |
| AJU-01 | 409 | `adjustment_not_allowed` | ya hay un ajuste pendiente o la cotización no está aceptada |
| AJU-02 | 409 | `adjustment_already_decided` | el ajuste ya fue decidido o venció |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
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

**Colección `ajustes_precio`.** Cuando el técnico descubre un problema que obliga a cobrar más que el tope de la cotización, propone un cambio con un motivo. Lo propone cuando lo detecta y no al terminar el trabajo. El cliente lo aprueba o lo rechaza. Si lo rechaza, el trabajo se cancela sin costo, o, si ya hubo visita, solo se paga la visita. Nunca se pide aprobar una diferencia menor a 100 pesos

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| visitaId | ObjectId | Sí | Debe existir y estar en curso | Compuesto |
| cotizacionId | ObjectId | Sí | Debe existir | No |
| montoNuevo | Entero | Sí | Mayor que el tope en al menos 100 pesos | No |
| motivo | Texto | Sí | Mínimo 10 caracteres. Moderado | No |
| estado | Texto | Sí | Solo esos valores | Compuesto |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |
| decididoEn | Fecha y hora UTC | No |  | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Compuesto | visitaId + estado | Ver si hay un cambio pendiente en una visita |

**Campos nuevos opcionales en `visitas` (spec 021).** `resultado` (`solo_visita` cuando el cliente rechaza la cotización final) y `cotizacionFinal` embebida `{ precio, descripcion, estado (enviada, aprobada, rechazada, vencida), venceEn }`. Son aditivos: no cambian los estados ni las reglas de la spec 021.

## 7. Arquitectura y componentes
- Proyecto `Knock.Market.Api`, módulo `FinalQuotes` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 020 de este repositorio.
- Spec 021 de este repositorio.
- Spec 022 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 039 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Tareas de vencimiento del ejecutor de la spec 054 (hoy no implementado)
  - Topes de la spec 020 (su dominio ya está en `sprint-04`)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-03, CA-09, CA-15, CA-21, CA-31 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-03, CA-09, CA-15, CA-21, CA-31 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-04 a CA-05, CA-10 a CA-11, CA-16 a CA-17, CA-22 a CA-23 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-08, CA-14, CA-20 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-33 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-32 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-34 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_049_NN` en el nombre (para `python scripts/sdd_lint.py --trace 049`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-05, CA-08 a CA-11, CA-14 a CA-17, CA-20 a CA-23, CA-31, CA-33) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Cotización final:** el técnico termina la revisión previa → envía precio y descripción → el cliente recibe el aviso, la revisa y aprueba → el trabajo continúa con ese precio.
- **Rechazo:** el cliente rechaza la cotización final → la visita queda `terminada` con `resultado = solo_visita`.
- **Ajuste:** el técnico pide subir el precio sobre el tope con su motivo → el cliente aprueba en 24 horas o el ajuste vence y se mantiene el tope.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 049` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** Vigencias de 48 horas (cotización final, igual que la solicitud de la spec 020) y de 24 horas (ajuste, una decisión rápida sobre un cambio pequeño): evitan que algo quede pendiente indefinidamente.
- **D-05:** Sin precio máximo funcional: un tope arbitrario bloquearía trabajos grandes legítimos; la aprobación del cliente es el control.
- **D-06:** El rechazo de la cotización final deja `resultado = solo_visita` y no crea un estado de pago: el cobro lo define su historia posterior.
- **D-07:** El tope sobre el que se pide el ajuste es el de la spec 020 (precio fijo más 10 %; rango más 20 % o 25 % según el oficio).

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Estado `visita_pagada`: antes un estado nuevo; ahora `resultado = solo_visita` (la spec 020 deja el cobro para una historia posterior).
- **C-02:** Precio máximo de 100 000 pesos: eliminado.
- **C-03:** Tope: la duda citaba 10 % para todo; la spec 020 R-14 fija 10 % para precio fijo y 20 % o 25 % para rangos.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: tareas de vencimiento de la spec 054 y los topes de la spec 020. El cobro de la visita queda fuera de esta spec.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
