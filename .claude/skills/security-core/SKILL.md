---
name: security-core
description: Matriz de seguridad y privacidad de Knock basada en OWASP Top 10 y OWASP API Security Top 10 (autenticación, autorización por recurso, entradas, datos sensibles y biométricos, archivos, sesiones, límites, secretos, cadena de suministro). Úsala al construir endpoints o pantallas y al revisar antes del PR.
---
<!-- GENERADO por scripts/sync_agents.py desde .agents/skills/security-core/SKILL.md. No editar a mano. -->
# security-core

## Checks (PASS / FAIL / N/A con motivo)
| # | Control | OWASP |
|---|---|---|
| 1 | Autorización por recurso en el servidor: el usuario solo ve o cambia lo suyo; el técnico solo ve la **colonia** hasta ser elegido; panel por permisos (`verification:review`, `reports:moderate`…) con doble factor | API1, API5, A01 |
| 2 | Autenticación: token de Firebase validado en servidor; JWT propio HS256 15 min, `ClockSkew=0`, algoritmos fijos, blacklist por `jti`; refresh rotativo con detección de reuso | API2, A07 |
| 3 | Validación en frontera (FluentValidation / validadores de formulario) aunque el cliente valide; tamaños máximos; tipos de archivo | API3, API8, A03 |
| 4 | Inyección: EF Core parametrizado; nada de SQL interpolado; sin `innerHTML` ni `bypassSecurityTrust` en Angular; sin rutas de archivo desde el usuario (traversal) | A03, API8 |
| 5 | SSRF: el backend no descarga URLs que manda el usuario; integraciones con lista permitida | API7, A10 |
| 6 | Límites: rate limits de `docs/rate-limit-matrix.md`, 429 uniforme, paginación con máximo, tamaño de cuerpo | API4 |
| 7 | Datos sensibles: dirección y celular cifrados a nivel de campo; CURP solo como HMAC; nunca en logs, errores, fixtures ni prompts de IA | API3, A02 |
| 8 | **Biométricos (INE y selfie):** contenedor privado y separado, cifrados con llave propia por verificación, enlaces de 5 min, registro de cada acceso; se borran al decidir o a los 7 días destruyendo la llave | A02, A04 |
| 9 | Archivos: Blob privado, SAS ≤ 5 min, EXIF eliminado, tipo y tamaño validados | API8 |
| 10 | Sesión en apps: tokens en almacenamiento seguro (Keychain/Keystore), nunca en SharedPreferences/localStorage; cierre de sesión elimina token de push | A07 |
| 11 | Errores: ProblemDetails sin stack trace fuera de Dev; mensajes del catálogo sin detalles técnicos | A05 |
| 12 | Headers y CORS: CSP, HSTS, nosniff, DENY; CORS solo el origen del panel | A05 |
| 13 | Secretos: Key Vault / variables; gitleaks limpio; nada en el repo, logs ni artefactos | A05, A08 |
| 14 | Dependencias y cadena de suministro: sin vulnerables altas, licencias permitidas, acciones fijadas a SHA, imágenes escaneadas, SBOM en releases | A06, A08 |
| 15 | Auditoría: eventos sensibles (registro, verificación, suspensión, eliminación) con TraceId, sin PII | A09 |
| 16 | IA: filtro de datos personales antes del modelo; la IA nunca cambia estados ni decide | — |

## Política de bloqueo
Crítica o alta explotable en el contexto de ejecución → bloquea el merge. Falso positivo → evidencia + excepción en `security/exceptions.yaml` (máx. 90 días, responsable, plan). Nunca supresiones globales ni silenciosas.

## Salida del revisor
`| # | Check | PASS/FAIL/N/A | archivo:línea | nota |` y veredicto `BLOQUEA` o `LIBERA`. Un escáner sin alertas no prueba que algo sea seguro.
