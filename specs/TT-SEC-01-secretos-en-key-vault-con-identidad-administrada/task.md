# TT-SEC-01 — tareas

> Borrador. Ninguna tarea empieza hasta que la spec esté Aprobada y el líder responda las dudas de spec §8 (sobre todo la 1). Lo marcado «persona» lo ejecuta quien tenga acceso a Azure.

## T1 — Inventario y documento de secretos
- **Estado:** pendiente de aprobación de la spec. `docs/SECRETOS.md` (inventario, local con user-secrets, rotación, qué hacer si se filtra) y enlaces desde el README y `CONTRIBUTING.md`.
- **Verificar:** TC-TT-SEC-01-03

## T2 — Pruebas de secretos en el repositorio y de arranque cerrado
- **Estado:** pendiente. Escribir primero y ver fallar lo que corresponda contra el estado actual.
- **Verificar:** TC-TT-SEC-01-01 · TC-TT-SEC-01-02 · TC-TT-SEC-01-04

## T3 — gitleaks: muestra e historial
- **Estado:** pendiente. Corrida local de la imagen oficial sobre una muestra (reglas propias y marcadores) y sobre todo el historial; resolver hallazgos (rotar o registrar con huella).
- **Verificar:** TC-TT-SEC-01-05 · TC-TT-SEC-01-06

## T4 — Despliegue y rotación (persona)
- **Estado:** pendiente de la implementación de TT-AZ-02. Cargar los secretos en Key Vault, comprobar las referencias de cada aplicación y ensayar la rotación, con evidencia sin valores.
- **Verificar:** TC-TT-SEC-01-07 · TC-TT-SEC-01-08

## T5 — Gates y cierre
- **Estado:** pendiente. Build, formato y gobierno en verde; `Memoria.md`; informe de `story-workflow` en el PR.
- **Verificar:** TC-TT-SEC-01-09

## Fuera de esta tarea
- Módulos de Terraform, estado remoto, cuenta de servicio de Firebase, hash de la CURP, `Azure.Identity` para Blob, CodeQL y escaneo de dependencias (TT-AZ-02, TT-AZ-05, tareas futuras y TT-SEC-02).
