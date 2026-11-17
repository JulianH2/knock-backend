# Spec 039 — Cuenta y datos personales: cambio de número, recuperación de acceso, exportación, consentimientos y eliminación

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Origen:** `Documentacion/00_Profesores/Desarrollo_Movil/Entregables/Knock_Endpoints_CRUD.xlsx` (revisión 2026-11-02): endpoints USR-03, USR-04, USR-05, USR-06, USR-07, USR-08, USR-09, USR-10, USR-11.
> **Claves de trabajo:** KNOCK-266 (tarea paraguas de las specs de los endpoints del Excel) · **Sprint:** por definir · **Prioridad:** por definir
> **Servicio:** Identidad (`Knock.Identity.Api`) · **Módulo:** `Account` · **Colecciones:** `usuarios`
> **Responsable sugerido:** por asignar (Alexis o Guillermo) · Erick consume el contrato (frontend 029)

## 1. Objetivo
Definir y especificar los endpoints USR-03, USR-04, USR-05, USR-06, USR-07, USR-08, USR-09, USR-10, USR-11 del Excel de endpoints para que el equipo los implemente con el mismo estilo, seguridad y pruebas que las specs 020 a 037. Como persona con cuenta, quiero cambiar mi número, recuperar mi acceso, exportar mis datos, revisar mis consentimientos y eliminar mi cuenta, para tener control de mis datos personales.

## 2. Historia y alcance
**2.1 Historia**
- Como persona con cuenta, quiero cambiar mi número, recuperar mi acceso, exportar mis datos, revisar mis consentimientos y eliminar mi cuenta, para tener control de mis datos personales.

**2.2 Dentro**
- `POST /api/v1/me/cambio-numero` (USR-03): Comprueba que el número nuevo no pertenece a otra cuenta, antes de que la app gaste un SMS de Firebase.
- `POST /api/v1/me/cambio-numero/confirmar` (USR-04): Confirma el cambio con el token de Firebase del número nuevo, cambia el celular, cierra las demás sesiones y deja el aviso `sesion_revocada` en ellas.
- `POST /api/v1/recuperacion-acceso` (USR-05): Inicia la recuperación de acceso con el número nuevo verificado por Firebase; crea un caso para la cola del panel (spec 053) y siempre responde igual.
- `POST /api/v1/me/exportaciones` (USR-06): Pide la descarga de todos los datos propios; se entrega en 48 horas.
- `GET /api/v1/me/consentimientos` (USR-07): Lista los consentimientos con su versión y fecha.
- `PUT /api/v1/me/consentimientos` (USR-08): Activa o retira consentimientos opcionales (promociones, encuestas, recomendaciones).
- `GET /api/v1/me/pendientes` (USR-09): Lista lo que hay pendiente antes de eliminar la cuenta: citas y cotizaciones abiertas.
- `POST /api/v1/me/eliminacion` (USR-10): Programa la eliminación de la cuenta con 7 días de gracia; cancela de inmediato las solicitudes abiertas y las cotizaciones pendientes.
- `POST /api/v1/me/eliminacion/cancelar` (USR-11): Cancela la eliminación durante los 7 días de gracia.
- Los índices y las reglas de cada campo de las colecciones de §6, el límite de peticiones y la documentación del contrato.

**2.3 Fuera**
- La implementación: esta spec solo se escribe; las tareas se activan cuando una persona la apruebe y lo indique.
- Pantallas y textos de la app: los define la spec de frontend 029.
- Endpoints de otras colecciones o de otras specs (020 a 038) que el Excel nombra distinto: se anotan en §15.

## 3. Requisitos
- **R-01:** `POST /api/v1/me/cambio-numero` (crear (acción)): Comprueba que el número nuevo no pertenece a otra cuenta, antes de que la app gaste un SMS de Firebase. Lo puede llamar: persona con sesión (cliente o técnico). Responde 204 cuando sale bien.
- **R-02:** Entrada de `POST /api/v1/me/cambio-numero`: Cuerpo: celularNuevo (E.164 de México). Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-03:** Estados de `POST /api/v1/me/cambio-numero`: 409 `phone_already_registered` (el número ya pertenece a otra cuenta). Los códigos de negocio van en inglés y en snake_case.
- **R-04:** `POST /api/v1/me/cambio-numero/confirmar` (actualizar (acción)): Confirma el cambio con el token de Firebase del número nuevo, cambia el celular, cierra las demás sesiones y deja el aviso `sesion_revocada` en ellas. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-05:** Entrada de `POST /api/v1/me/cambio-numero/confirmar`: Cuerpo: firebaseIdToken (del número nuevo). Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-06:** Estados de `POST /api/v1/me/cambio-numero/confirmar`: 409 (el número ya pertenece a otra cuenta). El código de negocio de cada conflicto va en inglés y en snake_case, y se define al implementar.
- **R-07:** `POST /api/v1/recuperacion-acceso` (crear (acción)): Inicia la recuperación de acceso con el número nuevo verificado por Firebase; crea un caso para la cola del panel (spec 053) y siempre responde igual. Lo puede llamar: cualquier persona, sin sesión. Responde 202 cuando sale bien.
- **R-08:** Entrada de `POST /api/v1/recuperacion-acceso`: Cuerpo: firebaseIdToken (del número nuevo), celularAnterior, tipo (tecnico|cliente). Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-09:** `POST /api/v1/me/exportaciones` (crear (acción)): Pide la descarga de todos los datos propios; se entrega en 48 horas. Lo puede llamar: persona con sesión (cliente o técnico). Responde 202 cuando sale bien.
- **R-10:** Estados de `POST /api/v1/me/exportaciones`: 409 `export_in_progress` (ya hay una en curso). Los códigos de negocio van en inglés y en snake_case.
- **R-11:** `GET /api/v1/me/consentimientos` (leer todos): Lista los consentimientos con su versión y fecha. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-12:** `PUT /api/v1/me/consentimientos` (actualizar): Activa o retira consentimientos opcionales (promociones, encuestas, recomendaciones). Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-13:** Entrada de `PUT /api/v1/me/consentimientos`: Cuerpo: tipo, vigente. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-14:** `GET /api/v1/me/pendientes` (leer todos): Lista lo que hay pendiente antes de eliminar la cuenta: citas y cotizaciones abiertas. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-15:** `POST /api/v1/me/eliminacion` (crear (acción)): Programa la eliminación de la cuenta con 7 días de gracia; cancela de inmediato las solicitudes abiertas y las cotizaciones pendientes. Lo puede llamar: persona con sesión (cliente o técnico). Responde 202 cuando sale bien.
- **R-16:** Entrada de `POST /api/v1/me/eliminacion`: Cuerpo: confirmacion (el texto ELIMINAR), firebaseIdToken si la sesión tiene más de 24 horas, motivo opcional. Un valor inválido responde 422 con `errors.<campo>` sin repetirlo; un cuerpo ilegible, 400.
- **R-17:** Estados de `POST /api/v1/me/eliminacion`: 409 `deletion_not_allowed` (ya está programada o hay una visita en curso). Los códigos de negocio van en inglés y en snake_case.
- **R-18:** `POST /api/v1/me/eliminacion/cancelar` (actualizar (acción)): Cancela la eliminación durante los 7 días de gracia. Lo puede llamar: persona con sesión (cliente o técnico). Responde 200 cuando sale bien.
- **R-19:** Estados de `POST /api/v1/me/eliminacion/cancelar`: 409 `no_deletion_scheduled`. Los códigos de negocio van en inglés y en snake_case.
- **R-20:** **Cambio de número (USR-03 y USR-04).** El SMS lo envía Firebase en el teléfono (spec 015), no el backend. `POST /me/cambio-numero` (USR-03) solo **comprueba que `celularNuevo` (E.164 de México) no pertenece a otra cuenta** antes de que la app gaste un SMS: responde 204 si está libre y 409 `phone_already_registered` si no; admite 3 comprobaciones por hora y por persona. `POST /me/cambio-numero/confirmar` (USR-04) recibe el `firebaseIdToken` del número nuevo, lo valida igual que el inicio de sesión (spec 015 R-02: firma, emisor, destinatario, vencimiento y número verificado), cambia el celular (protegido como en el registro), cierra las demás sesiones conservando la actual (specs 016 y 018) y antes de cerrarlas les deja el aviso `sesion_revocada` (spec 037).
- **R-21:** **Recuperación de acceso (USR-05).** Es pública y limitada (3 por hora por IP y por número). La persona prueba que controla el número nuevo con el `firebaseIdToken` de ese número y envía `celularAnterior` y `tipo` (`tecnico` o `cliente`). **Siempre responde 202** sin revelar si la cuenta existe; si existe, crea un caso en `recuperaciones` (estado `pendiente`) que aparece en la cola del panel (spec 053, tipo `recuperacion`) con datos que el revisor ya conoce (nombre, colonia, fecha de la última solicitud y, si es técnico verificado, la comparación de selfie de la spec 025). No cambia ningún dato hasta que un verificador la apruebe; al aprobarla se cambia el número como en USR-04 y se cierran todas las sesiones anteriores. Un caso sin decidir vence a los 7 días (tarea `VencerRecuperacion`, spec 054).
- **R-22:** **Exportación (USR-06).** Una sola exportación en curso por persona. Se prepara con la tarea `PrepararExportacion` (spec 054): Identidad junta sus datos y pide los de Mercado y Mensajería por rutas internas con el token de la spec 037, arma un ZIP con un archivo JSON por servicio en el contenedor privado `exportaciones` de la cuenta de almacenamiento privada de la spec 025 (ciclo de vida de 7 días) y avisa a la persona con un enlace firmado de 24 horas. Se entrega dentro de las 48 horas que fija el Excel. Contiene solo datos propios.
- **R-23:** **Consentimientos (USR-07 y USR-08).** Los opcionales son `promociones`, `encuestas` y `recomendaciones`; los obligatorios (aviso de privacidad y términos) no se retiran por esta ruta. Cada cambio guarda tipo, versión y fecha, y retirar un consentimiento surte efecto de inmediato. Un `tipo` fuera de la lista responde 422.
- **R-24:** **Pendientes antes de eliminar (USR-09).** Devuelve solo conteos e identificadores de sus citas y cotizaciones abiertas (consulta interna a Mercado), sin datos de otras personas; sin paginación, con un máximo de 50.
- **R-25:** **Eliminación (USR-10 y USR-11).** Se confirma con el texto exacto `ELIMINAR`; si la sesión tiene más de 24 horas, la app pide además re-autenticar con Firebase y envía el `firebaseIdToken`. `motivo` opcional de hasta 200 caracteres. Programa `EliminarCuenta` a 7 días (spec 054) y **al programarla** cancela las solicitudes abiertas y las cotizaciones pendientes de la persona, avisando a la otra parte; si hay una visita `en_curso` responde 409 `deletion_not_allowed`. Durante la gracia puede cancelar. **Al ejecutarse**, las visitas `confirmada` que queden se cancelan como cancelación tardía con aviso a la otra parte, se borran sus datos personales, direcciones y fotos de verificación, se anonimizan sus opiniones (spec 051) y se cierran sus sesiones. La cancelación deja la tarea `hecha` sin efecto.
- **R-26:** **Sin datos personales en registros.** Ninguna de estas rutas escribe celulares, códigos ni textos de motivo en los registros; el aviso al número anterior no repite el número nuevo completo.
- **R-27:** La persona de cada operación es siempre la del `sub` del token; ningún cuerpo, ruta o consulta acepta el identificador de otra persona para actuar en su nombre, y un identificador ajeno enviado en el cuerpo se ignora.
- **R-28:** Los errores usan ProblemDetails con `correlationId` y `traceId`; los registros llevan método, ruta, estado y correlación, sin teléfono, dirección, coordenadas, nombres ni textos escritos por la persona (spec 011).
- **R-29:** Por encima del límite de peticiones responde 429 con `Retry-After`; cada ruta tiene su límite registrado en `docs/rate-limit-matrix.md` y los cuerpos se limitan según la spec 009 (413 si lo exceden).
- **R-30:** Las rutas que crean algo no usan `Idempotency-Key` (retirado en la spec 020): la repetición se controla con la regla de estado o el índice único de §6.

## 4. Criterios de aceptación
- **CA-01:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/me/cambio-numero` con datos válidos, entonces responde 204 con lo descrito en §5.
- **CA-02:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/me/cambio-numero`, entonces responde 401 y no ejecuta la operación.
- **CA-03:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/me/cambio-numero`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-04:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/me/cambio-numero`, entonces responde 400 y no guarda nada.
- **CA-05:** Dado el caso «el número nuevo ya pertenece a otra cuenta», cuando llama `POST /api/v1/me/cambio-numero`, entonces responde 409 `phone_already_registered` con ProblemDetails y no cambia el estado.
- **CA-06:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/me/cambio-numero/confirmar` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-07:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/me/cambio-numero/confirmar`, entonces responde 401 y no ejecuta la operación.
- **CA-08:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/me/cambio-numero/confirmar`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-09:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/me/cambio-numero/confirmar`, entonces responde 400 y no guarda nada.
- **CA-10:** Dado el caso «el número ya pertenece a otra cuenta», cuando llama `POST /api/v1/me/cambio-numero/confirmar`, entonces responde 409 con ProblemDetails y no cambia el estado.
- **CA-11:** Dado el rol «cualquier persona, sin sesión», cuando llama `POST /api/v1/recuperacion-acceso` con datos válidos, entonces responde 202 con lo descrito en §5.
- **CA-12:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/recuperacion-acceso`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-13:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/recuperacion-acceso`, entonces responde 400 y no guarda nada.
- **CA-14:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/me/exportaciones` con datos válidos, entonces responde 202 con lo descrito en §5.
- **CA-15:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/me/exportaciones`, entonces responde 401 y no ejecuta la operación.
- **CA-16:** Dado el caso «ya hay una exportación en curso», cuando llama `POST /api/v1/me/exportaciones`, entonces responde 409 `export_in_progress` con ProblemDetails y no cambia el estado.
- **CA-17:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/me/consentimientos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-18:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/me/consentimientos`, entonces responde 401 y no ejecuta la operación.
- **CA-19:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `PUT /api/v1/me/consentimientos` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-20:** Dada una petición sin token o con token vencido, cuando llama `PUT /api/v1/me/consentimientos`, entonces responde 401 y no ejecuta la operación.
- **CA-21:** Dado un valor inválido según las reglas de §6, cuando llama `PUT /api/v1/me/consentimientos`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-22:** Dado un cuerpo que no se puede leer, cuando llama `PUT /api/v1/me/consentimientos`, entonces responde 400 y no guarda nada.
- **CA-23:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `GET /api/v1/me/pendientes` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-24:** Dada una petición sin token o con token vencido, cuando llama `GET /api/v1/me/pendientes`, entonces responde 401 y no ejecuta la operación.
- **CA-25:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/me/eliminacion` con datos válidos, entonces responde 202 con lo descrito en §5.
- **CA-26:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/me/eliminacion`, entonces responde 401 y no ejecuta la operación.
- **CA-27:** Dado un valor inválido según las reglas de §6, cuando llama `POST /api/v1/me/eliminacion`, entonces responde 422 con `errors.<campo>`, no repite el valor y no guarda nada.
- **CA-28:** Dado un cuerpo que no se puede leer, cuando llama `POST /api/v1/me/eliminacion`, entonces responde 400 y no guarda nada.
- **CA-29:** Dado el caso «la eliminación ya está programada o hay una visita en curso», cuando llama `POST /api/v1/me/eliminacion`, entonces responde 409 `deletion_not_allowed` con ProblemDetails y no cambia el estado.
- **CA-30:** Dado el rol «persona con sesión (cliente o técnico)», cuando llama `POST /api/v1/me/eliminacion/cancelar` con datos válidos, entonces responde 200 con lo descrito en §5.
- **CA-31:** Dada una petición sin token o con token vencido, cuando llama `POST /api/v1/me/eliminacion/cancelar`, entonces responde 401 y no ejecuta la operación.
- **CA-32:** Dado el caso «no hay una eliminación programada que cancelar», cuando llama `POST /api/v1/me/eliminacion/cancelar`, entonces responde 409 `no_deletion_scheduled` con ProblemDetails y no cambia el estado.
- **CA-33:** Dada una persona con sesión, cuando verifica el número nuevo con Firebase y lo confirma, entonces cambia su celular, se cierran sus otras sesiones y se conserva la actual; con un número que ya es de otra cuenta responde 409 `phone_already_registered`, y a la cuarta comprobación en una hora, 429.
- **CA-34:** Dado un número que no existe y otro que sí, cuando se pide la recuperación con un número nuevo verificado, entonces ambos responden 202 igual; el caso del que sí existe aparece en la cola del panel, no cambia ningún dato hasta aprobarse y vence a los 7 días; a la cuarta solicitud en una hora responde 429.
- **CA-35:** Dada una persona sin exportación en curso, cuando la pide, entonces responde 202 y recibe un aviso con un enlace de 24 horas cuando el ZIP está listo (máximo 48 horas); una segunda mientras la primera sigue responde 409 `export_in_progress`; el archivo solo trae sus datos.
- **CA-36:** Dada una persona, cuando retira `promociones`, entonces queda `vigente: false` con fecha y versión y deja de recibir esos avisos; un tipo fuera de la lista o intentar retirar uno obligatorio responde 422.
- **CA-37:** Dada una persona con citas y cotizaciones abiertas, cuando consulta sus pendientes, entonces recibe sus conteos e identificadores y ningún dato de otra persona.
- **CA-38:** Dada una persona que confirma la eliminación, cuando pasan 7 días sin cancelar, entonces se borran sus datos y se anonimizan sus opiniones y las visitas confirmadas pendientes se cancelan con aviso; si cancela antes, no se borra nada; pedirla con una visita en curso o dos veces responde 409 `deletion_not_allowed`.
- **CA-39:** Dadas estas operaciones, cuando se inspeccionan los registros, entonces no aparecen celulares completos, códigos ni motivos.
- **CA-40:** Dado un cuerpo con el identificador de otra persona, cuando se llama cualquier ruta de esta spec, entonces la operación usa el `sub` del token y no el del cuerpo.
- **CA-41:** Dada una operación con datos personales, cuando se inspeccionan los registros y las respuestas de error, entonces no aparecen esos datos.
- **CA-42:** Dada una persona que supera el límite de una ruta, cuando repite la petición, entonces responde 429 con `Retry-After` y, con un cuerpo de tamaño excesivo, 413.
- **CA-43:** Dada una operación de creación repetida por mala señal, cuando llega dos veces, entonces no deja duplicados porque lo impide la regla de estado o el índice único.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec039Tests.Criterio01_CA_039_01` |
| CA-02 | `Spec039Tests.Criterio02_CA_039_02` |
| CA-03 | `Spec039Tests.Criterio03_CA_039_03` |
| CA-04 | `Spec039Tests.Criterio04_CA_039_04` |
| CA-05 | `Spec039Tests.Criterio05_CA_039_05` |
| CA-06 | `Spec039Tests.Criterio06_CA_039_06` |
| CA-07 | `Spec039Tests.Criterio07_CA_039_07` |
| CA-08 | `Spec039Tests.Criterio08_CA_039_08` |
| CA-09 | `Spec039Tests.Criterio09_CA_039_09` |
| CA-10 | `Spec039Tests.Criterio10_CA_039_10` |
| CA-11 | `Spec039Tests.Criterio11_CA_039_11` |
| CA-12 | `Spec039Tests.Criterio12_CA_039_12` |
| CA-13 | `Spec039Tests.Criterio13_CA_039_13` |
| CA-14 | `Spec039Tests.Criterio14_CA_039_14` |
| CA-15 | `Spec039Tests.Criterio15_CA_039_15` |
| CA-16 | `Spec039Tests.Criterio16_CA_039_16` |
| CA-17 | `Spec039Tests.Criterio17_CA_039_17` |
| CA-18 | `Spec039Tests.Criterio18_CA_039_18` |
| CA-19 | `Spec039Tests.Criterio19_CA_039_19` |
| CA-20 | `Spec039Tests.Criterio20_CA_039_20` |
| CA-21 | `Spec039Tests.Criterio21_CA_039_21` |
| CA-22 | `Spec039Tests.Criterio22_CA_039_22` |
| CA-23 | `Spec039Tests.Criterio23_CA_039_23` |
| CA-24 | `Spec039Tests.Criterio24_CA_039_24` |
| CA-25 | `Spec039Tests.Criterio25_CA_039_25` |
| CA-26 | `Spec039Tests.Criterio26_CA_039_26` |
| CA-27 | `Spec039Tests.Criterio27_CA_039_27` |
| CA-28 | `Spec039Tests.Criterio28_CA_039_28` |
| CA-29 | `Spec039Tests.Criterio29_CA_039_29` |
| CA-30 | `Spec039Tests.Criterio30_CA_039_30` |
| CA-31 | `Spec039Tests.Criterio31_CA_039_31` |
| CA-32 | `Spec039Tests.Criterio32_CA_039_32` |
| CA-33 | `Spec039Tests.Criterio33_CA_039_33` |
| CA-34 | `Spec039Tests.Criterio34_CA_039_34` |
| CA-35 | `Spec039Tests.Criterio35_CA_039_35` |
| CA-36 | `Spec039Tests.Criterio36_CA_039_36` |
| CA-37 | `Spec039Tests.Criterio37_CA_039_37` |
| CA-38 | `Spec039Tests.Criterio38_CA_039_38` |
| CA-39 | `Spec039Tests.Criterio39_CA_039_39` |
| CA-40 | `Spec039Tests.Criterio40_CA_039_40` |
| CA-41 | `Spec039Tests.Criterio41_CA_039_41` |
| CA-42 | `Spec039Tests.Criterio42_CA_039_42` |
| CA-43 | `Spec039Tests.Criterio43_CA_039_43` |

## 5. Contrato
| ID | Método y ruta | Quién | Entrada | Respuestas |
|---|---|---|---|---|
| USR-03 | `POST /api/v1/me/cambio-numero` | persona con sesión (cliente o técnico) | Cuerpo: celularNuevo (E.164 de México) | 204; 400; 401; 409 (el número ya pertenece a otra cuenta); 429 |
| USR-04 | `POST /api/v1/me/cambio-numero/confirmar` | persona con sesión (cliente o técnico) | Cuerpo: firebaseIdToken (del número nuevo) | 200; 400; 401; 409 (el número ya pertenece a otra cuenta); 429 |
| USR-05 | `POST /api/v1/recuperacion-acceso` | cualquier persona, sin sesión | Cuerpo: firebaseIdToken (del número nuevo), celularAnterior, tipo (tecnico/cliente) | 202; 400; 429 |
| USR-06 | `POST /api/v1/me/exportaciones` | persona con sesión (cliente o técnico) | Ninguno | 202; 401; 409 (ya hay una en curso); 429 |
| USR-07 | `GET /api/v1/me/consentimientos` | persona con sesión (cliente o técnico) | Ninguno | 200; 401 |
| USR-08 | `PUT /api/v1/me/consentimientos` | persona con sesión (cliente o técnico) | Cuerpo: tipo, vigente | 200; 400; 401 |
| USR-09 | `GET /api/v1/me/pendientes` | persona con sesión (cliente o técnico) | Ninguno | 200; 401 |
| USR-10 | `POST /api/v1/me/eliminacion` | persona con sesión (cliente o técnico) | Cuerpo: confirmacion (el texto ELIMINAR), firebaseIdToken si la sesión tiene más de 24 horas, motivo opcional | 202; 400; 401; 409 (ya está programada o hay una visita en curso); 429 |
| USR-11 | `POST /api/v1/me/eliminacion/cancelar` | persona con sesión (cliente o técnico) | Ninguno | 200; 401; 409 |

**Filtros, orden, paginación y caché**

| ID | Filtros y orden | Paginación | Caché |
|---|---|---|---|
| USR-03 | No aplica | No | No |
| USR-04 | No aplica | No | No |
| USR-05 | No aplica | No | No |
| USR-06 | No aplica | No | No |
| USR-07 | No aplica | No | No |
| USR-08 | No aplica | No | No |
| USR-09 | No aplica | No | No |
| USR-10 | No aplica | No | No |
| USR-11 | No aplica | No | No |

**Códigos de error de negocio**

| ID | Estado | Código | Cuándo |
|---|---|---|---|
| USR-03 | 409 | `phone_already_registered` | el número nuevo ya pertenece a otra cuenta |
| USR-06 | 409 | `export_in_progress` | ya hay una exportación en curso |
| USR-10 | 409 | `deletion_not_allowed` | la eliminación ya está programada o hay una visita en curso |
| USR-11 | 409 | `no_deletion_scheduled` | no hay una eliminación programada que cancelar |


**Códigos de error:** los de forma salen como 422 con `errors` por campo y los de sesión y límite como 401 y 429; los conflictos de negocio llevan un código en inglés y snake_case que se fija al implementar y se agrega al catálogo.

## 6. Modelo de datos
**Colección `usuarios`.** Guarda la cuenta de cada persona que usa Knock. Una sola cuenta sirve para ser cliente y para ser técnico: si la persona se registra como técnico, su perfil de técnico vive en la colección tecnicos del servicio de Mercado y aquí solo queda la marca de que es técnico. Es la colección más consultada, porque todas las peticiones con sesión empiezan identificando a la persona. No existe una tabla de…

| Campo | Tipo | Obligatorio | Reglas y validaciones | Índice |
|---|---|---|---|---|
| _id | ObjectId | Sí | Lo genera la base | Llave primaria |
| celularCifrado | Texto | Sí | Cifrado con AES-256-GCM y llave de Key Vault (nonce de 12 bytes). Nunca se registra ni se devuelve en una res… | No |
| celularHash | Texto | Sí | HMAC-SHA256 del celular de 10 dígitos (sin +52) con llave de Key Vault. Único: un celular corresponde a una s… | Único |
| sub | Texto | Sí | UUID v8 derivado con SHA-256 del uid de Firebase (TT-API-12). No cambia. Único. El uid no se guarda | Único |
| perfilCompleto | Booleano | Sí | Por defecto falso; pasa a verdadero cuando existe el nombre (USR-02) | No |
| nombre | Texto | No | Opcional al crear la cuenta; se pide al completar el perfil (USR-02). De 2 a 80 caracteres: letras (con acent… | No |
| fotoUrl | Texto | No | Foto reducida a 320 px. Solo se acepta una dirección de nuestro almacenamiento o CDN. Si no hay, la app muest… | No |
| colonia | Texto | No | Debe existir en la colección colonias | No |
| codigoPostal | Texto | No | 5 dígitos | No |
| esTecnico | Booleano | Sí | Por defecto falso | No |
| consentimientos | Lista de documentos | Sí | Al crear la cuenta se guardan dos: aviso_privacidad y terminos, cada uno con su versión vigente. El consentim… | No |
| estado | Texto | Sí | Solo estos valores | Compuesto con eliminarEn |
| eliminarEn | Fecha y hora UTC | No | Se borra si la persona cancela la eliminación | Sí (para el proceso de borrado) |
| creadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |
| actualizadoEn | Fecha y hora UTC | Sí | Lo asigna el servidor | No |

| Tipo | Campos | Para qué sirve |
|---|---|---|
| Llave principal | _id | Identifica de forma única cada documento. La genera la base de datos |
| Único | celularHash | Que un celular corresponda a una sola cuenta y buscarlo al iniciar sesión sin descifrarlo |
| Único | sub | Encontrar la cuenta a partir del token y evitar dos cuentas con el mismo sub |
| Compuesto | estado + eliminarEn | Que el proceso de fondo encuentre las cuentas cuyos 7 días de gracia ya vencieron |

**Colección `recuperaciones` (nueva, Identidad).** Un documento por caso: `usuarioId` (si existe), `celularNuevoProtegido` (mismo protegido que `usuarios`), `tipo` (`tecnico` o `cliente`), `estado` (`pendiente`, `aprobada`, `rechazada`, `vencida`), `decididaPor`, `creadoEn` y `actualizadoEn`. Índice `{ estado, creadoEn }` y TTL de 90 días sobre `actualizadoEn` para los cerrados.

## 7. Arquitectura y componentes
- Proyecto `Knock.Identity.Api`, módulo `Account` con las capas del estilo de la spec 002: `Controllers`, `Dtos`, `Validators`, `Services`, `Data` y `Hosting`.
- Cada ruta recibe sus parámetros en objetos tipados con validador registrado, nunca como `string` suelto, y consulta con filtros por el `sub` del token.
- Las rutas bajo `/admin` usan la política de rol de la spec 033 y no están disponibles sin la sesión del panel.
- La caché, la paginación por cursor y los límites usan las piezas de las specs 009 y 010.
- El contrato se publica en OpenAPI y en `docs/endpoints.md`; los límites, en `docs/rate-limit-matrix.md`.

## 8. Dependencias
- Spec 015 de este repositorio.
- Spec 016 de este repositorio.
- Spec 017 de este repositorio.
- Spec 018 de este repositorio.
- Spec 025 de este repositorio.
- Specs 002, 003, 009, 010 y 011 (estilo, bases de datos, seguridad, caché y registros).
- Frontend: spec 029 consume este contrato.
- **Prerrequisitos que hoy no existen (trabajo pendiente, no capacidades implementadas):**
  - Rutas internas con el token de la spec 037: sus claves `avisos-key-*` y el secreto nuevo `interno-key-identity` (Identidad firma, Mercado verifica) en Key Vault y Terraform (hoy no implementados ni creados)
  - Contenedor privado `exportaciones` en la cuenta de almacenamiento privada de la spec 025 (hoy no existe en Terraform)
  - Ejecutor de tareas y bandeja de salida de la spec 054 (hoy no implementados)
  - Cola del panel de la spec 053 y panel de la spec 033 (hoy no implementados)

## 9. Amenazas y controles
| Amenaza | Control | Casos que lo prueban |
|---|---|---|
| Acceso a datos de otra persona (IDOR) | Toda consulta y escritura filtra por el `sub` del token; ajeno responde 403 o 404 | CA-40 |
| Asignación masiva o identificador ajeno en el cuerpo | El DTO no acepta identificadores de persona; se ignoran | CA-40 |
| Inyección en filtros y parámetros | Parámetros tipados con validador; nada se concatena en consultas | CA-03 a CA-04, CA-08 a CA-09, CA-12 a CA-13, CA-21 a CA-22, CA-27 a CA-28 |
| Ruta sin sesión | JWT obligatorio salvo las públicas | CA-02, CA-07, CA-15, CA-18, CA-20, CA-24, CA-26, CA-31 |
| Abuso por repetición o cuerpos grandes | Límite de peticiones, `Retry-After` y tope de cuerpo | CA-42 |
| Datos personales en registros y errores | Registros sin datos personales; ProblemDetails sin valores | CA-41 |
| Duplicados por reintento | Regla de estado o índice único, sin `Idempotency-Key` | CA-43 |

## 10. Plan de pruebas
Las pruebas citan el caso como `CA_039_NN` en el nombre (para `python scripts/sdd_lint.py --trace 039`).

| Tipo | ¿Aplica? | Qué se prueba / herramienta |
|---|---|---|
| Unitarias | Sí | Validadores, reglas de estado y cálculos del dominio |
| Integración | Sí | Cada ruta con la base real en contenedor (Testcontainers) y `MarketApiFactory`/fábrica del servicio |
| Seguridad negativa | Sí | 401, 403/404, 422, 413 y 429 (CA-02 a CA-04, CA-07 a CA-09, CA-12 a CA-13, CA-15, CA-18, CA-20 a CA-22, CA-24, CA-26 a CA-28, CA-31, CA-40, CA-42) |
| Base de datos | Sí | Índices y reglas de cada colección de §6 |
| Contrato | Sí | OpenAPI regenerado y comparado con `contracts/` |
| Mutación | Sí | Meta del módulo según la spec 008 |
| Rendimiento | No aplica | Sin ruta crítica; se mide solo si la spec 034 lo pide |

## 11. Escenarios
- **Cambio de número:** la persona pide el cambio → Firebase le manda el SMS al número nuevo → lo verifica y confirma el cambio → se cierran sus otras sesiones y el número anterior recibe el aviso.
- **Eliminar cuenta:** la persona ve sus pendientes → confirma con el código → la cuenta queda programada para 7 días → si se arrepiente, la cancela desde Perfil.
- **Exportar datos:** la persona pide su exportación → recibe un aviso cuando está lista → descarga el archivo con un enlace de 24 horas.

## 12. Definición de terminado
- Los criterios de aceptación están cubiertos por pruebas en verde y `sdd_lint --trace 039` sin faltantes.
- El CI en verde, un par revisó el cambio y `Memoria.md` al día.
- OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md` actualizados.

## 13. Decisiones
- La paginación, la caché y los roles de cada ruta salen de la hoja de cada colección del Excel de endpoints (revisión 2026-11-02).
- Las rutas de la persona viven bajo `/me` o por identificador con dueño verificado; las del personal interno, bajo `/admin`.

## 14. Decisiones tomadas sin consulta
- **D-01:** Se toma el Excel de endpoints como fuente del contrato porque es el insumo acordado para las specs; cualquier diferencia con las specs 020 a 038 se resuelve a favor de la spec numerada.
- **D-02:** Los conflictos de negocio llevan códigos en inglés y snake_case (los de §5 y, si falta alguno, los que se fijen al implementar), siguiendo la convención de errores de la spec 002.
- **D-03:** No se usa `Idempotency-Key`, aunque el Excel lo menciona en creaciones.
- **D-04:** El cambio de número usa Firebase, que ya está en el proyecto con facturación activa: es la única forma de que el SMS salga de un proveedor que el backend no tiene que operar. USR-03 solo comprueba disponibilidad para no gastar SMS.
- **D-05:** Se deja de avisar por SMS al número anterior (el backend no envía SMS y contratar otro proveedor no aporta lo suficiente); en su lugar se avisa a las demás sesiones y se cierran.
- **D-06:** La recuperación de acceso se resuelve con un caso en la cola del panel (spec 053): reutiliza la cola y los permisos que ya existen y no obliga a crear un proceso de soporte aparte.
- **D-07:** La eliminación nunca queda bloqueada por citas, salvo una visita en curso: se avisa y se cancelan con aviso, para que una persona pueda irse sin quedar atrapada y la otra parte sepa qué pasó.
- **D-08:** La exportación sale en un ZIP con un JSON por servicio, en un contenedor privado de la cuenta de la spec 025, con enlace de 24 horas: no agrega cuentas ni servicios nuevos.

**Correcciones de la revisión del 2026-11-13** (se conserva lo propuesto antes y se explica el cambio)
- **C-01:** Cambio de número: antes el backend pedía y validaba un código SMS (10 minutos, 5 intentos); ahora lo verifica Firebase y el backend valida el token. Motivo: la spec 015 fija que el SMS lo envía Firebase.
- **C-02:** Recuperación: antes «el cliente pasa a soporte» sin proceso; ahora hay un caso en la cola del panel con vencimiento a los 7 días.
- **C-03:** Eliminación: antes se cancelaba todo al ejecutarse; ahora se cancelan solicitudes y cotizaciones al programarla y se bloquea solo con una visita en curso.
- **C-04:** Exportación: antes sin destino definido; ahora ZIP en contenedor privado con enlace de 24 horas y tarea `PrepararExportacion`.

## 15. Riesgos y dudas abiertas
- Sin dudas abiertas: todas las decisiones de esta spec quedaron resueltas en el cierre del 2026-11-13.
- Dependencias: verificación por Firebase de la spec 015, cola del panel de la spec 053, rutas internas con el token de la spec 037 (H-1), contenedor privado `exportaciones` en la cuenta de almacenamiento de la spec 025 (H-2, tarea de infraestructura) y los plazos del aviso de privacidad (KNOCK-218).

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-14
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
