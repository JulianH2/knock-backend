# 057 — tareas

> Estado: pendientes. No se ejecutan hasta que la spec esté Aprobada y una persona lo indique.

## T1 — Campos y script de la purga [BD]
- **Crear:** `db/057-identity-verificaciones-purga.js` (idempotente); `Spec057InvariantesTests`.
- **Modificar:** el modelo de `verificaciones` (`alertadaEn`, `purgadaEn`, `razonPurga`).
- **Verificar:** `dotnet test tests/DatabaseTest` (TC-HU-068-08).
- **No hacer:** editar el script de la spec 025.
- **Agente:** `db-builder`.

## T2 — Servicio de purga [Back]
- **Crear:** `PurgaDeVerificacion` con transición condicionada, destrucción de llave, borrado de blobs, auditoría y evento.
- **Modificar:** `VerificacionStore`, `FotoVerificacionStore`.
- **Verificar:** unitarias e integración de TC-HU-068-01, 05, 06, 07, 09, 10.
- **No hacer:** borrar blobs o llaves desde otro lugar; registrar rutas, CURP o llave.
- **Agente:** `api-builder`. **Depende de:** spec 025 T-01, T-03, T-04.

## T3 — Manejadores de tarea y programación [Back]
- **Crear:** `AvisarAlVerificadorHandler`, `BorrarFotosDeVerificacionHandler`, eventos.
- **Modificar:** `VerificacionService.Enviar` (programa ambas tareas en la misma transacción) y el registro de manejadores.
- **Verificar:** TC-HU-068-02, 03, 04, 13 con `TimeProvider` falso y dos instancias.
- **No hacer:** tipos de tarea nuevos fuera del catálogo de la spec 054.
- **Agente:** `api-builder`. **Depende de:** T2 y spec 054 (ejecutor de Identidad).

## T4 — Decisión y baja de cuenta [Back]
- **Modificar:** `VerificacionDecisionService` (purga al decidir) y el manejador de `EliminarCuenta` (spec 039).
- **Verificar:** TC-HU-068-01 y 11.
- **No hacer:** crear el endpoint de decisión (es de la spec 053 / HU-067).
- **Agente:** `api-builder`. **Depende de:** T2.

## T5 — Seguridad negativa y registros [Pruebas]
- **Crear:** `Spec057AccesoTests`, `Spec057RegistrosTests`; regla de arquitectura de borrado único.
- **Verificar:** TC-HU-068-12 y 14; `semgrep scan --config .semgrep/semgrep.yaml --error`.
- **Agente:** `api-builder`; revisa `security-reviewer`.

## T6 — Documentación y cierre
- **Modificar:** `docs/events.md`, `Memoria.md`, spec conciliada con las rutas reales de prueba.
- **Verificar:** gates de `AGENTS.md §5`, `traceability-clerk` sin drift.
- **Agente:** `docs-keeper`.
