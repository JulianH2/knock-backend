# Tareas 006 — Escalabilidad, balanceo y salud de los servicios

> **Estado:** T-01, T-02, T-04 y T-05 hechas (PR #61 fusionado; k6 de KNOCK-88 del 2026-11-14). T-03 parcial: KNOCK-247 conecta Redis, pero durante una caída los límites vuelven a memoria por copia por decisión del Tech Lead; esta degradación no satisface literalmente R-05.

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Agregar la variable de réplicas y las sondas de salud | R-01, R-02, R-06 · CA-03 | hecha |
| T-02 | Configurar los encabezados reenviados confiables | R-03 | hecha |
| T-03 | Implementar el almacén compartido de tokens cancelados y contadores en Redis | R-04, R-05 · CA-02 | **parcial**: Redis compartido en Identity y Market; respaldo en memoria por copia si Redis falla, por decisión del Tech Lead (KNOCK-247) |
| T-04 | Probar con dos copias y apagar una | R-01 · CA-01 | hecha |
| T-05 | Prueba de carga k6 con 2 instancias contra el gateway | R-01 · CA-01 | hecha (KNOCK-88): k6 del 2026-11-14 contra el despliegue con 2 réplicas, 0 % de fallos y p95 de 158.76 ms; con la caída de una copia de Market, 0 % y 153.07 ms (detalle en KNOCK-88) |

## Notas
- **T-01:** `deploy/apps/variables.tf` fija min=max=2 por defecto en los cuatro servicios (R-01); el comentario explica cómo bajar a 1 fuera de la ventana (R-06). Redis queda en 1 (`main.tf`). Las sondas ya estaban: el módulo `deploy/modules/service` usa `/health` y `/health/ready` por defecto y `deploy/apps` no las sobreescribe (R-02). **No verificado con terraform** (no hay terraform en esta máquina): hay que correr `terraform fmt -check` y `terraform validate` (init `-backend=false`) donde exista.
- **T-02:** `Knock.Messaging.Api` registra `AddKnockForwardedHeaders()` y `UseForwardedHeaders()` igual que Identity/Market; `deploy/apps/main.tf` le pasa `ForwardedHeaders__KnownNetworks__0`. Pruebas `_R_006_03` en `tests/UnitTest/Hosting/Spec006ForwardedHeadersTests.cs` (configuración y suplantación ignorada desde IP no confiable, aceptada desde red confiable).
- **T-03 (KNOCK-247):** sesiones canceladas, cachés y contadores usan Redis. Identity y Market comparten los límites por política y partición mientras Redis responde; si falla, mantienen un límite en memoria por copia y registran un aviso sin datos personales. Market sin Redis configurado también usa memoria. **R-05 queda parcial:** durante la degradación el límite efectivo puede multiplicarse por el número de copias. Las copias temporales esperan extracción en KNOCK-251 y excepción del Tech Lead; sin commit ni revisión todavía.
- **T-04:** `tests/IntegrationTest/Identity/Spec006ScalingTests.cs` (`_CA_006_01`..`_CA_006_03`) con dos `IdentityApiFactory` compartiendo MongoDB y Redis de Testcontainers. CA-01/CA-02 requieren el emulador de Firebase Auth (`FIREBASE_AUTH_EMULATOR_HOST=127.0.0.1:9099`).
- **T-05 (KNOCK-88):** k6 en `tests/perf/spec006-two-replicas.js` (carga constante contra `GET /health/ready` y `GET /api/v1/oficios` del gateway vía `BASE_URL`; umbrales `http_req_failed < 1 %` y `p95 < 500 ms`; check sin `5xx`; escenario de caída de una copia manual documentado en el script) y `tests/perf/spec006-two-replicas.md` (local y Azure). No había k6 previo de la spec 010 en el repo (`tests/PerformanceTest` es una biblioteca .NET vacía), así que el script se creó desde cero. `k6` **no está instalado** en esta máquina y no hay servicios levantados en local: no se corrió ninguna pasada; la salida real queda pendiente de la primera corrida con k6.

## Verificación
- `dotnet build Knock.slnx -c Release -warnaserror`: 0 errores, 0 advertencias.
- `dotnet format Knock.slnx --verify-no-changes`: sin cambios.
- `dotnet test tests/UnitTest`: 778/778. `tests/SecurityTest`: 286/286. Integración `Spec006`: 3/3 (Docker una vez, con emulador de Firebase).

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
