# Tareas 016 — Renovación y cierre de sesión

> **Estado:** hechas (KNOCK-211, #19; conciliado en KNOCK-45). Los diez criterios tienen prueba (`python scripts/sdd_lint.py --trace 016`).

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Agregar la rotación atómica y la revocación masiva al repositorio de sesiones | R-03, R-04, R-05 · CA-02, CA-03 | hecha (KNOCK-211, #19): `SesionesStore` rota con una sola operación condicionada a la huella vigente y revoca las sesiones de la cuenta. Pruebas: `RefreshAndLogoutTests` (CA-02, CA-03) |
| T-02 | Implementar el servicio de renovación con detección de reuso | R-02, R-03, R-05, R-08 · CA-01, CA-04 | hecha (KNOCK-211, #19): `SessionRefreshService` con detección de reuso. Pruebas: `SessionRefreshServiceTests` y `RefreshAndLogoutTests` (CA-01, CA-04) |
| T-03 | Implementar el servicio de logout y la cancelación en Redis | R-06 · CA-05, CA-08 | hecha (KNOCK-211, #19, y KNOCK-203): `LogoutService` revoca la sesión y escribe `sesion-cancelada:{sid}`; solo cancela si la sesión es de esa cuenta. Pruebas: CA-05 y CA-08 en `RefreshAndLogoutTests` |
| T-04 | Validar en el gateway que el `sid` no esté cancelado | R-07 · CA-06 | hecha (spec 009): el verificador de sesiones del gateway rechaza el `sid` cancelado. Prueba: `SessionClosedByLogout_IsSeenAsRevokedByTheGatewayChecker_CA_016_06` |
| T-05 | Crear los endpoints `refresh` y `logout` y sus validaciones | R-01, R-09, R-10 · CA-07, CA-09, CA-10 | hecha (KNOCK-211, #19): `RefreshController`, `LogoutController` y `RefreshRequestValidator`. Pruebas: CA-07, CA-09 y CA-10 en `RefreshAndLogoutTests` |
| T-06 | Escribir las pruebas unitarias, de integración y de seguridad negativa | R-01 a R-10 · CA-01 a CA-10 | hecha: unitarias (`SessionRefreshServiceTests`, `RefreshTokenFingerprintTests`, `RefreshRequestValidatorTests`), integración (`RefreshAndLogoutTests`) y seguridad negativa (`SessionSecurityTests`, `SEC_016_01` a `SEC_016_08`) |
| T-07 | Publicar el OpenAPI actualizado y registrar la decisión en `Memoria.md` | R-09 | hecha: `docs/openapi.identity.v1.json` y `docs/endpoints.md` con `refresh` y `logout`; decisión en `Memoria.md` (2026-11-04) |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
