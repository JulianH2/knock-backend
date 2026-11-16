# Tareas 015 — Registro e inicio de sesión

> **Estado (2026-11-16):** T-01 a T-10 hechas con KNOCK-210 y KNOCK-231 (Identity.Api, fusionados en `sprint-04`); la tarjeta KNOCK-43 (HU-001) solo reconcilia este archivo. Pruebas: `tests/UnitTest/Identity/{FirebaseExchangeRequestValidatorTests,LoginServiceTests,SessionTokenIssuerTests}.cs` y `tests/IntegrationTest/Identity/{FirebaseExchangeTests,SesionesIndexesAndLimitTests}.cs` (CA-01 a CA-12; `sdd_lint.py --trace 015` sin casos sin prueba). Límites conocidos: «otra firma» de CA-04 solo se prueba con el emulador de Firebase, no con la firma real de Google; las transacciones de MongoDB deben comprobarse contra el clúster real.

| Tarea | Qué se hace | Cubre | Estado |
|---|---|---|---|
| T-01 | Escribir el script de `db/` con `usuarios` y `sesiones` y sus índices (únicos en `sub` y `celularHash`, TTL en `venceEn`) | R-05, R-08 · CA-08 | hecha |
| T-02 | Crear el validador del cuerpo (`firebaseIdToken`, `deviceId`, `aceptaAviso`, `dispositivo`) | R-03 · CA-03 | hecha |
| T-03 | Implementar el verificador de Firebase detrás de su interfaz | R-02, R-13 · CA-04 | hecha |
| T-04 | Derivar el `sub`, cifrar el celular, calcular su hash y emitir el JWT y el token de renovación con su huella | R-04, R-08, R-09 · CA-10 | hecha |
| T-05 | Implementar el servicio de inicio de sesión: cuenta nueva o existente, conflicto de cuenta y estados de la cuenta | R-04, R-05, R-06, R-07 · CA-01, CA-02, CA-05, CA-06, CA-08, CA-09 | hecha |
| T-06 | Aplicar el máximo de 10 sesiones por cuenta | R-10 · CA-07 | hecha |
| T-07 | Crear el controller y la respuesta sin caché ni compresión | R-11, R-12 · CA-10, CA-12 | hecha |
| T-08 | Marcar la ruta como pública en el gateway con el límite de autenticación | R-01 · CA-11 | hecha |
| T-09 | Escribir las pruebas unitarias, de integración y de seguridad negativa | R-01 a R-13 · CA-01 a CA-12 | hecha |
| T-10 | Registrar en `Memoria.md` la dependencia de Firebase y publicar el OpenAPI | R-13 | hecha |

## Definición de terminado
- Los criterios de aceptación de `spec.md` están cubiertos por pruebas en verde.
- El CI está en verde, un par revisó el cambio y `Memoria.md` está al día.
