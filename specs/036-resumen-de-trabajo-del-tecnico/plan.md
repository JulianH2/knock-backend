# Plan 036 — Resumen de trabajo del técnico

> Parte de `spec.md` (Aprobada); la implementación se activa por tarea con indicación de una persona.
> **Responsable:** Guillermo (módulo de Mercado) · **Sprint:** 5 (propuesto)

## Enfoque
1. Añadir `TecnicoTrabajoService` con cuatro consultas independientes (visitas próximas, cotizaciones enviadas, terminadas paginadas y resumen) y componer la respuesta única de R-02.
2. Escribir las reglas puras: dirección solo en `confirmada` o `en_curso` (R-03), `motivoRechazo` (R-04), `disponibleHoy` por omisión (R-05) y orden con desempate de `terminadas` (R-02).
3. Derivar `verificacion.estado` de `nivelVerificacion` en `tecnicos`: `basica` y `completa` dan `verificada`; `ninguno` y valores ausentes o desconocidos dan `sin_verificar`. Dejar `nivel` en `null` hasta la spec 051 y mapear fallas de base a `503 unavailable`, sin valores de respaldo (R-06, R-07).
4. Exponer `GET /api/v1/tecnicos/me/trabajo` con `Cache-Control: private, no-store`, cursor de `terminadas` (spec 010) y el límite de lectura del gateway (R-08, R-10).
5. Documentar la ruta en OpenAPI, `docs/endpoints.md` y `docs/rate-limit-matrix.md`, y la decisión en `Memoria.md`.

## Archivos que se crean o cambian
- `src/Knock.Market.Api/Modules/Tecnicos/` (`Controllers/V1/TrabajoController.cs`, `Services/TecnicoTrabajoService.cs`, `Domain/ReglasDeTrabajo.cs`, `Dtos/`, `Validators/`, `Data/TrabajoStore.cs`)
- `src/Knock.Market.Api/Program.cs` (registro del módulo)
- `tests/` (clases `Spec036*` con MongoDB y Redis reales)
- `contracts/openapi.v1.json` (regenerado), `docs/endpoints.md`, `docs/rate-limit-matrix.md`, `Memoria.md`

## Dependencias
- Spec 020 (técnico, cotizaciones y solicitudes) y 021 (motivo de rechazo y visita confirmada) fusionadas o con datos de semilla.
- Spec 022 (dirección solo tras elegir y `puedeLlegar`) para la forma de `proximasVisitas`.
- Spec 025 (evento que actualiza `nivelVerificacion` en `tecnicos`) fusionada o, antes, datos de semilla de verificación; la reputación y su `nivel` esperan a la spec 051.
- Spec 010 (cursor y caché) y 009 (límite de lectura del gateway).
- Frontend 019 consume la respuesta; su cliente se regenera después de que este contrato esté en OpenAPI.

## Orden y paralelismo
- T-01 (índices) y T-02 (reglas puras) arrancan juntas; T-03 (lectura de verificación y nivel desde `tecnicos`) puede correr en paralelo con T-02.
- T-04 (servicio y controlador) espera a T-02 y T-03.
- Pruebas de integración y seguridad (T-05) y humo/E2E (T-06) cierran; T-07 (OpenAPI y docs) corre en paralelo con T-06.
- No bloquea ninguna cadena de Mercado; frontend 019 puede avanzar con su doble mientras tanto.

## Riesgos
- Si la base no responde, toda la pestaña falla (R-07). Se mide el tiempo límite de las cuatro consultas en la prueba de CA-05 y se revisa con Alexis antes de la fusión.
- Cuatro consultas por respuesta: se usan índices de `visitas` y `cotizaciones` (T-01) y se mide el tiempo en la prueba de integración.

## Verificación
- `dotnet test Knock.slnx -c Release` con las clases `Spec036*` en verde, cada CA con su prueba nombrada `_CA_036_NN`.
- `oasdiff` contra `contracts/openapi.v1.json` sin cambios incompatibles fuera de la ruta nueva.
- `python scripts/sdd_lint.py --trace 036` sin `FALTA`; `Memoria.md` al día.
