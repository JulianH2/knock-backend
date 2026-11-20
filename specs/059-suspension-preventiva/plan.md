# Plan 059 — Suspensión preventiva en casos críticos

> Parte de `spec.md` (estado Borrador). Este plan no se activa hasta que una persona apruebe la spec y resuelva las dudas de su §15 que bloquean el código (1 a 6).

## 1. Enfoque
1. Contrato: documentar `CuentaSuspendida` y `CuentaReactivada` en `docs/events.md` y la nota de efecto de REP-05 en `docs/endpoints.md`; pedir las enmiendas a las specs 037 (tipo de aviso) y 054 (catálogo de `tipo` de `outbox`).
2. [BD] Script de `db/` que documenta el subdocumento `usuarios.suspension` (sin índices) y prueba de base de datos.
3. [Back] Dominio de la cuenta (`Suspender`, `Reactivar`, regla gradual) con pruebas unitarias antes que el servicio.
4. [Back] Servicio `AccountSuspension` en el módulo `Safety`: transacción de R-04 (estado, sesiones de la app y del panel, `outbox`, auditoría) y cancelación posterior en Redis con reintento.
5. [Back] Consumidor en Mercado: proyección del estado del técnico y avisos a clientes con cita próxima, idempotente por `eventoId`.
6. Conexión con REP-05 de la spec 053 (cuando exista) y con la regla gradual al registrar advertencias.
7. Pruebas por criterio (CA-01 a CA-19), seguridad negativa, `docs/openapi.v1.json` regenerado solo si cambia el contrato, `Memoria.md` e informe.

## 2. Archivos que se crean o cambian
- `db/059-suspension-cuenta.js` (documenta `usuarios.suspension`)
- `src/Knock.Identity.Api/Modules/Safety/` (`Services/AccountSuspension`, dominio, configuración `Suspension:*`)
- `src/Knock.Identity.Api/Modules/Sesiones/` (revocación en bloque por cuenta, interfaz de módulo)
- `src/Knock.Market.Api/` (consumidor de `CuentaSuspendida` y `CuentaReactivada`, proyección y avisos)
- `tests/UnitTest`, `tests/IntegrationTest`, `tests/SecurityTest`, `tests/DatabaseTest` (clases `Spec059*`)
- `docs/events.md`, `docs/endpoints.md`, `Memoria.md`

## 3. No tocar
- La forma pública de las rutas de las specs 015, 016, 043 y 053; `.github/`, `scripts/`, constitución y configuración de analizadores.
- Scripts de índices o semillas ya fusionados; `docs/openapi.v1.json` a mano.
- Colecciones o servicios de otro módulo (usar `I<Modulo>Api` y eventos).

## 4. Dependencias
- Spec 053 (REP-05) y 033 (permisos, doble factor, `panel_auditoria`): sin ellas no hay disparo ni autorización reales; hasta entonces `Suspender` solo se prueba como operación interna.
- Spec 043 (reportes y apelación) y spec 054 (bandeja de salida y su catálogo de eventos).
- Spec 037 (tipo de aviso nuevo) y specs 020, 027 y 050 (elegibilidad y visitas).

## 5. Verificación
- `python scripts/sdd_lint.py --trace 059` sin faltantes; comandos de `AGENTS.md §2` y gates de `§5`; `Memoria.md` al día.
