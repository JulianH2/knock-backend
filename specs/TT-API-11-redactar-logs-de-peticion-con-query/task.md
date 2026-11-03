# TT-API-11 — tareas

> Borrador. Ninguna tarea empieza hasta que la spec esté Aprobada y el líder responda las dudas 1 y 2 de spec §8.

## T1 — Pruebas que fallan primero
- **Estado:** pendiente de aprobación de la spec. `RequestLogQueryTests` con los casos 01 a 04; deben **fallar** contra el estado actual con los niveles bajados.
- **Verificar:** TC-TT-API-11-01 · TC-TT-API-11-02 · TC-TT-API-11-03 · TC-TT-API-11-04

## T2 — Filtro de la categoría de hospedaje
- **Estado:** pendiente. `Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Diagnostics"))` en `LoggingExtensions.AddKnockLogging`, antes de los sinks.
- **Verificar:** TC-TT-API-11-01 · TC-TT-API-11-02 · TC-TT-API-11-04

## T3 — Regresión y mutación
- **Estado:** pendiente. Las pruebas de TT-API-03 y TT-API-07 en verde sin editar; mutación manual (quitar el filtro) con salida en el PR.
- **Verificar:** TC-TT-API-11-03 · TC-TT-API-11-05

## T4 — Gates y cierre
- **Estado:** pendiente. Build, formato y gobierno en verde; `Memoria.md`; informe de `story-workflow` en el PR.
- **Verificar:** TC-TT-API-11-06

## Fuera de esta tarea (hasta que el líder responda)
- Otras categorías que registren una URL con query (cliente HTTP saliente, Kestrel): duda 2 de la spec.
