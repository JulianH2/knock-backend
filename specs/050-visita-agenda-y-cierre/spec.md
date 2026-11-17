# Spec 050 — Visita: agenda, reprogramación, cancelación, cliente ausente, cierre y permiso de fotos

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints VIS-02, VIS-04, VIS-05, VIS-06, VIS-07, VIS-09, VIS-10, VIS-11.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Mercado (`Knock.Market.Api`) · **Módulo:** `Visits` · **Colecciones:** `visitas`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 040)

## 1. Objetivo
Definir y especificar los endpoints VIS-02, VIS-04, VIS-05, VIS-06, VIS-07, VIS-09, VIS-10, VIS-11 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como cliente o técnico, quiero ver mi agenda y poder reprogramar, cancelar, marcar ausencia, terminar o dejar el trabajo sin terminar, para cerrar cada visita con claridad.

## 2. Historia y alcance
**2.1 Historia**
- Como cliente o técnico, quiero ver mi agenda y poder reprogramar, cancelar, marcar ausencia, terminar o dejar el trabajo sin terminar, para cerrar cada visita con claridad.

**2.2 Dentro**
- `GET /api/v1/agenda` (VIS-02): Agenda del técnico: próximas visitas y solicitudes nuevas (Mi agenda).
- `POST /api/v1/visitas/{id}/reprogramar` (VIS-04): Una parte propone otra fecha.
- `POST /api/v1/visitas/{id}/reprogramar/respuesta` (VIS-05): La otra parte confirma o rechaza.
- `POST /api/v1/visitas/{id}/cancelar` (VIS-06): Cancela con motivo obligatorio. Gratis con más de 24 h; menos queda como cancelación tardía y se avisa de inmediato.
- `POST /api/v1/visitas/{id}/cliente-ausente` (VIS-07): El técnico registra que el cliente no estaba, sin penalizar. Solo dentro de la ventana de la cita.
- `POST /api/v1/visitas/{id}/terminar` (VIS-09): El técnico termina con fotos (mínimo 1) y qué hizo. No hay confirmación del cliente.
- `POST /api/v1/visitas/{id}/no-terminado` (VIS-10): El cliente reporta que no se terminó (hasta 7 días después).
- `PUT /api/v1/visitas/{id}/permiso-fotos` (VIS-11): El cliente autoriza o no publicar las fotos del trabajo.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 040.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `GET /api/v1/agenda` (leer todos / filtro): Agenda del técnico: próximas visitas y solicitudes nuevas (Mi agenda). Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-02:** Entrada de `GET /api/v1/agenda`: Query: desde, hasta. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-03:** `GET /api/v1/agenda` pagina por cursor (Por cursor, 20): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Filtro por fechas; orden por hora.
- **R-04:** `POST /api/v1/visitas/{id}/reprogramar` (actualizar (acción)): Una parte propone otra fecha. Lo puede llamar: persona con sesión que sea participante del recurso. Responde 200 cuando sale bien.
- **R-05:** Entrada de `POST /api/v1/visitas/{id}/reprogramar`: Ruta: id. Cuerpo: fechaHora. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `fechaHora`: Futuro al crearse.
- **R-06:** Estados de `POST /api/v1/visitas/{id}/reprogramar`: 409 `visit_not_reschedulable`. Los códigos de negocio van en inglés y en snake_case.
- **R-07:** `POST /api/v1/visitas/{id}/reprogramar/respuesta` (actualizar (acción)): La otra parte confirma o rechaza. Lo puede llamar: persona con sesión que sea participante del recurso. Responde 200 cuando sale bien.
- **R-08:** Entrada de `POST /api/v1/visitas/{id}/reprogramar/respuesta`: Ruta: id. Cuerpo: acepta. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-09:** Estados de `POST /api/v1/visitas/{id}/reprogramar/respuesta`: 409 `no_pending_proposal`. Los códigos de negocio van en inglés y en snake_case.
- **R-10:** `POST /api/v1/visitas/{id}/cancelar` (actualizar (acción)): Cancela con motivo obligatorio. Gratis con más de 24 h; menos queda como cancelación tardía y se avisa de inmediato. Lo puede llamar: persona con sesión que sea participante del recurso. Responde 200 cuando sale bien.
- **R-11:** Entrada de `POST /api/v1/visitas/{id}/cancelar`: Ruta: id. Cuerpo: motivo. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-12:** Estados de `POST /api/v1/visitas/{id}/cancelar`: 409 `visit_already_closed`. Los códigos de negocio van en inglés y en snake_case.
- **R-13:** `POST /api/v1/visitas/{id}/cliente-ausente` (actualizar (acción)): El técnico registra que el cliente no estaba, sin penalizar. Solo dentro de la ventana de la cita. Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-14:** Entrada de `POST /api/v1/visitas/{id}/cliente-ausente`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-15:** Estados de `POST /api/v1/visitas/{id}/cliente-ausente`: 409 `outside_arrival_window`. Los códigos de negocio van en inglés y en snake_case.
- **R-16:** `POST /api/v1/visitas/{id}/terminar` (actualizar (acción)): El técnico termina con fotos (mínimo 1) y qué hizo. No hay confirmación del cliente. Lo puede llamar: cuenta con perfil de técnico. Responde 200 cuando sale bien.
- **R-17:** Entrada de `POST /api/v1/visitas/{id}/terminar`: Ruta: id. Cuerpo: descripcion, recomendacion, fotos. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-18:** Estados de `POST /api/v1/visitas/{id}/terminar`: 409 `visit_not_in_progress`. Los códigos de negocio van en inglés y en snake_case.
- **R-19:** `POST /api/v1/visitas/{id}/no-terminado` (crear (acción)): El cliente reporta que no se terminó (hasta 7 días después). Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 201 cuando sale bien.
- **R-20:** Entrada de `POST /api/v1/visitas/{id}/no-terminado`: Ruta: id. Cuerpo: texto. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-21:** Estados de `POST /api/v1/visitas/{id}/no-terminado`: 409 `report_window_closed`. Los códigos de negocio van en inglés y en snake_case.
- **R-22:** `PUT /api/v1/visitas/{id}/permiso-fotos` (actualizar): El cliente autoriza o no publicar las fotos del trabajo. Lo puede llamar: persona con sesión que sea dueña del recurso. Responde 200 cuando sale bien.
- **R-23:** Entrada de `PUT /api/v1/visitas/{id}/permiso-fotos`: Ruta: id. Cuerpo: permiso. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-24:** **Agenda (VIS-02).** Solo el técnico; devuelve sus visitas y las solicitudes nuevas abiertas entre `desde` y `hasta` (por omisión hoy y los 14 días siguientes; máximo 31 días de rango), ordenadas por fecha en UTC. Un rango mayor o con `hasta` anterior a `desde` responde 422.
- **R-25:** **Reprogramar (VIS-04 y VIS-05).** Una parte propone una `fechaHora` futura (al menos 2 horas adelante y como máximo a 30 días); solo puede haber una propuesta pendiente por visita; la otra parte tiene 24 horas, o hasta 2 horas antes de la cita si es menos, para aceptar o rechazar (tarea `VencerPropuestaDeReprogramacion`, spec 054); sin respuesta la propuesta vence y la cita se mantiene; como máximo 2 reprogramaciones aceptadas por visita para evitar ciclos, y después queda cancelar.
- **R-26:** **Cancelar (VIS-06).** `motivo` de 8 a 300 caracteres, obligatorio. Con más de 24 horas de anticipación es gratis; con menos queda `cancelada` con `tardia: true` y se avisa de inmediato a la otra parte. No hay penalización económica (el Excel no la define).
- **R-27:** **Cliente ausente (VIS-07).** Solo el técnico, desde 15 minutos después de la hora de la cita y hasta 2 horas después; marca la visita `no_presentado` (estado de la spec 021) con `quienFalto = cliente`, sin penalizar, y avisa al cliente.
- **R-28:** **Terminar (VIS-09).** Solo el técnico con la visita `en_curso`; `descripcion` de 8 a 500 caracteres, `recomendacion` opcional de hasta 300 y de 1 a 5 `fotos` (privadas, spec 032). No requiere confirmación del cliente. La visita queda `terminada` y se programan `CerrarPeriodoDeOpinion` (30 días) y `CerrarReporteDeNoTerminado` (7 días) (spec 054).
- **R-29:** **No terminado (VIS-10).** Solo el cliente dueño y dentro de los 7 días siguientes a terminada la visita; `texto` de 8 a 500 caracteres; una sola vez por visita. Crea un reporte que pasa a revisión (spec 053) y responde 201.
- **R-30:** **Permiso de fotos (VIS-11).** El cliente dueño autoriza o no (`permiso`, booleano; por omisión, no). Las fotos del trabajo solo se publican en el perfil del técnico si hay permiso y pasaron la moderación (specs 031 y 032); revocar el permiso las retira.
- **R-31:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-32:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-33:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-34:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/agenda` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/agenda`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `GET /api/v1/agenda`, entonces responde 403 y no cambia nada.
- **CA-04:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/agenda`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-05:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-06:** Dado el rol «persona con sesión que sea participante del recurso», cuando llama `POST /api/v1/visitas/{id}/reprogramar` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-07:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/reprogramar`, entonces responde 401 y no ejecuta la operación.
- **CA-08:** Dada una persona que no cumple el rol «persona con sesión que sea participante del recurso», cuando llama `POST /api/v1/visitas/{id}/reprogramar`, entonces responde 403 y no cambia nada.
- **CA-09:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/reprogramar`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-10:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/visitas/{id}/reprogramar`, entonces responde 400 y no guarda nada.
- **CA-11:** Dado el caso «la visita ya empezó o terminó, ya hay una propuesta pendiente o ya se reprogramó dos veces», cuando llama `POST /api/v1/visitas/{id}/reprogramar`, entonces responde 409 `visit_not_reschedulable` con ProblemDetails y no cambia el estado.
- **CA-12:** Dado el rol «persona con sesión que sea participante del recurso», cuando llama `POST /api/v1/visitas/{id}/reprogramar/respuesta` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-13:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/reprogramar/respuesta`, entonces responde 401 y no ejecuta la operación.
- **CA-14:** Dada una persona que no cumple el rol «persona con sesión que sea participante del recurso», cuando llama `POST /api/v1/visitas/{id}/reprogramar/respuesta`, entonces responde 403 y no cambia nada.
- **CA-15:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/reprogramar/respuesta`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-16:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/visitas/{id}/reprogramar/respuesta`, entonces responde 400 y no guarda nada.
- **CA-17:** Dado el caso «no hay una propuesta pendiente o ya venció», cuando llama `POST /api/v1/visitas/{id}/reprogramar/respuesta`, entonces responde 409 `no_pending_proposal` con ProblemDetails y no cambia el estado.
- **CA-18:** Dado el rol «persona con sesión que sea participante del recurso», cuando llama `POST /api/v1/visitas/{id}/cancelar` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-19:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/cancelar`, entonces responde 401 y no ejecuta la operación.
- **CA-20:** Dada una persona que no cumple el rol «persona con sesión que sea participante del recurso», cuando llama `POST /api/v1/visitas/{id}/cancelar`, entonces responde 403 y no cambia nada.
- **CA-21:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/cancelar`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-22:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/visitas/{id}/cancelar`, entonces responde 400 y no guarda nada.
- **CA-23:** Dado el caso «la visita ya terminó o ya fue cancelada», cuando llama `POST /api/v1/visitas/{id}/cancelar`, entonces responde 409 `visit_already_closed` con ProblemDetails y no cambia el estado.
- **CA-24:** Dado el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/visitas/{id}/cliente-ausente` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-25:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/cliente-ausente`, entonces responde 401 y no ejecuta la operación.
- **CA-26:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/visitas/{id}/cliente-ausente`, entonces responde 403 y no cambia nada.
- **CA-27:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/cliente-ausente`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-28:** Dado el caso «fuera de la ventana de la cita o la visita ya empezó», cuando llama `POST /api/v1/visitas/{id}/cliente-ausente`, entonces responde 409 `outside_arrival_window` con ProblemDetails y no cambia el estado.
- **CA-29:** Dado el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/visitas/{id}/terminar` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-30:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/terminar`, entonces responde 401 y no ejecuta la operación.
- **CA-31:** Dada una persona que no cumple el rol «cuenta con perfil de técnico», cuando llama `POST /api/v1/visitas/{id}/terminar`, entonces responde 403 y no cambia nada.
- **CA-32:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/terminar`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-33:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/visitas/{id}/terminar`, entonces responde 400 y no guarda nada.
- **CA-34:** Dado el caso «la visita no está en curso», cuando llama `POST /api/v1/visitas/{id}/terminar`, entonces responde 409 `visit_not_in_progress` con ProblemDetails y no cambia el estado.
- **CA-35:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/visitas/{id}/no-terminado` con datos válidos, entonces responde 201 con lo descrito en §5.
- **CA-36:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/visitas/{id}/no-terminado`, entonces responde 401 y no ejecuta la operación.
- **CA-37:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `POST /api/v1/visitas/{id}/no-terminado`, entonces responde 403 y no cambia nada.
- **CA-38:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/visitas/{id}/no-terminado`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-39:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/visitas/{id}/no-terminado`, entonces responde 400 y no guarda nada.
- **CA-40:** Dado el caso «pasaron más de 7 días de terminada o ya se reportó», cuando llama `POST /api/v1/visitas/{id}/no-terminado`, entonces responde 409 `report_window_closed` con ProblemDetails y no cambia el estado.
- **CA-41:** Dado el rol «persona con sesión que sea dueña del recurso», cuando llama `PUT /api/v1/visitas/{id}/permiso-fotos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-42:** Dada una petición sin token o con token vencido, cuando llama `PUT /api/v1/visitas/{id}/permiso-fotos`, entonces responde 401 y no ejecuta la operación.
- **CA-43:** Dada una persona que no cumple el rol «persona con sesión que sea dueña del recurso», cuando llama `PUT /api/v1/visitas/{id}/permiso-fotos`, entonces responde 403 y no cambia nada.
- **CA-44:** Dado un valor inválido según las reglas de §6, cuando llama `PUT /api/v1/visitas/{id}/permiso-fotos`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-45:** Dado un técnico con visitas, cuando consulta su agenda, entonces recibe solo las suyas dentro del rango y ordenadas; un rango mayor de 31 días o invertido responde 422.
- **CA-46:** Dada una visita confirmada, cuando una parte propone una fecha válida y la otra acepta, entonces la cita cambia; si rechaza o vence, la cita no cambia; una tercera reprogramación aceptada responde 409 `visit_not_reschedulable`.
- **CA-47:** Dada una visita con más de 24 horas, cuando una parte cancela con motivo, entonces queda `cancelada`; con menos de 24 horas queda `cancelada` con `tardia: true` y la otra parte recibe aviso inmediato.
- **CA-48:** Dado el técnico dentro de la ventana de la cita, cuando registra la ausencia, entonces la visita queda `no_presentado` con `quienFalto = cliente` y el cliente recibe aviso; antes de los 15 minutos o después de las 2 horas responde 409 `outside_arrival_window`.
- **CA-49:** Dada una visita en curso, cuando el técnico la termina con descripción y al menos una foto, entonces queda `terminada` y se programan los dos cierres; sin foto responde 422.
- **CA-50:** Dada una visita terminada hace menos de 7 días, cuando el cliente reporta que no se terminó, entonces se crea el reporte; pasados 7 días o con un reporte previo responde 409 `report_window_closed`.
- **CA-51:** Dado el cliente dueño, cuando autoriza o revoca el permiso, entonces las fotos se publican solo con permiso y moderación, y se retiran al revocarlo.
- **CA-52:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-53:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-54:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-55:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec050Tests.Criterio01_CA_050_01` |
| CA-02 | `Spec050Tests.Criterio02_CA_050_02` |
| CA-03 | `Spec050Tests.Criterio03_CA_050_03` |
| CA-04 | `Spec050Tests.Criterio04_CA_050_04` |
| CA-05 | `Spec050Tests.Criterio05_CA_050_05` |
| CA-06 | `Spec050Tests.Criterio06_CA_050_06` |
| CA-07 | `Spec050Tests.Criterio07_CA_050_07` |
| CA-08 | `Spec050Tests.Criterio08_CA_050_08` |
| CA-09 | `Spec050Tests.Criterio09_CA_050_09` |
| CA-10 | `Spec050Tests.Criterio10_CA_050_10` |
| CA-11 | `Spec050Tests.Criterio11_CA_050_11` |
| CA-12 | `Spec050Tests.Criterio12_CA_050_12` |
| CA-13 | `Spec050Tests.Criterio13_CA_050_13` |
| CA-14 | `Spec050Tests.Criterio14_CA_050_14` |
| CA-15 | `Spec050Tests.Criterio15_CA_050_15` |
| CA-16 | `Spec050Tests.Criterio16_CA_050_16` |
| CA-17 | `Spec050Tests.Criterio17_CA_050_17` |
| CA-18 | `Spec050Tests.Criterio18_CA_050_18` |
| CA-19 | `Spec050Tests.Criterio19_CA_050_19` |
| CA-20 | `Spec050Tests.Criterio20_CA_050_20` |
| CA-21 | `Spec050Tests.Criterio21_CA_050_21` |
| CA-22 | `Spec050Tests.Criterio22_CA_050_22` |
| CA-23 | `Spec050Tests.Criterio23_CA_050_23` |
| CA-24 | `Spec050Tests.Criterio24_CA_050_24` |
| CA-25 | `Spec050Tests.Criterio25_CA_050_25` |
| CA-26 | `Spec050Tests.Criterio26_CA_050_26` |
| CA-27 | `Spec050Tests.Criterio27_CA_050_27` |
| CA-28 | `Spec050Tests.Criterio28_CA_050_28` |
| CA-29 | `Spec050Tests.Criterio29_CA_050_29` |
| CA-30 | `Spec050Tests.Criterio30_CA_050_30` |
| CA-31 | `Spec050Tests.Criterio31_CA_050_31` |
| CA-32 | `Spec050Tests.Criterio32_CA_050_32` |
| CA-33 | `Spec050Tests.Criterio33_CA_050_33` |
| CA-34 | `Spec050Tests.Criterio34_CA_050_34` |
| CA-35 | `Spec050Tests.Criterio35_CA_050_35` |
| CA-36 | `Spec050Tests.Criterio36_CA_050_36` |
| CA-37 | `Spec050Tests.Criterio37_CA_050_37` |
| CA-38 | `Spec050Tests.Criterio38_CA_050_38` |
| CA-39 | `Spec050Tests.Criterio39_CA_050_39` |
| CA-40 | `Spec050Tests.Criterio40_CA_050_40` |
| CA-41 | `Spec050Tests.Criterio41_CA_050_41` |
| CA-42 | `Spec050Tests.Criterio42_CA_050_42` |
| CA-43 | `Spec050Tests.Criterio43_CA_050_43` |
| CA-44 | `Spec050Tests.Criterio44_CA_050_44` |
| CA-45 | `Spec050Tests.Criterio45_CA_050_45` |
| CA-46 | `Spec050Tests.Criterio46_CA_050_46` |
| CA-47 | `Spec050Tests.Criterio47_CA_050_47` |
| CA-48 | `Spec050Tests.Criterio48_CA_050_48` |
| CA-49 | `Spec050Tests.Criterio49_CA_050_49` |
| CA-50 | `Spec050Tests.Criterio50_CA_050_50` |
| CA-51 | `Spec050Tests.Criterio51_CA_050_51` |
| CA-52 | `Spec050Tests.Criterio52_CA_050_52` |
| CA-53 | `Spec050Tests.Criterio53_CA_050_53` |
| CA-54 | `Spec050Tests.Criterio54_CA_050_54` |
| CA-55 | `Spec050Tests.Criterio55_CA_050_55` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| VIS-02 | `GET /api/v1/agenda` | cuenta con perfil de técnico | Query: desde, hasta | 200; 401; 403 |
| VIS-04 | `POST /api/v1/visitas/{id}/reprogramar` | persona con sesión que sea participante del recurso | Ruta: id. Cuerpo: fechaHora | 200; 400; 401; 403; 409 |
| VIS-05 | `POST /api/v1/visitas/{id}/reprogramar/respuesta` | persona con sesión que sea participante del recurso | Ruta: id. Cuerpo: acepta | 200; 400; 401; 403; 409 |
| VIS-06 | `POST /api/v1/visitas/{id}/cancelar` | persona con sesión que sea participante del recurso | Ruta: id. Cuerpo: motivo | 200; 400; 401; 403; 409 |
| VIS-07 | `POST /api/v1/visitas/{id}/cliente-ausente` | cuenta con perfil de técnico | Ruta: id | 200; 401; 403; 409 |
| VIS-09 | `POST /api/v1/visitas/{id}/terminar` | cuenta con perfil de técnico | Ruta: id. Cuerpo: descripcion, recomendacion, fotos | 200; 400; 401; 403; 409; 422 |
| VIS-10 | `POST /api/v1/visitas/{id}/no-terminado` | persona con sesión que sea dueña del recurso | Ruta: id. Cuerpo: texto | 201; 400; 401; 403; 409 |
| VIS-11 | `PUT /api/v1/visitas/{id}/permiso-fotos` | persona con sesión que sea dueña del recurso | Ruta: id. Cuerpo: permiso | 200; 401; 403 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| VIS-02 | Filtro por fechas; orden por hora | Por cursor, 20 | No |
| VIS-04 | No aplica | No | No |
| VIS-05 | No aplica | No | No |
| VIS-06 | No aplica | No | No |
| VIS-07 | No aplica | No | No |
| VIS-09 | No aplica | No | No |
| VIS-10 | No aplica | No | No |
| VIS-11 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| VIS-04 | 409 | `visit_not_reschedulable` | la visita ya empezó o terminó, ya hay una propuesta pendiente o ya se reprogramó dos veces |
| VIS-05 | 409 | `no_pending_proposal` | no hay una propuesta pendiente o ya venció |
| VIS-06 | 409 | `visit_already_closed` | la visita ya terminó o ya fue cancelada |
| VIS-07 | 409 | `outside_arrival_window` | fuera de la ventana de la cita o la visita ya empezó |
| VIS-09 | 409 | `visit_not_in_progress` | la visita no está en curso |
| VIS-10 | 409 | `report_window_closed` | pasaron más de 7 días de terminada o ya se reportó |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `visitas`.** Nace cuando el cliente elige una cotización. Si el servicio es a domicilio es una visita y lleva un código de 4 dígitos que el cliente pide al técnico en la puerta. Si es en taller es una cita, sin código, con día, hora y dirección del taller. Concentra la agenda, la llegada, reprogramar, cancelar, el cliente ausente y terminar el trabajo con fotos obligatorias

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| solicitudId | ObjectId | Sí | Debe existir | Sí |
| cotizacionId | ObjectId | Sí | Una por visita | Único |
| clienteId | ObjectId | Sí | Debe existir en usuarios | Compuesto |
| tecnicoId | ObjectId | Sí | Debe existir en tecnicos | Compuesto |
| tipo | Texto | Sí | Según el oficio | No |
| fechaHora | Fecha y hora UTC | Sí | Futuro al crearse | Compuesto |
| codigoHuella | Texto | No | Un solo uso, con vencimiento. Solo visita a domicilio | No |
| intentosCodigo | Entero | Sí | Se bloquea tras varios intentos | No |
| llegadaEn | Fecha y hora UTC | No |  | No |
| estado | Texto | Sí | Solo transiciones válidas | Compuesto |
| reprogramacion | Documento | No | La otra parte confirma | No |
| cancelacion | Documento | No | Motivo obligatorio | No |
| trabajo | Documento | No | Al terminar: al menos 1 foto y descripción | No |
| permisoPublicarFotos | Booleano | No | Sin permiso no van a Explorar ni al perfil | No |
| limiteNoTerminado | Fecha y hora UTC | No |  | No |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Compuesto | tecnicoId + estado + fechaHora | Agenda y Próximas visitas del técnico |
| Compuesto | clienteId + estado + fechaHora | Visitas y citas del cliente en Actividad |
| Único | cotizacionId | Una visita por cotización elegida |

**Campos nuevos opcionales en `visitas` (spec 021).** `tardia` (booleano), `quienFalto` (`cliente`), `reprogramaciones` (entero) y `propuestaReprogramacion` `{ fechaHora, propuestaPor, venceEn }`. Son aditivos: los estados siguen siendo los de la spec 021.

## 7. Arquitectura y componentes
- Proyecto `Knock.Market.Api`, módulo `Visits` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 022 de este repositorio.
- Spec 031 de este repositorio.
- Spec 032 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 040 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Ciclo de la visita de las specs 021 y 022 y tareas de la 054 (hoy no implementados)
  - Moderación e imágenes de las specs 031 y 032 (hoy no implementadas)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-03, CA-08, CA-14, CA-20, CA-26, CA-31, CA-37, CA-43, CA-52 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-03, CA-08, CA-14, CA-20, CA-26, CA-31, CA-37, CA-43, CA-52 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-04, CA-09 a CA-10, CA-15 a CA-16, CA-21 a CA-22, CA-27, CA-32 a CA-33, CA-38 a CA-39, CA-44 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-07, CA-13, CA-19, CA-25, CA-30, CA-36, CA-42 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-54 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-53 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-55 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_050_NN` en el nombre (para `python scripts/sdd_lint.py --trace 050`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-04, CA-07 a CA-10, CA-13 a CA-16, CA-19 a CA-22, CA-25 a CA-27, CA-30 a CA-33, CA-36 a CA-39, CA-42 a CA-44, CA-52, CA-54) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Reprogramar:** el cliente propone otra fecha → el técnico acepta en 24 horas → la cita cambia y ambos reciben aviso; si el técnico no responde, la cita se mantiene.
- **Cliente ausente:** el técnico llega y espera → 20 minutos después marca «Cliente ausente» dentro de la ventana → el cliente recibe aviso y no hay penalización.
- **Terminar:** el técnico termina con foto y descripción → el cliente recibe el aviso y tiene 30 días para opinar y 7 para reportar que no se terminó.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 050` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** Ventana de ausencia desde 15 minutos después de la hora de la cita hasta 2 horas después: el técnico espera un tiempo razonable antes de declarar la ausencia y deja de poder hacerlo cuando el hecho ya es antiguo.
- **D-05:** Máximo 2 reprogramaciones aceptadas y propuesta vigente 24 horas (o hasta 2 horas antes de la cita): evitan ciclos de cambios y citas sin confirmar cuando se acerca la hora.
- **D-06:** La cancelación tardía no tiene penalización económica: solo se avisa y queda registrada con `tardia: true`.
- **D-07:** Estados: se reutilizan los de la spec 021 (`confirmada`, `en_curso`, `terminada`, `cancelada`, `no_presentado`); la cancelación tardía y quién faltó son campos, no estados nuevos.
- **D-08:** La agenda se limita a 31 días por consulta para acotar el costo.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Estados: antes `cliente_ausente` y `cancelacion_tardia`; ahora `no_presentado` (spec 021) y `cancelada` con `tardia: true`.
- **C-02:** Ventana de ausencia: antes 60 minutos desde la cita; ahora de 15 minutos a 2 horas después, para que el técnico espere antes de declarar la ausencia.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: estados y ciclo de la visita de las specs 021 y 022, tareas de vencimiento de la 054 y moderación de fotos (031 y 032).

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
