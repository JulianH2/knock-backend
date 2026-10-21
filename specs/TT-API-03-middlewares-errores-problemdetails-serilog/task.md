# TT-API-03 — tareas

## T1 — Evaluación de paquetes y orden del pipeline
- **Estado:** pendiente de aprobación de la spec (evaluar `FluentValidation` y su extensión de DI; escribir `docs/MIDDLEWARES.md` y `docs/rate-limit-matrix.md`)
- **Verificar:** TC-TT-API-03-16 · TC-TT-API-03-17

## T2 — Correlación (`X-Correlation-Id`) y logs
- **Estado:** pendiente
- **Verificar:** TC-TT-API-03-08 · TC-TT-API-03-09 · TC-TT-API-03-10 · TC-TT-API-03-11 · TC-TT-API-03-12 · TC-TT-API-03-15

## T3 — Manejador de excepciones de dominio a ProblemDetails
- **Estado:** pendiente
- **Verificar:** TC-TT-API-03-13 · TC-TT-API-03-14 (400, 403, 404, 409, 422; y regresión de TC-TT-API-01-04..06)

## T4 — Validación con respuesta 422
- **Estado:** pendiente (`/auth/firebase` se mantiene en 400)
- **Verificar:** TC-TT-API-03-01 · TC-TT-API-03-02

## T5 — Rate limiting y ForwardedHeaders
- **Estado:** pendiente (solo por IP; `/health` exento)
- **Verificar:** TC-TT-API-03-03 · TC-TT-API-03-04 · TC-TT-API-03-06 · TC-TT-API-03-07 (TC-TT-API-03-05 diferido al gateway)

## T6 — Gates de compilación y formato
- **Estado:** pendiente
- **Verificar:** TC-TT-API-03-18
