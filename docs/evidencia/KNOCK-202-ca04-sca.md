# Evidencia — KNOCK-202 · CA-04 de la spec 007: una dependencia vulnerable hace fallar el CI

> Spec: `specs/007-integracion-continua-y-despliegue/spec.md` (Aprobada), CA-04: «Dado un PR con un secreto de ejemplo o una dependencia con vulnerabilidad crítica, cuando corre el CI, entonces el check falla.» · Fecha: 2026-11-10
> Esta nota guarda la prueba aunque el PR que la produjo siga cerrado sin fusionar.

## Qué se hizo
- **PR de prueba:** #75 (`test/KNOCK-202-ca04-vulnerable-dependency`). Agregó a propósito el paquete `System.Text.Encodings.Web` 4.5.0, que tiene una vulnerabilidad conocida. El título decía «no fusionar».
- **Corrida:** [`ci-pr` 42883550234](https://github.com/Tensei-Devs/knock-backend/actions/runs/42883550234) del 2026-11-10 a las 19:16 UTC, sobre el commit `9de0c3c`.
- **Resultado:** el job **«SCA (dependencias vulnerables, obsoletas y licencias)»** falló, y con él el job `gate`, que es el que bloquea la fusión. «API — build, formato y pruebas» también falló, pero por otra causa: `error NU1004` (el archivo de bloqueo de `Knock.UnitTest.csproj` no coincide con las referencias de paquetes agregadas) y `error NU1510` (advertencia tratada como error: `System.Text.Encodings.Web` no se poda). Lo que demuestra el CA-04 es el job de SCA, no ese.
- **Cierre:** el #75 se cerró sin fusionar (19:17 UTC). No dejó ningún cambio en `sprint-04`.

## Conclusión
El CI rechaza un PR con una dependencia vulnerable, como pide el CA-04. La parte del secreto de ejemplo la cubre el job de Gitleaks, que corre en cada PR.
