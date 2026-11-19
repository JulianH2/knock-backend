# Plan 037 — Registro de dispositivos y despachador de avisos push

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.
> **Responsable:** Alexis · **Revisión de par:** Guillermo · **Sprint:** 5 (propuesto)

## Enfoque
1. Añadir el módulo `Avisos` en Mensajería con `DispositivosController` y `DispositivosService`: registro idempotente por token (R-01), reasignación al iniciar sesión, borrado al cerrar sesión (R-02) y cifrado del identificador (R-03).
2. Escribir las reglas puras: carga v1 con lista cerrada de campos (R-04), solo datos sin bloque `notification` (R-05), clave de deduplicación (R-06) y ventana de 5 minutos de `mensaje_nuevo` (R-07).
3. Escribir `DespachadorDeAvisos`: lee el buzón de salida de Mercado y Mensajería (spec 003), deduplica, consulta la presencia `chat:presencia:{sub}` (R-08), envía por Firebase con reintentos (R-09) y registra en `notifications.enviados` sin datos personales.
4. Escribir la lectura de credenciales de Firebase desde el almacén de secretos del despliegue (R-10), sin valores en el repositorio.
5. Documentar las rutas en OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md`, y la decisión en `Memoria.md`.

## Archivos que se crean o cambian
- `src/Knock.Messaging.Api/Modules/Avisos/` (`Controllers/V1/DispositivosController.cs`, `Services/DispositivosService.cs`, `Services/DespachadorDeAvisos.cs`, `Domain/CargaV1.cs`, `Domain/VentanaMensajeNuevo.cs`, `Data/DispositivosStore.cs`, `Data/EnviadosStore.cs`, `Data/PendientesStore.cs`, `Dtos/`, `Validators/`, `Options/AvisosOptions.cs`)
- `src/Knock.Messaging.Api/Modules/Avisos/Firebase/` (cliente HTTP de FCM con interfaz `IEnvioPush`, para poder simular)
- `src/Knock.Messaging.Api/Program.cs` (`AddAvisosModule()`) y `appsettings.json` (`Avisos:*`, sin secretos)
- `tests/` (clases `Spec037*` con MongoDB y Redis reales y Firebase simulado)
- `contracts/openapi.v1.json` (regenerado), `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `Memoria.md`

## Dependencias
- Spec 003 (buzón de salida, MongoDB, transacciones): sin el buzón no hay eventos que despachar.
- Specs 026 y 030 (presencia por grupo `usuario:{sub}` y eventos del hub).
- Spec 020 y 021 (eventos `CotizacionEnviada`, `CotizacionAceptada` y los de solicitud).
- Frontend 012 (registro del identificador de dispositivo) y frontend 024 (catálogo de avisos y carga v1 de ejemplo en `app/test/fixtures/avisos/*.json`).
- Riesgo de transporte (spec 037 §8.1): si el buzón no alcanza el volumen, hay que abrir una spec de transporte antes de cerrar CA-03 de punta a punta.

## Orden y paralelismo
- T-01 (índices y prueba de base) y T-02 (reglas puras) arrancan juntas; T-03 (registro) y T-04 (despachador) siguen a T-02.
- T-05 (integración y seguridad negativa) integra registro y despachador.
- T-06 (contrato y arquitectura) y T-07 (carga y humo) corren después de T-05; T-08 (OpenAPI, docs y secretos) corre en paralelo con ellas.
- Frontend 012 y 024 pueden probar contra un doble de la API mientras tanto.

## Riesgos
- **Transporte de eventos entre servicios.** Sin el buzón de salida de Mercado y Mensajería en producción, el despachador no tiene entrada. Se escala a José antes de Sprint 5.
- **Presencia obsoleta.** Si el hub no renueva la clave cada 15 segundos, se envían avisos a personas conectadas (fallo barato, R-08). Se mide en CA-06.
- **Límites de Firebase.** La ráfaga de CA de carga se acota y se revisa la cuota del proyecto antes de activarla en producción.

## Verificación
- `dotnet test Knock.slnx -c Release` con las clases `Spec037*` en verde; cada CA con su prueba nombrada `_CA_037_NN`.
- `oasdiff` contra `contracts/openapi.v1.json` sin cambios incompatibles fuera de las dos rutas nuevas.
- Escaneo de secretos del CI sin hallazgos en `appsettings.json` ni en el código.
- `python scripts/sdd_lint.py --trace 037` sin `FALTA`; `Memoria.md` al día.
