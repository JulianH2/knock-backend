# TT-API-06 — tareas

> Borrador. Ninguna tarea empieza hasta que la spec esté Aprobada y el líder resuelva las dudas de spec §8.

## T1 — Orden del pipeline y configuración de seguridad
- **Estado:** pendiente de aprobación de la spec (opciones `Security:*` validadas, `UseKnockApi` en el orden de la constitución §4, `docs/MIDDLEWARES.md`)
- **Verificar:** TC-TT-API-06-13 · TC-TT-API-06-14 · TC-TT-API-06-15 · TC-TT-API-06-17

## T2 — Encabezados de seguridad y `Server` apagado
- **Estado:** pendiente (`SecurityHeadersMiddleware` con `OnStarting`, `Cache-Control` si falta, Kestrel sin `Server`)
- **Verificar:** TC-TT-API-06-01 · TC-TT-API-06-02 · TC-TT-API-06-03 · TC-TT-API-06-04 · TC-TT-API-06-18

## T3 — ForwardedHeaders con esquema, HSTS y redirección
- **Estado:** pendiente (`X-Forwarded-Proto` solo con proxies de confianza; HSTS fuera de Development; redirección opt-in)
- **Verificar:** TC-TT-API-06-05 · TC-TT-API-06-06 · TC-TT-API-06-07 · TC-TT-API-06-08

## T4 — CORS con falla cerrada
- **Estado:** pendiente (orígenes por configuración, sin comodín ni credenciales; preflight; errores con CORS)
- **Verificar:** TC-TT-API-06-09 · TC-TT-API-06-10 · TC-TT-API-06-11 · TC-TT-API-06-12

## T5 — Endpoints públicos con límite
- **Estado:** pendiente (prueba que enumera los públicos; la duda 8 está resuelta: `/health` exento y `/health/ready` con política propia de 60 por minuto)
- **Verificar:** TC-TT-API-06-16 · (regresión) TC-TT-API-03-03 · TC-TT-API-03-04 · TC-TT-API-03-06

## T6 — Documentación y gates
- **Estado:** pendiente (`docs/security-headers.md`, nota en `docs/rate-limit-matrix.md`, sin paquetes nuevos, build y formato)
- **Verificar:** TC-TT-API-06-19 · TC-TT-API-06-20
