# TT-API-07 — tareas

> Servicio afectado: Mercado (módulo Catalog). Borrador: nada implementado.

## T1 — Enlace y validación de coordenadas en la frontera
- **Estado:** pendiente de aprobación de la spec. Decidido (líder, #54, 2026-10-26, opción 2 de (j)): DTO con `Lat` y `Lon` de tipo `string` con un binder de un solo valor que rechaza el repetido con 422 y `[ValidateNever]`, sin parámetros tipados; hasta 10 decimales (decisión (d)).
- **Verificar:** TC-TT-API-07-01 · TC-TT-API-07-02 · TC-TT-API-07-06 · TC-TT-API-07-15

## T2 — Endpoint con sesión, sin caché y bajo el límite global
- **Estado:** pendiente. Decidido (líder, #54, 2026-10-26): sin colonia → `200` con `colonia: null` (b); requiere sesión JWT (c).
- **Verificar:** TC-TT-API-07-07 · TC-TT-API-07-09 · TC-TT-API-07-10 · TC-TT-API-07-11

## T3 — Contrato OpenAPI
- **Estado:** pendiente. `lat` y `lon` como `string` obligatorio con el patrón fijo y `maxLength: 16`, sin transformador (decisión (j), opción 2, 2026-10-26).
- **Verificar:** TC-TT-API-07-13

## T4 — Integración con la consulta interna de Catalog en MongoDB
- **Estado:** pendiente de TT-BD-06 (KNOCK-156) y de su consulta interna (TC-TT-BD-06-19)
- **Verificar:** TC-TT-API-07-03 · TC-TT-API-07-04 · TC-TT-API-07-05 · TC-TT-API-07-08 · TC-TT-API-07-12

## T5 — Privacidad de la query en los logs
- **Estado:** pendiente. Aquí solo la guarda del nivel de `Microsoft.AspNetCore`; la redacción de `Microsoft.AspNetCore.Hosting.Diagnostics` es una tarea aparte de seguimiento de TT-API-03 (decisión (i)).
- **Verificar:** TC-TT-API-07-08 · TC-TT-API-07-16

## T6 — Rendimiento local
- **Estado:** pendiente de TT-BD-06. Umbrales decididos (decisión (e)): informa contra 150 ms y en el CI falla con más de 500 ms; 150 ms con `KNOCK_PERF_STRICT=1`.
- **Verificar:** TC-TT-API-07-14

## T7 — Documentos vivos y gates
- **Estado:** pendiente
- **Verificar:** TC-TT-API-07-17 · TC-TT-API-07-18
