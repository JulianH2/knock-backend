# Spec 053 — Panel de revisión: verificaciones de identidad y reportes

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints VER-03, VER-04, VER-05, REP-04, REP-05.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Identidad (`Knock.Identity.Api`) · **Módulo:** `Moderation` · **Colecciones:** `verificaciones`, `reportes`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 043, panel web)

## 1. Objetivo
Definir y especificar los endpoints VER-03, VER-04, VER-05, REP-04, REP-05 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como personal de verificación o moderación, quiero revisar verificaciones y reportes y decidir, para mantener la confianza en la plataforma.

## 2. Historia y alcance
**2.1 Historia**
- Como personal de verificación o moderación, quiero revisar verificaciones y reportes y decidir, para mantener la confianza en la plataforma.

**2.2 Dentro**
- `GET /api/v1/admin/verificaciones` (VER-03): Cola de revisión del panel: verificaciones de identidad y recuperaciones de acceso.
- `GET /api/v1/admin/verificaciones/{id}` (VER-04): Abre una verificación; entrega enlaces de 5 minutos a las fotos y registra quién las abrió.
- `POST /api/v1/admin/verificaciones/{id}/decision` (VER-05): Aprueba o rechaza con motivo; en una verificación destruye la llave de las fotos y en una recuperación cambia el número de la cuenta.
- `GET /api/v1/admin/reportes` (REP-04): Cola de moderación.
- `POST /api/v1/admin/reportes/{id}/decision` (REP-05): Asigna la gravedad (la respuesta se deriva de ella: suspensión preventiva, ocultar, advertencia o mediación) y avisa a las dos partes sin revelar datos de la otra.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 043 (panel web).
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `GET /api/v1/admin/verificaciones` (leer todos / filtro): Cola de revisión del panel: verificaciones de identidad y recuperaciones de acceso. Lo puede llamar: personal interno (Verificador (doble verificación)) con doble verificación, spec 033. Responde 200 cuando sale bien.
- **R-02:** Entrada de `GET /api/v1/admin/verificaciones`: Query: tipo (identidad|recuperacion), estado, desde, hasta. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo. Reglas: `estado`: Solo esos valores. A los 7 días sin revisión vuelve a sin_verificar.
- **R-03:** `GET /api/v1/admin/verificaciones` pagina por cursor (Por cursor, 20): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Filtro por estado y antigüedad; orden por enviadaEn ascendente.
- **R-04:** `GET /api/v1/admin/verificaciones/{id}` (leer uno): Abre una verificación; entrega enlaces de 5 minutos a las fotos y registra quién las abrió. Lo puede llamar: personal interno (Verificador (doble verificación)) con doble verificación, spec 033. Responde 200 cuando sale bien.
- **R-05:** Entrada de `GET /api/v1/admin/verificaciones/{id}`: Ruta: id. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo.
- **R-06:** Estados de `GET /api/v1/admin/verificaciones/{id}`: 404 `verification_not_found`; 410 `photos_destroyed` (fotos ya destruidas). Los códigos de negocio van en inglés y en snake_case.
- **R-07:** `POST /api/v1/admin/verificaciones/{id}/decision` (actualizar (acción)): Aprueba o rechaza con motivo; en una verificación destruye la llave de las fotos y en una recuperación cambia el número de la cuenta. Lo puede llamar: personal interno (Verificador (doble verificación)) con doble verificación, spec 033. Responde 200 cuando sale bien.
- **R-08:** Entrada de `POST /api/v1/admin/verificaciones/{id}/decision`: Cuerpo: decision (aprobada|rechazada), motivo. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `motivo`: Obligatorio si el estado es no_verificado.
- **R-09:** Estados de `POST /api/v1/admin/verificaciones/{id}/decision`: 404 `verification_not_found`; 409 `verification_already_decided`. Los códigos de negocio van en inglés y en snake_case.
- **R-10:** `GET /api/v1/admin/reportes` (leer todos / filtro): Cola de moderación. Lo puede llamar: personal interno (Moderación (doble verificación)) con doble verificación, spec 033. Responde 200 cuando sale bien.
- **R-11:** Entrada de `GET /api/v1/admin/reportes`: Query: gravedad, estado, desde, hasta. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo. Reglas: `gravedad`: La asigna la moderación; `estado`: Solo esos valores.
- **R-12:** `GET /api/v1/admin/reportes` pagina por cursor (Por cursor, 20): parámetros `cursor` y `limite`, 20 por defecto y 50 como máximo; orden: Filtro por gravedad y estado; orden por gravedad y antigüedad.
- **R-13:** `POST /api/v1/admin/reportes/{id}/decision` (actualizar (acción)): Asigna la gravedad (la respuesta se deriva de ella: suspensión preventiva, ocultar, advertencia o mediación) y avisa a las dos partes sin revelar datos de la otra. Lo puede llamar: personal interno (Moderación (doble verificación)) con doble verificación, spec 033. Responde 200 cuando sale bien.
- **R-14:** Entrada de `POST /api/v1/admin/reportes/{id}/decision`: Cuerpo: gravedad (critica|alta|media|baja), motivo. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400. Reglas: `motivo`: Solo esos valores; `gravedad`: La asigna la moderación.
- **R-15:** Estados de `POST /api/v1/admin/reportes/{id}/decision`: 404 `report_not_found`. Los códigos de negocio van en inglés y en snake_case.
- **R-16:** **Cola de verificaciones (VER-03).** Solo con el permiso `verification:review` y doble verificación (spec 033); filtros `tipo` (`identidad` o `recuperacion`, esta última de la spec 039), `estado`, `desde` y `hasta` (por omisión, los últimos 30 días); por cursor (20 por defecto, máximo 50), de la más antigua a la más reciente; la lista no incluye fotos ni datos personales, solo identificador, tipo, estado y fecha. Una verificación sin revisar a los 7 días vuelve a «sin verificar» (spec 025).
- **R-17:** **Abrir una verificación (VER-04).** Entrega enlaces de 5 minutos a las fotos y registra quién las abrió y cuándo (auditoría); los enlaces no se guardan ni se cachean y no se repiten en registros; con las fotos ya destruidas responde 410 `photos_destroyed`.
- **R-18:** **Decidir una verificación o una recuperación (VER-05).** `decision` es `aprobada` o `rechazada`; `motivo` de 8 a 300 caracteres y obligatorio al rechazar; la decide una persona con el rol verificador que no sea la persona verificada. En una verificación de identidad se destruye la llave de las fotos (spec 025); en una **recuperación de acceso** (spec 039), aprobarla cambia el número de la cuenta al número nuevo ya verificado y cierra las sesiones anteriores. En ambos casos se avisa a la persona. Decidir una ya decidida responde 409 `verification_already_decided`.
- **R-19:** **Cola de reportes (REP-04).** Solo con `reports:resolve` y doble verificación; filtros `gravedad`, `estado`, `desde` y `hasta` (por omisión, los últimos 30 días); por cursor (20 por defecto); muestra el motivo y el texto del reporte pero nunca el teléfono ni la dirección de las personas.
- **R-20:** **Decidir un reporte (REP-05).** El moderador asigna la `gravedad` (`critica`, `alta`, `media` o `baja`) y un `motivo` de 8 a 300 caracteres; la respuesta se deriva de la gravedad, como define el Excel: `critica` = suspensión preventiva de la cuenta hasta revisión, `alta` = se oculta al técnico de nuevas solicitudes (`oculto = true`), `media` = advertencia a la persona reportada y `baja` = mediación (aviso a ambas partes para que lo resuelvan, sin sanción). Se avisa a las dos partes **sin revelar datos de la otra** y la decisión queda auditada. Si hay apelación (spec 043), se asigna con prioridad a otra persona del panel.
- **R-21:** **Sin datos en registros ni caché.** Ninguna ruta de esta spec guarda en registros o caché fotos, enlaces firmados, textos de reportes ni datos personales de las personas revisadas; todas las respuestas llevan `Cache-Control: private, no-store`.
- **R-22:** **Permiso y auditoría de las decisiones.** El permiso `verification:review` (rol `verificador`, y `administrador`, spec 033 R-02) cubre las dos colas, la de verificaciones de identidad y la de recuperaciones de acceso; la lista de permisos de la 033 es cerrada y no se agrega uno nuevo. Cada decisión de VER-05 escribe en `panel_auditoria` (spec 033 R-23) un registro con `evento` `verificacion_decidida` o `recuperacion_decidida`, `resultado`, `sub` del verificador y `recursoId`, sin celular, motivo ni datos de la persona; si la auditoría no se puede escribir, la acción responde 503 `audit_unavailable` y no se ejecuta (R-24). Como las recuperaciones y el panel viven en Identidad, el cambio de número, el cierre de sesiones y el registro de auditoría se escriben en **una sola transacción**. Un moderador sin `verification:review` recibe 403 `insufficient_permission` (también auditado).
- **R-23:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-24:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-25:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-26:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «personal interno (Verificador (doble verificación)) con doble verificación, spec 033», cuando llama `GET /api/v1/admin/verificaciones` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/admin/verificaciones`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dada una persona que no cumple el rol «personal interno (Verificador (doble verificación)) con doble verificación, spec 033», cuando llama `GET /api/v1/admin/verificaciones`, entonces responde 403 y no cambia nada.
- **CA-04:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/admin/verificaciones`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-05:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-06:** Dado el rol «personal interno (Verificador (doble verificación)) con doble verificación, spec 033», cuando llama `GET /api/v1/admin/verificaciones/{id}` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-07:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/admin/verificaciones/{id}`, entonces responde 401 y no ejecuta la operación.
- **CA-08:** Dada una persona que no cumple el rol «personal interno (Verificador (doble verificación)) con doble verificación, spec 033», cuando llama `GET /api/v1/admin/verificaciones/{id}`, entonces responde 403 y no cambia nada.
- **CA-09:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/admin/verificaciones/{id}`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-10:** Dado el caso «la verificación no existe», cuando llama `GET /api/v1/admin/verificaciones/{id}`, entonces responde 404 `verification_not_found` con ProblemDetails y no cambia el estado.
- **CA-11:** Dado el caso «las fotos ya fueron destruidas», cuando llama `GET /api/v1/admin/verificaciones/{id}`, entonces responde 410 `photos_destroyed` con ProblemDetails y no cambia el estado.
- **CA-12:** Dado el rol «personal interno (Verificador (doble verificación)) con doble verificación, spec 033», cuando llama `POST /api/v1/admin/verificaciones/{id}/decision` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-13:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/admin/verificaciones/{id}/decision`, entonces responde 401 y no ejecuta la operación.
- **CA-14:** Dada una persona que no cumple el rol «personal interno (Verificador (doble verificación)) con doble verificación, spec 033», cuando llama `POST /api/v1/admin/verificaciones/{id}/decision`, entonces responde 403 y no cambia nada.
- **CA-15:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/admin/verificaciones/{id}/decision`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-16:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/admin/verificaciones/{id}/decision`, entonces responde 400 y no guarda nada.
- **CA-17:** Dado el caso «la verificación o el caso no existe», cuando llama `POST /api/v1/admin/verificaciones/{id}/decision`, entonces responde 404 `verification_not_found` con ProblemDetails y no cambia el estado.
- **CA-18:** Dado el caso «ya tiene una decisión o no está en revisión», cuando llama `POST /api/v1/admin/verificaciones/{id}/decision`, entonces responde 409 `verification_already_decided` con ProblemDetails y no cambia el estado.
- **CA-19:** Dado el rol «personal interno (Moderación (doble verificación)) con doble verificación, spec 033», cuando llama `GET /api/v1/admin/reportes` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-20:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/admin/reportes`, entonces responde 401 y no ejecuta la operación.
- **CA-21:** Dada una persona que no cumple el rol «personal interno (Moderación (doble verificación)) con doble verificación, spec 033», cuando llama `GET /api/v1/admin/reportes`, entonces responde 403 y no cambia nada.
- **CA-22:** Dado un valor inválido según las reglas de §6, cuando llama `GET /api/v1/admin/reportes`, entonces responde 422 con `errors.<campo>`, no repite el valor y no devuelve datos.
- **CA-23:** Dado un listado con más de 20 elementos, cuando se pide sin `limite`, entonces devuelve 20 y el cursor de la página siguiente; con `limite=51` responde 422.
- **CA-24:** Dado el rol «personal interno (Moderación (doble verificación)) con doble verificación, spec 033», cuando llama `POST /api/v1/admin/reportes/{id}/decision` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-25:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/admin/reportes/{id}/decision`, entonces responde 401 y no ejecuta la operación.
- **CA-26:** Dada una persona que no cumple el rol «personal interno (Moderación (doble verificación)) con doble verificación, spec 033», cuando llama `POST /api/v1/admin/reportes/{id}/decision`, entonces responde 403 y no cambia nada.
- **CA-27:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/admin/reportes/{id}/decision`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-28:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/admin/reportes/{id}/decision`, entonces responde 400 y no guarda nada.
- **CA-29:** Dado el caso «el reporte no existe», cuando llama `POST /api/v1/admin/reportes/{id}/decision`, entonces responde 404 `report_not_found` con ProblemDetails y no cambia el estado.
- **CA-30:** Dado un verificador, cuando consulta la cola, entonces ve identificadores, tipo, estado y fecha, de la más antigua a la más reciente y de los últimos 30 días por omisión; un `tipo` o `estado` inválido responde 422.
- **CA-31:** Dado un verificador que abre una verificación, cuando recibe los enlaces, entonces vencen a los 5 minutos y queda registrada la apertura; con las fotos destruidas responde 410 `photos_destroyed`.
- **CA-32:** Dada una verificación o una recuperación en revisión, cuando el verificador aprueba o rechaza con motivo, entonces queda decidida y se aplica su efecto (llave de fotos destruida o cambio de número con sesiones cerradas) y la persona recibe aviso; rechazar sin motivo responde 422.
- **CA-33:** Dado un moderador, cuando consulta la cola, entonces ve motivo y texto y ningún teléfono ni dirección; un filtro inválido responde 422.
- **CA-34:** Dado un reporte abierto, cuando el moderador asigna una gravedad con motivo, entonces se aplica la respuesta de esa gravedad (suspensión, ocultar, advertencia o mediación), ambas partes reciben aviso sin datos de la otra y la decisión queda auditada.
- **CA-35:** Dadas estas rutas, cuando se inspeccionan registros, caché y encabezados, entonces no aparecen fotos, enlaces ni textos y las respuestas no son cacheables.
- **CA-36:** Dada una recuperación en revisión, cuando un verificador la aprueba, entonces el número cambia, se cierran las sesiones y queda un registro `recuperacion_decidida` en la misma transacción; sin poder escribir la auditoría responde 503 `audit_unavailable` y no cambia nada; y un moderador sin `verification:review` recibe 403 con su registro de denegación.
- **CA-37:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-38:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-39:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-40:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec053Tests.Criterio01_CA_053_01` |
| CA-02 | `Spec053Tests.Criterio02_CA_053_02` |
| CA-03 | `Spec053Tests.Criterio03_CA_053_03` |
| CA-04 | `Spec053Tests.Criterio04_CA_053_04` |
| CA-05 | `Spec053Tests.Criterio05_CA_053_05` |
| CA-06 | `Spec053Tests.Criterio06_CA_053_06` |
| CA-07 | `Spec053Tests.Criterio07_CA_053_07` |
| CA-08 | `Spec053Tests.Criterio08_CA_053_08` |
| CA-09 | `Spec053Tests.Criterio09_CA_053_09` |
| CA-10 | `Spec053Tests.Criterio10_CA_053_10` |
| CA-11 | `Spec053Tests.Criterio11_CA_053_11` |
| CA-12 | `Spec053Tests.Criterio12_CA_053_12` |
| CA-13 | `Spec053Tests.Criterio13_CA_053_13` |
| CA-14 | `Spec053Tests.Criterio14_CA_053_14` |
| CA-15 | `Spec053Tests.Criterio15_CA_053_15` |
| CA-16 | `Spec053Tests.Criterio16_CA_053_16` |
| CA-17 | `Spec053Tests.Criterio17_CA_053_17` |
| CA-18 | `Spec053Tests.Criterio18_CA_053_18` |
| CA-19 | `Spec053Tests.Criterio19_CA_053_19` |
| CA-20 | `Spec053Tests.Criterio20_CA_053_20` |
| CA-21 | `Spec053Tests.Criterio21_CA_053_21` |
| CA-22 | `Spec053Tests.Criterio22_CA_053_22` |
| CA-23 | `Spec053Tests.Criterio23_CA_053_23` |
| CA-24 | `Spec053Tests.Criterio24_CA_053_24` |
| CA-25 | `Spec053Tests.Criterio25_CA_053_25` |
| CA-26 | `Spec053Tests.Criterio26_CA_053_26` |
| CA-27 | `Spec053Tests.Criterio27_CA_053_27` |
| CA-28 | `Spec053Tests.Criterio28_CA_053_28` |
| CA-29 | `Spec053Tests.Criterio29_CA_053_29` |
| CA-30 | `Spec053Tests.Criterio30_CA_053_30` |
| CA-31 | `Spec053Tests.Criterio31_CA_053_31` |
| CA-32 | `Spec053Tests.Criterio32_CA_053_32` |
| CA-33 | `Spec053Tests.Criterio33_CA_053_33` |
| CA-34 | `Spec053Tests.Criterio34_CA_053_34` |
| CA-35 | `Spec053Tests.Criterio35_CA_053_35` |
| CA-36 | `Spec053Tests.Criterio36_CA_053_36` |
| CA-37 | `Spec053Tests.Criterio37_CA_053_37` |
| CA-38 | `Spec053Tests.Criterio38_CA_053_38` |
| CA-39 | `Spec053Tests.Criterio39_CA_053_39` |
| CA-40 | `Spec053Tests.Criterio40_CA_053_40` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| VER-03 | `GET /api/v1/admin/verificaciones` | personal interno (Verificador (doble verificación)) con doble verificación, spec 033 | Query: tipo (identidad/recuperacion), estado, desde, hasta | 200; 401; 403 |
| VER-04 | `GET /api/v1/admin/verificaciones/{id}` | personal interno (Verificador (doble verificación)) con doble verificación, spec 033 | Ruta: id | 200; 401; 403; 404; 410 (fotos ya destruidas) |
| VER-05 | `POST /api/v1/admin/verificaciones/{id}/decision` | personal interno (Verificador (doble verificación)) con doble verificación, spec 033 | Cuerpo: decision (aprobada/rechazada), motivo | 200; 400; 401; 403; 404; 409 |
| REP-04 | `GET /api/v1/admin/reportes` | personal interno (Moderación (doble verificación)) con doble verificación, spec 033 | Query: gravedad, estado, desde, hasta | 200; 401; 403 |
| REP-05 | `POST /api/v1/admin/reportes/{id}/decision` | personal interno (Moderación (doble verificación)) con doble verificación, spec 033 | Cuerpo: gravedad (critica/alta/media/baja), motivo | 200; 400; 401; 403; 404 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| VER-03 | Filtro por estado y antigüedad; orden por enviadaEn ascendente | Por cursor, 20 | No |
| VER-04 | No aplica | No | No |
| VER-05 | No aplica | No | No |
| REP-04 | Filtro por gravedad y estado; orden por gravedad y antigüedad | Por cursor, 20 | No |
| REP-05 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| VER-04 | 404 | `verification_not_found` | la verificación no existe |
| VER-04 | 410 | `photos_destroyed` | las fotos ya fueron destruidas |
| VER-05 | 404 | `verification_not_found` | la verificación o el caso no existe |
| VER-05 | 409 | `verification_already_decided` | ya tiene una decisión o no está en revisión |
| REP-05 | 404 | `report_not_found` | el reporte no existe |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `verificaciones`.** Registra el proceso de verificación de identidad de un técnico. Es opcional y la revisa una persona del equipo desde el panel. Esta colección NO guarda las fotos de INE ni de selfie: esas viven cifradas con una llave propia en un almacenamiento privado, y se destruyen al decidir o a los 7 días. Aquí queda el resultado, la fecha, quién decidió y la huella de la CURP

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| usuarioId | ObjectId | Sí | Debe existir en usuarios | Sí |
| nivel | Texto | Sí | Solo esos valores | No |
| estado | Texto | Sí | Solo esos valores. A los 7 días sin revisión vuelve a sin_verificar | Compuesto con enviadaEn |
| consentimientoId | ObjectId | Sí | Obligatorio antes de subir fotos | No |
| fotosRef | Documento | No | Se borra al destruir la llave | No |
| curpHuella | Texto | No | Única entre verificaciones aprobadas | Único parcial |
| motivo | Texto | No | Obligatorio si el estado es no_verificado | No |
| decididaPor | ObjectId | No | Debe tener el rol verificador | No |
| enviadaEn | Fecha y hora UTC | Sí | Lo asigna el servidor | Sí |
| decididaEn | Fecha y hora UTC | No |  | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Normal | usuarioId | Ver el estado de verificación de un técnico |
| Compuesto | estado + enviadaEn | Cola de pendientes del panel, ordenada por antigüedad, y la alerta a las 24 horas |
| Único parcial | curpHuella (solo verificadas) | Impedir dos cuentas de la misma persona |

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
- Proyecto `Knock.Identity.Api`, módulo `Moderation` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 025 de este repositorio.
- Spec 031 de este repositorio.
- Spec 033 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: la spec 043 (panel web) consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Panel con doble factor, permisos y auditoría de la spec 033 (hoy no implementados)
  - Cuenta de almacenamiento privada y cifrado de fotos de la spec 025 (hoy no existen en Terraform)
  - Moderación de la spec 031 (hoy no implementada)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-03, CA-08, CA-14, CA-21, CA-26, CA-37 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-03, CA-08, CA-14, CA-21, CA-26, CA-37 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-04, CA-09, CA-15 a CA-16, CA-22, CA-27 a CA-28 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-07, CA-13, CA-20, CA-25 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-39 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-38 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-40 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_053_NN` en el nombre (para `python scripts/sdd_lint.py --trace 053`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-04, CA-07 a CA-09, CA-13 a CA-16, CA-20 a CA-22, CA-25 a CA-28, CA-37, CA-39) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Verificar:** el verificador abre la cola → abre una verificación → ve las fotos 5 minutos → la aprueba y se destruye la llave de las fotos.
- **Moderar:** el moderador abre un reporte con evidencia → asigna gravedad y sanción → ambas partes reciben aviso sin datos de la otra.
- **Apelación:** la persona sancionada apela → otra persona del panel revisa y confirma o revierte.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 053` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** Los enlaces de fotos duran 5 minutos y cada apertura queda auditada, según el Excel.
- **D-05:** Quien decide una verificación o recuperación no puede ser la persona verificada.
- **D-06:** La respuesta a un reporte se deriva de la gravedad definida en el Excel (suspensión preventiva, ocultar, advertencia o mediación); no se agrega una lista aparte de sanciones.
- **D-07:** La cola de recuperaciones de acceso (spec 039) reutiliza la de verificaciones y el permiso `verification:review`: no se crea un panel de soporte.
- **D-08:** Las colas filtran por fechas con los últimos 30 días por omisión y sin rango máximo: la paginación por cursor ya acota el costo.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Sanciones: antes una lista propia (`ninguna`, `advertencia`, `ocultar_perfil`, `suspender_cuenta`); ahora la respuesta se deriva de la gravedad del Excel.
- **C-02:** Rango de 90 días en las colas: eliminado; ahora últimos 30 días por omisión.
- **C-03:** Recuperación de acceso: agregada a la cola de verificaciones (spec 039).
- **C-04:** Apelación: antes siempre otra persona; ahora con prioridad, sin bloqueo.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: el panel con doble factor y los permisos `verification:review` y `reports:resolve` (spec 033, H-4), la cuenta de almacenamiento privada de la spec 025 (H-2) y la moderación de la 031.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
