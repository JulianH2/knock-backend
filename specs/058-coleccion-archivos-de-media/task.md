# Tareas 058 — Colección `archivos` de Media: registro de archivos privados en MongoDB y tope de subidas pendientes

> **Estado:** pendientes. En esta etapa **no se ejecutan**: se activan cuando la spec esté Aprobada y una persona lo indique.
> **Orden:** T-01 → T-02 → T-03 y T-04 (en paralelo entre sí, ambas dependen de T-02) → T-05 (después de T-03) → T-06 → T-07. Las decisiones D1 a D8 de la spec ya están contestadas (2026-11-20); T-08 es independiente y puede ir en paralelo con T-03 y T-04.

| Tarea | Qué se hace | Cubre | Depende de | ¿En paralelo? | Responsable sugerido | Estado |
|---|---|---|---|---|---|---|
| T-01 | Script de `db/` (siguiente número libre al implementar) con la colección `archivos`, el índice único por `archivoId`, el compuesto `propietarioId`/`estado`/`confirmarAntesDe` y el TTL de `expiraEn`; `Spec058ArchivosScriptTests` en `DatabaseTest` (índices, idempotencia, TTL y `explain`) | R-01, R-02, R-03, R-07 · CA-01, CA-04, CA-20 | — (D2 y D7 decididas) | No (bloquea T-02) | Alexis | pendiente |
| T-02 | `MongoMediaFileStore` con `IMediaFileStore` en camelCase (`_id` ObjectId, `archivoId`, `version`), `MediaFile` con `confirmarAntesDe` y `expiraEn`, registro en `AddMediaModule`; las pruebas actuales del módulo siguen en verde y los nombres de blob no cambian | R-02, R-04, R-05, R-06, R-13 · CA-02, CA-03, CA-05, CA-06, CA-11 | T-01 | No (bloquea T-03 y T-04) | Alexis | pendiente |
| T-03 | Migrador de `registros-temporales` a `archivos`: lectura de los JSON antiguos, traducción, inserción idempotente por `archivoId`, descarte de pendientes caducados con borrado de su blob de cuarentena, conteos y fallidos sin datos personales; pruebas con Azurite y MongoDB | R-08, R-12, R-16, R-17 · CA-12, CA-13, CA-14, CA-15 | T-02 | Sí (con T-04) | Alexis | pendiente |
| T-04 | Tope de subidas pendientes: método de «insertar y verificar» en el almacén, `LimitExceeded` en `MediaService.CreateUploadAsync`, valores configurables y validados al arrancar, `Retry-After`; pruebas unitarias y de concurrencia | R-09, R-10, R-11, R-12 · CA-07, CA-08, CA-09, CA-10 | T-02 | Sí (con T-03) | Alexis | pendiente |
| T-05 | Retirar `BlobMediaFileStore`, `Media:RecordsContainer` y toda referencia a `registros-temporales`; prueba de arquitectura; dejar listo el paso manual de borrar el contenedor en Azure (no se ejecuta; lo hace una persona tras verificar la migración) y revisar si algún Terraform lo declara | R-15, R-16 · CA-16 | T-03 | No | Alexis | pendiente |
| T-06 | Pruebas de seguridad y concurrencia repetidas: dueño solo del token, archivo ajeno, `archivoId` UUID v7 no enumerable, registros y métricas sin datos, y 25 subidas simultáneas | R-05, R-10, R-12, R-13 · CA-06, CA-08, CA-17, CA-18, CA-19 | T-04, T-05 | No | Alexis | pendiente |
| T-07 | Contrato y cierre: prueba de contrato y OpenAPI sin diferencias, documento del flujo privado (429 `limit_exceeded`), `CODEMAP`, `Memoria.md` y PR con «Uso de IA» | R-14 · CA-21, CA-22 | T-06, T-08 | No (cierre) | Alexis | pendiente |
| T-08 | Alinear la spec 020 con D4 en el código: regex de `fotos` a 32 hexadecimales en `CreateRequestBodyValidator`, comentario del DTO, `docs/openapi.v1.json` regenerado y `Spec020FotosTests` (la enmienda del texto de la spec 020 ya está hecha); `Spec058ContractTests` del CA-23 | R-18 · CA-21, CA-23 | T-02 | Sí (con T-03 y T-04) | Guillermo (dueño de la 020) | pendiente |
| T-09 | Regla de ciclo de vida de 1 día en `cuarentena` y revisión del contenedor `registros-temporales` en Azure (cero JSON: solo borrarlo; con JSON: migrar y verificar). Lo hace una persona con permiso; el agente no ejecuta nada en Azure | R-16, R-17, R-19 | T-05 | No | Persona con permiso en Azure (Tech Lead) | pendiente |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas o comandos en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
