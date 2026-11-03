# TT-API-11 — tareas

> Borrador. Ninguna tarea empieza hasta que la spec esté Aprobada (dudas 1 y 2 resueltas por el líder el 2026-11-03; la 5 por confirmar).

## T1 — Pruebas que fallan primero
- **Estado:** pendiente de aprobación de la spec. `RequestLogQueryTests` con los casos 01 a 04; deben **fallar** contra el estado actual con los niveles bajados.
- **Verificar:** TC-TT-API-11-01 · TC-TT-API-11-02 · TC-TT-API-11-03 · TC-TT-API-11-04 · TC-TT-API-11-07 · TC-TT-API-11-08

## T2 — Filtro de la categoría de hospedaje
- **Estado:** pendiente. `Filter.ByExcluding(Matching.FromSource(...))` para `Microsoft.AspNetCore.Hosting.Diagnostics` y `Microsoft.AspNetCore.Server.Kestrel.BadRequests`, con la lista en una constante, en `LoggingExtensions.AddKnockLogging`, antes de los sinks.
- **Verificar:** TC-TT-API-11-01 · TC-TT-API-11-02 · TC-TT-API-11-04

## T3 — Regresión y mutación
- **Estado:** pendiente. Las pruebas de TT-API-03 y TT-API-07 en verde sin editar; mutación manual (quitar el filtro) con salida en el PR.
- **Verificar:** TC-TT-API-11-03 · TC-TT-API-11-05

## T4 — Gates y cierre
- **Estado:** pendiente. Build, formato y gobierno en verde; `Memoria.md`; informe de `story-workflow` en el PR.
- **Verificar:** TC-TT-API-11-06

## Fuera de esta tarea
- Excluir `System.Net.Http.HttpClient` y `Microsoft.AspNetCore.Server.Kestrel` enteras: duda 5 de la spec (hoy solo se vigilan con los casos 07 y 08).
