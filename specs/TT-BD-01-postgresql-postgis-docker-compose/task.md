# TT-BD-01 — tareas

## T1 — Compose y PostGIS local (independiente de TT-API-01)
- **Verificar:** TC-TT-BD-01-01
- **Estado:** Hecho. `deploy/docker-compose.local.yml` usa `postgis/postgis:16-3.4`, exige `POSTGRES_PASSWORD`, publica solo en loopback y define healthcheck y volumen persistente. Arranque y `SELECT postgis_version()` verificados localmente; instrucciones de reinicio documentadas en `README.md`.

## T2 — EF Core y migración inicial (requiere TT-API-01 / KNOCK-20)
- **Verificar:** TC-TT-BD-01-02
- **Pendiente:** Integrar Npgsql y NetTopologySuite en contextos existentes; habilitar extensión PostGIS, `HasDefaultSchema(...)`, nombres `snake_case` tras evaluar el paquete requerido y migrar extensión/esquemas con el comando documentado.

## T3 — Reutilizar readiness y Testcontainers (requiere TT-API-01 / KNOCK-20)
- **Verificar:** TC-TT-BD-01-03 · TC-TT-BD-01-04
- **Pendiente:** Usar `/health/ready` y `tests/TestSupport` de TT-API-01, sin duplicar checks ni añadir paquetes `AspNetCore.HealthChecks.*`; demostrar PostgreSQL arriba/abajo y una integración con la imagen PostGIS.

## T4 — Secretos y destino local
- **Verificar:** TC-TT-BD-01-05
- **Estado:** Parcial. `.env` está ignorado y `.env.example` contiene solo nombres y una contraseña vacía; quedan pendientes Gitleaks, la revisión de `appsettings*.json` y el rechazo de hosts remotos en la configuración de migración de TT-API-01.
