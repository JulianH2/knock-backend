# TT-BD-01 — tareas

## T1 — Compose y PostGIS local (independiente de TT-API-01)
- **Verificar:** TC-TT-BD-01-01
- **Pendiente:** Crear `deploy/docker-compose.local.yml` con imagen PostGIS fija, variable de contraseña obligatoria, puerto ligado a loopback, healthcheck y volumen; documentar inicio, verificación de versión y reinicio seguro.

## T2 — EF Core y migración inicial (requiere TT-API-01 / KNOCK-20)
- **Verificar:** TC-TT-BD-01-02
- **Pendiente:** Integrar Npgsql y NetTopologySuite en contextos existentes; habilitar extensión PostGIS, `HasDefaultSchema(...)`, nombres `snake_case` tras evaluar el paquete requerido y migrar extensión/esquemas con el comando documentado.

## T3 — Reutilizar readiness y Testcontainers (requiere TT-API-01 / KNOCK-20)
- **Verificar:** TC-TT-BD-01-03 · TC-TT-BD-01-04
- **Pendiente:** Usar `/health/ready` y `tests/TestSupport` de TT-API-01, sin duplicar checks ni añadir paquetes `AspNetCore.HealthChecks.*`; demostrar PostgreSQL arriba/abajo y una integración con la imagen PostGIS.

## T4 — Secretos y destino local
- **Verificar:** TC-TT-BD-01-05
- **Pendiente:** Revisar `.env` ignorado, `.env.example` sin secretos, `appsettings*.json` sin contraseñas; ejecutar Gitleaks y verificar que la configuración/migración local rechaza hosts no locales.
