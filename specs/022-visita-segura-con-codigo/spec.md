# Spec 022 — Visita segura con código de 4 dígitos

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`
> **Claves de trabajo (verificadas en Jira el 2026-11-09):** KNOCK-83 «HU-036 · Ver mi visita con el código de 4 dígitos o la cita en el taller», KNOCK-85 «HU-038 · Confirmar mi llegada con el código ("Ya llegué")» y KNOCK-84 «HU-037 · Avisar a alguien de confianza» (etiquetada `candidata-recorte`)
> **Responsables sugeridos:** Guillermo (backend de HU-036, HU-037 y HU-038) · Erick consume el contrato (frontend 016) · **Ola:** B · **Sprint:** 4 · **Prioridad:** P2 (HU-037, P3)
> **Servicio:** Mercado · **Módulo:** Requests · **Colecciones:** `visitas`, `solicitudes`, `cotizaciones`

## 1. Objetivo
Que la persona vea su visita a domicilio con el código de cuatro dígitos que reconoce al técnico en la puerta (o su cita en el taller con la dirección y “Cómo llegar”), que pueda avisar a alguien de confianza sin exponer el código, y que el técnico confirme su llegada con el código en el servidor, con intentos limitados y bloqueo.

## 2. Historia y alcance
**2.1 Historias**
- **HU-036** — Como cliente, quiero ver mi visita con el código de 4 dígitos o mi cita en el taller, para saber quién viene y reconocerlo en la puerta.
- **HU-038** — Como técnico, quiero capturar el código que me da el cliente en la puerta, para demostrar que soy la persona que eligió y empezar el trabajo.
- **HU-037** — Como cliente, quiero avisar a alguien de confianza quién viene y cuándo, para que otra persona sepa quién entra a mi casa.

**2.2 Dentro**
- Ver la visita propia (cliente) y la vista acotada del técnico elegido.
- Mostrar el código solo al cliente, solo en domicilio, solo mientras la visita está vigente.
- Confirmar la llegada con el código: un solo uso, tres intentos y bloqueo.
- Registrar la hora real de llegada y el evento para el cliente.
- Preparar el texto de aviso a una persona de confianza, sin código y sin dirección.
- Cita en taller: día, hora, dirección y “Cómo llegar” (la ruta la abre la app con el mapa del teléfono).

**2.3 Fuera**
- Generar la cita y el código (spec 021).
- Reprogramar, cancelar y “el cliente no estaba” (HU-034, HU-041).
- Terminar el trabajo y subir fotos (HU-039).
- Revisión del panel, reportes y sanciones (specs de Identidad).
- Enviar el mensaje por WhatsApp o SMS y guardar contactos: lo hace la app con la hoja de compartir del sistema; el servidor solo arma el texto.
- Código para la cita en taller: no existe.

## 3. Requisitos
- **R-01:** `GET /api/v1/visitas/{id}` exige JWT y responde solo al cliente dueño de la visita o al técnico elegido. Cualquier otra persona recibe 404. El identificador debe ser un `ObjectId` válido; si no, 400.
- **R-02:** La vista del cliente trae `{ id, rol: "cliente", estado, modalidad, dia, hora, precio, tope, tecnico: { nombre, fotoUrl, verificado, nivelVerificacion, telefono?, whatsapp? }, lugar: { tipo, direccion }, codigo?, codigoVenceEn?, motivoCodigo?, reglaSeguridad?, puedeAvisar, puedeLlegar: false }`. `telefono` y `whatsapp` son el contacto público del técnico (spec 021, R-15); aparecen porque la visita solo existe después de elegir.
- **R-03:** El código solo aparece en la vista del cliente, solo con `modalidad = domicilio`, solo en estado `confirmada` o `en_curso`, solo si no se usó, no venció y la visita no está bloqueada. En cualquier otro caso el campo va ausente y `motivoCodigo` explica por qué (`usado`, `vencido`, `bloqueada`, `no_aplica`).
- **R-04:** La regla de seguridad es el texto fijo: “Pídele a {nombre del técnico} que te diga este código en la puerta. Si no lo sabe, no abras.” El código nunca se dicta por teléfono ni por mensaje.
- **R-05:** La vista del técnico trae `{ id, rol: "tecnico", estado, modalidad, dia, hora, precio, tope, cliente: { colonia }, lugar: { tipo, direccion }, puedeLlegar }` y **nunca** incluye el código, el nombre ni el teléfono del cliente. La dirección del cliente se revela aquí porque el técnico ya fue elegido, **pero solo mientras la visita esté `confirmada` o `en_curso` y no bloqueada** (decisión del líder, 2026-11-16): en `terminada`, `cancelada`, `no_presentado` o con la visita bloqueada, `lugar.direccion` va ausente en la vista del técnico (la dirección del taller es del propio técnico y no se oculta). `puedeLlegar` es verdadero solo en domicilio, estado `confirmada`, sin bloqueo y dentro de la **ventana de la cita**: desde las 00:00 del `dia` de la cita hasta `codigoVenceEn` (23:59:59 de ese día más 24 h), en la zona `America/Mexico_City`.
- **R-06:** Para `modalidad = taller`, la vista muestra día, hora y la dirección del taller; no hay código y `puedeLlegar` es falso. La app abre “Cómo llegar” con la dirección; el servidor no genera rutas ni enlaces de mapas.
- **R-07:** `POST /api/v1/visitas/{id}/llegada` exige JWT del técnico elegido y cuerpo `{ codigo }` de exactamente cuatro dígitos ASCII (`^[0-9]{4}$`, se conservan los ceros a la izquierda). Un `codigo` con otro formato responde 422 con `errors.codigo` y **no** cuenta como intento. Antes de la ventana de la cita responde 409 `outside_window` y no cuenta como intento. Compara en tiempo constante contra el código descifrado; si coincide, responde 200 con la vista del técnico en `en_curso` y `llegadaEn`.
- **R-08:** Un código incorrecto responde 422 `wrong_code` con `intentosRestantes` y no cambia la visita, salvo el contador. Al tercer intento fallido la visita queda `bloqueada = true`, responde 409 `visit_locked` y se emite `VisitaBloqueada` para que Identidad abra un reporte de gravedad alta.
- **R-09:** Un código ya usado (visita `en_curso`) responde 409 `code_used`; uno vencido, 409 `code_expired`; una visita bloqueada, 409 `visit_locked`; una cancelada, 409 `visit_cancelled`; una `terminada` o `no_presentado`, 409 `visit_not_active`; una de taller, 409 `visit_without_code`. Ninguno de estos casos cuenta como intento.
- **R-10:** Repetir una llegada ya exitosa responde 409 `code_used` y no vuelve a emitir el evento ni a cambiar `llegadaEn`. Un intento fallido repetido vuelve a contar como intento.
- **R-11:** Si la visita no existe o el técnico no es el elegido, responde 404, incluso si el código es correcto. Un cliente que llama a `/llegada` recibe 403 `not_a_technician`.
- **R-12:** `POST /api/v1/visitas/{id}/aviso` exige JWT del cliente dueño y cuerpo vacío. Responde 200 `{ texto, incluyeCodigo: false, incluyeDireccion: false }`; el texto incluye el nombre público del técnico, su estado de verificación, el día, la hora y la recomendación de pedir el código en la puerta. No guarda contactos ni envía mensajes.
- **R-13:** El aviso solo se prepara con la visita en `confirmada` o `en_curso`; en otro estado responde 409 `visit_not_notifiable`. Cada aviso incrementa `avisosEnviados` y emite `AvisoConfianzaPreparado` con identificadores, sin teléfonos ni texto. `avisosEnviados` es solo informativo (cuenta cuántas veces se preparó el texto, no cuántos se enviaron): ninguna regla lo usa como límite ni bloquea el endpoint.
- **R-14:** Las respuestas llevan `Cache-Control: private, no-store`, sin `ETag`. El código y la dirección no se registran en los logs; los errores usan ProblemDetails con `correlationId`, `traceId` y `code` (convención de errores de la spec 020, §5); las respuestas 429 llevan `Retry-After`.
- **R-15:** El código se guarda cifrado (AES-256-GCM) con llave versionada en Key Vault y vence a las 23:59:59 del día de la cita más 24 horas. La comparación usa `CryptographicOperations.FixedTimeEquals`; nunca se registra ni se devuelve al técnico.
- **R-16:** El contador de intentos y el bloqueo son parte de la visita y no se reinician al reconectar. Un bloqueo solo lo levanta una persona del equipo (fuera de esta spec).
- **R-17:** Toda transición de la visita a `cancelada`, `terminada`, `no_presentado` o `bloqueada` escribe en el buzón de salida (spec 003), en la misma transacción que el cambio, el evento `VisitaCerrada { visitaId, motivo }` con `motivo` ∈ `cancelada`, `terminada`, `no_presentado`, `bloqueada`; solo lleva identificadores, sin datos personales. `VisitaBloqueada` (R-08) no cambia. Mensajería lo consume y marca la sala de la visita en Redis (spec 026 R-10). El nombre del evento es `VisitaCerrada` (decisión 13 de la sección 14).

## 4. Criterios de aceptación
- **CA-01:** Dada una visita a domicilio confirmada, cuando el cliente la abre, entonces ve al técnico, día, hora, el código de cuatro dígitos y la regla de seguridad.
- **CA-02:** Dada la misma visita, cuando la abre el técnico elegido, entonces la respuesta no contiene el código ni el teléfono del cliente.
- **CA-03:** Dado el documento de la visita, cuando se inspecciona, entonces el código está cifrado y no en claro.
- **CA-04:** Dado el código correcto, cuando el técnico confirma, entonces responde 200, la visita queda `en_curso`, se guarda `llegadaEn` y hay `VisitaEnCurso` en el buzón.
- **CA-05:** Dado un código incorrecto, cuando confirma, entonces responde 422 con `intentosRestantes` y no cambia el estado.
- **CA-06:** Dados tres intentos fallidos, cuando confirma el tercero, entonces la visita queda bloqueada, responde 409 y hay `VisitaBloqueada` en el buzón.
- **CA-07:** Dado un código ya usado, cuando confirma de nuevo, entonces responde 409 `code_used`.
- **CA-08:** Dada una visita cancelada o un código vencido, cuando confirma, entonces responde 409 y no cambia nada.
- **CA-09:** Dada una cita en taller, cuando el cliente la abre, entonces no hay código y ve la dirección; cuando el técnico intenta confirmar llegada, 409 `visit_without_code`.
- **CA-10:** Dada una visita de otra persona, cuando se pide o se intenta confirmar, entonces responde 404; con el token del cliente en `/llegada`, 403.
- **CA-11:** Dada una visita vigente, cuando el cliente pide el aviso, entonces el texto no contiene el código ni la dirección e incluye nombre, verificación, día y hora.
- **CA-12:** Dada una llegada exitosa, cuando se repite, entonces responde 409 `code_used` y no hay un segundo evento.
- **CA-13:** Dada una petición sin token, cuando se llama cualquier ruta, entonces responde 401.
- **CA-14:** Dado el registro de una llegada o de un aviso, cuando se inspecciona, entonces no contiene el código, la dirección ni el nombre del cliente.
- **CA-15:** Dado un `codigo` de tres dígitos, con letras o de cinco dígitos, cuando el técnico confirma, entonces responde 422 con `errors.codigo` y `intentos` no cambia.
- **CA-16:** Dada una visita cuyo día aún no empieza, cuando el técnico confirma con el código correcto, entonces responde 409 `outside_window`, el código sigue sin usar y `intentos` no cambia.
- **CA-17:** Dada una visita a domicilio confirmada, cuando el cliente la abre, entonces ve el contacto público del técnico (si lo publicó) y la dirección del lugar; cuando la abre el técnico, ve la dirección y la colonia del cliente, pero no su nombre ni su teléfono; y si la visita ya no está `confirmada` ni `en_curso`, o está bloqueada, el técnico no ve la dirección (solo la colonia).
- **CA-18:** Dada una visita `terminada` o `no_presentado`, cuando el técnico confirma o el cliente pide el aviso, entonces responde 409 `visit_not_active` o `visit_not_notifiable` y no cambia nada.
- **CA-19:** Dada una visita `confirmada`, cuando pasa a `cancelada`, `terminada`, `no_presentado` o `bloqueada`, entonces hay exactamente un `VisitaCerrada` con el `motivo` correcto en el buzón, en la misma transacción que el cambio; si el cambio falla, no hay evento. (R-17)

## 5. Contrato
| Método y ruta | Quién | Cuerpo | Respuestas |
|---|---|---|---|
| `GET /api/v1/visitas/{id}` | cliente dueño o técnico elegido | — | 200 (vista según rol, campo `rol`), 400, 401, 404, 429 |
| `POST /api/v1/visitas/{id}/llegada` | técnico elegido | `{ codigo }` | 200 (vista del técnico), 400, 401, 403, 404, 409, 413, 422, 429 |
| `POST /api/v1/visitas/{id}/aviso` | cliente dueño | vacío | 200 `{ texto, incluyeCodigo, incluyeDireccion }`, 400, 401, 404, 409, 429 |

**Códigos de error:** `wrong_code` (422, con `intentosRestantes`), `code_used`, `code_expired`, `visit_locked`, `visit_cancelled`, `visit_not_active`, `visit_without_code`, `outside_window`, `visit_not_notifiable` (todos 409), `not_a_technician` (403). Formato inválido: 422 con `errors`; visita ajena o inexistente: 404 uniforme.

**Caché:** ninguna respuesta se cachea (`Cache-Control: private, no-store`).

**Límites:** 3 intentos de código por visita; 5 peticiones de llegada por minuto por técnico. El aviso no tiene límite propio (decisión del 2026-11-12): preparar el texto no envía nada y bloquear una función de seguridad por contar toques sería peor que el abuso que evita; rige solo el límite general por usuario del gateway.

## 6. Modelo de datos
**`visitas`** (creada en la spec 021; esta spec la completa)
| Campo | Uso aquí |
|---|---|
| `codigoCifrado`, `codigoVenceEn` | lectura para el cliente; comparación en la llegada |
| `codigoUsado` | bloquea la reutilización |
| `intentos` | máximo 3 |
| `bloqueada` | la pone esta spec al tercer fallo |
| `llegadaEn` | hora real de llegada |
| `avisosEnviados` | contador entero |
| `estado` | `confirmada → en_curso`; `terminada`, `cancelada` y `no_presentado` los usan otras specs |

Índices: los de la spec 021; se agrega `{ estado: 1, codigoVenceEn: 1 }` para el proceso que vence códigos y `{ tecnicoUsuarioId: 1, estado: 1, dia: 1 }` para la agenda del técnico.

**Sin colecciones nuevas.** El aviso no guarda contactos ni texto; los contactos viven solo en el teléfono (HU-037).

**Cambios en `solicitudes`:** ninguno; la visita ya tiene la copia del precio y del tope.

## 7. Arquitectura y componentes
- `VisitasController` (lectura) y `LlegadaController` (confirmación) separados por responsabilidad (S6960); el aviso vive en `AvisoController`.
- `VisitaService` arma la vista del cliente y la del técnico con dos proyecciones distintas; el código solo se descifra en la del cliente y solo cuando corresponde.
- `LlegadaService` hace una sola actualización condicionada: `estado = confirmada`, `bloqueada = false`, `codigoUsado = false`, `intentos < 3`; si el código no coincide, incrementa `intentos` en la misma operación. Si el contador llega a 3, marca `bloqueada` y escribe el evento en el buzón.
- `AvisoService` arma el texto con la plantilla de la app; no guarda ni envía nada. La app abre la hoja de compartir del sistema.
- `CifradoDeCampo` y la llave versionada de la spec 021 se reutilizan; no se crea otra llave.
- Sin caché: la visita cambia con la llegada y el código es de un solo uso.

## 8. Dependencias
- Spec 021 (crea la visita y el código), 020 (solicitud y técnico), 003 (MongoDB, outbox), 009 (JWT y límites), 010 (sin caché personal), 013 (foto pública del técnico).
- Identidad consume `VisitaEnCurso`, `VisitaBloqueada` y `AvisoConfianzaPreparado` para avisos y reportes (consumidores fuera de esta spec).
- La app del cliente consume el contrato y guarda el código en almacenamiento seguro para verlo sin conexión (HU-036, CA2 de la ficha).
- La app del técnico aún no existe; la vista del técnico queda probada por contrato y semillas.

## 9. Amenazas y controles
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Fuerza bruta del código de 4 dígitos | 3 intentos, bloqueo, límite por minuto y evento de reporte | CA-05, CA-06 |
| Ver o usar la visita de otra persona | Autorización por `sub` en cliente y técnico; 404 uniforme | CA-10 |
| Fuga del código al técnico o a los registros | Proyección distinta por rol; no se registra; se cifra en reposo | CA-02, CA-03, CA-14 |
| Fuga de la dirección | Solo el cliente y el técnico elegido; no se registra | CA-09, CA-14 |
| Reutilizar el código | `codigoUsado` y condición de la actualización | CA-07 |
| Repetición de la llegada por red inestable | La visita ya `en_curso` responde 409 `code_used` sin repetir el evento | CA-12 |
| Suplantar al técnico | Se resuelve por `sub` y se comprueba que sea el elegido | CA-10 |
| Aviso que expone el código o la dirección | El texto se arma en el servidor sin esos campos y la respuesta lo declara | CA-11 |
| Enumerar visitas ajenas | 404 después de autenticar, sin distinguir inexistente de ajena | CA-10 |

## 10. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Unitarias | Sí | Ventanas de validez, intentos, bloqueo, textos y plantilla del aviso |
| Integración | Sí | MongoDB real: llegada correcta, incorrecta, bloqueo, repetición y eventos |
| Seguridad negativa | Sí | Visita ajena, rol equivocado, código vencido, usado y bloqueado |
| Contrato | Sí | OpenAPI de Mercado regenerado |
| Arquitectura | Sí | El controller no descifra ni toca MongoDB; el código no se registra |
| Mutación | Sí | Condición de intentos y de estado de la llegada |
| Rendimiento | No | Una visita por solicitud y un puñado de intentos |
| Aceptación | Sí | CA-01 a CA-19 con la tabla 4.1 |
| Base de datos | Sí | `DatabaseTest`: índices nuevos de `visitas` creados de forma idempotente |
| Regresión | Sí | Las suites de las specs 020 y 021 y de 015 a 019 siguen en verde; `dotnet test Knock.slnx -c Release` completo |
| CI/CD | Sí | Job `gate` de `ci-pr.yml` (build-test, openapi-diff, sast, sca, secrets) en verde; smoke de `/health/ready` tras el despliegue |
| Análisis estático | Sí | Semgrep (job `sast`) sin hallazgos nuevos; regla que prohíbe registrar `codigo`; complejidad máxima 10 |
| Vulnerabilidades y licencias | Sí | NuGetAudit, `dotnet list package --vulnerable`, OSV-Scanner y dependency-review sin altas ni críticas; sin dependencias nuevas |
| DAST | Sí (nocturno) | ZAP de API con token de prueba contra las tres rutas; no bloquea el PR |

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `Spec022VistaTests.Cliente_Ve_Codigo_CA_022_01` |
| CA-02 | `Spec022VistaTests.Tecnico_No_Ve_Codigo_CA_022_02` |
| CA-03 | `Spec022CodigoTests.Codigo_Cifrado_CA_022_03` |
| CA-04 | `Spec022LlegadaTests.Llegada_Correcta_CA_022_04` |
| CA-05 | `Spec022LlegadaTests.Codigo_Incorrecto_CA_022_05` |
| CA-06 | `Spec022LlegadaTests.Bloqueo_Tras_Tres_Intentos_CA_022_06` |
| CA-07 | `Spec022LlegadaTests.Codigo_Usado_CA_022_07` |
| CA-08 | `Spec022LlegadaTests.Visita_Cancelada_O_Vencida_CA_022_08` |
| CA-09 | `Spec022VistaTests.Cita_En_Taller_CA_022_09` |
| CA-10 | `Spec022SeguridadTests.Visita_Ajena_CA_022_10` |
| CA-11 | `Spec022AvisoTests.Texto_Sin_Codigo_Ni_Direccion_CA_022_11` |
| CA-12 | `Spec022LlegadaTests.Llegada_Repetida_Responde_409_CA_022_12` |
| CA-13 | `Spec022SeguridadTests.Exige_Sesion_CA_022_13` |
| CA-14 | `Spec022RegistrosTests.Sin_Datos_Sensibles_CA_022_14` |
| CA-15 | `Spec022LlegadaTests.Codigo_Mal_Formado_No_Cuenta_CA_022_15` |
| CA-16 | `Spec022LlegadaTests.Fuera_De_Ventana_CA_022_16` |
| CA-17 | `Spec022VistaTests.Contacto_Y_Lugar_Por_Rol_CA_022_17` |
| CA-18 | `Spec022LlegadaTests.Visita_No_Vigente_CA_022_18` |
| CA-19 | `Spec022CierreTests.Cierre_Emite_VisitaCerrada_CA_022_19` |

**Gates:** los de `AGENTS.md`; contrato regenerado; la prueba del aviso compara el texto completo para que no se cuele el código por descuido.

## 11. Escenarios
**Positivos**
- Visita a domicilio: el cliente ve el código y el técnico confirma con él.
- Cita en taller: el cliente ve la dirección y el técnico no tiene botón de llegada.
- El cliente comparte el aviso por WhatsApp; el mensaje no lleva código ni dirección.

**Negativos**
- El técnico equivocado intenta confirmar.
- El cliente intenta confirmar su propia llegada.
- Se usa el código después de la llegada.
- Se comparte el código con un tercero y se agotan los intentos.

**Errores esperados**
- 400 por identificador mal formado, 401 sin sesión, 403 por rol, 404 por visita ajena, 409 por estado, 422 por código incorrecto, 429 por límite.

**Casos límite**
- Código con ceros a la izquierda.
- Llegada un minuto antes y un minuto después de la ventana.
- Visita que vence mientras se escribe el código.
- Repetición después de que el cliente ya vio la visita.
- Aviso con el técnico sin verificar: el texto lo dice.

## 12. Definición de terminado
- Los 18 criterios tienen prueba en verde y la tabla 4.1 coincide con los nombres reales (`python scripts/sdd_lint.py --trace 022` sin faltantes).
- Job `gate` en verde, Semgrep y análisis de dependencias sin hallazgos altos ni críticos.
- La prueba de bloqueo comprueba el contador, el estado y el evento.
- El texto del aviso está probado carácter por carácter contra el código y la dirección.
- OpenAPI regenerado, `docs/endpoints.md` actualizado y `Memoria.md` con la entrada.
- Revisión de par y sin hallazgos de seguridad abiertos.

## 13. Decisiones
- **El código se cifra y no se hashea:** el cliente tiene que poder volver a verlo; con un hash no se podría. La protección es la llave de Key Vault, el acceso por dueño y el tiempo de vida.
- **Tres intentos y bloqueo:** un código de cuatro dígitos sin límite se adivina en minutos; el bloqueo convierte el ataque en un evento visible.
- **La llegada es del técnico, no del cliente:** el cliente ya sabe que está en su casa; el técnico demuestra que es quien fue elegido.
- **El aviso se arma en el servidor:** así se garantiza que no lleve el código ni la dirección, aunque la app cambie.
- **No hay código en taller:** el técnico va a su propio negocio; el código protege la entrada a una casa, no una cita en un local.

## 14. Decisiones tomadas sin consulta
1. **Se agrega `POST /visitas/{id}/aviso`.** El insumo lo propone; la app sola podría armar el texto, pero el servidor garantiza que no se filtren el código ni la dirección. Alternativa: dejar el aviso solo en la app y no tener nada que probar en el backend.
2. **El aviso no incluye el código, aunque la ficha de HU-037 lo menciona.** La descripción del proyecto y la regla de seguridad dicen que el código nunca se dicta ni se comparte; incluirlo en un mensaje que se reenvía anularía la protección. Se anota como conflicto con la ficha.
3. **El código se guarda cifrado, no como huella.** La descripción dice “huella”, pero el cliente necesita volver a verlo al abrir la visita y sin conexión; con huella no se puede. La alternativa —devolverlo una sola vez y confiar en la caché del teléfono— se pierde al reinstalar.
4. **Bloqueo tras el tercer intento con 409, no 423.** 423 es de WebDAV y no describe el error; 409 con `visit_locked` es coherente con el resto del contrato.
5. **La vista del técnico no lleva el nombre del cliente.** Market no lo tiene y no se quiere duplicar el dato de Identidad; en este sprint el técnico tampoco lo recibe por el chat (la spec 026 no envía identidad en los mensajes). Alternativa: pedirlo a Identidad, que abre una interfaz entre servicios que no está en este sprint.
6. **El aviso no se envía ni se guarda.** Los contactos de confianza viven solo en el teléfono (HU-037); guardarlos en el servidor sería un dato personal nuevo y un riesgo que la historia no pide.
7. **Ventana de la cita:** la llegada se permite del inicio del día de la cita a las 23:59:59 de ese día, más 24 h para cubrir retrasos. La ficha no fija la ventana; se elige una generosa y se documenta.

**Agregadas en la revisión del 9 de noviembre**

8. **Un código mal formado o fuera de ventana no cuenta como intento.** Solo un código de cuatro dígitos comparado contra el real es un intento de adivinar; contar los errores de captura bloquearía visitas por un dedo. Alternativa: contar todo, más estricto pero con bloqueos accidentales.
9. **La vista del cliente incluye el contacto público del técnico.** La spec 021 dice que el contacto se ve «en la respuesta de la elección o en el detalle de la visita»; el borrador de esta spec no lo traía. Alternativa: solo en la elección, que lo pierde al reabrir la app.
10. **Las respuestas llevan `rol`.** Mensajería (spec 026) y la app distinguen la vista sin inferirla por los campos presentes.
11. **El aviso no lleva clave de idempotencia.** No crea recursos ni envía nada; repetirlo solo incrementa un contador.
12. **Mensajería consulta esta vista para la pertenencia (spec 026).** Como la vista del cliente contiene el código y la dirección, la spec 026 deserializa solo `rol` y `estado` y no registra el cuerpo. Alternativa: una ruta interna de pertenencia, que agrega una superficie nueva; queda como mejora si se aprueba.
13. **El evento de cierre se llama `VisitaCerrada`, no `visita_cerrada`.** Sigue la convención PascalCase de los eventos de dominio de Mercado (`VisitaBloqueada`, `VisitaEnCurso`), de modo que Mensajería y el despachador de avisos reciben todos los eventos de visita con el mismo estilo de nombre. `visita_cerrada` solo se usó como nombre de trabajo en la revisión y no se usa en el código. Alternativa: snake_case, descartada porque rompe la convención del módulo.

## 15. Riesgos y dudas abiertas
- **`Idempotency-Key` retirada por ahora (decisión del 2026-11-10).** Se quitó de la llegada. Sin ella, si la respuesta de una llegada exitosa se pierde, el reintento recibe 409 `code_used`: la app debe consultar la visita (`GET /api/v1/visitas/{id}`) antes de mostrar un error. Se retoma con una spec propia si se decide volver a exigirla.
- **No hay consumidor de `VisitaBloqueada`:** el evento queda en el buzón y el reporte de gravedad alta de HU-038 CA3 no se crea hasta que Identidad lo consuma. Es la brecha más importante de esta spec.
- **El bloqueo no se puede levantar desde la API:** lo hará el panel (spec de verificación y reportes). Mientras tanto, una visita bloqueada requiere intervención en la base.
- **La ventana de la cita es una decisión:** si el equipo quiere una ventana más corta (por ejemplo, dos horas antes y cuatro después), se cambia en un solo lugar.
- **El código de cuatro dígitos es débil por naturaleza:** el control es el límite de intentos, no la entropía; no se debe reutilizar en otra visita.
- **La app del técnico no existe:** la confirmación de llegada queda probada por contrato, semillas e integración, pero no en un teléfono real.
- **El aviso por WhatsApp o SMS depende del sistema operativo:** la app puede no tener WhatsApp instalado; el texto debe seguir siendo legible si se copia y pega.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Implementada (tras el merge)
