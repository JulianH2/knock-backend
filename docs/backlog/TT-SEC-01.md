---
id: "TT-SEC-01"
tipo: "tarea técnica"
jira: "KNOCK-36"
titulo: "Secretos en Key Vault con identidad administrada; gitleaks en CI"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "2"
fuente: "TT-SEC-01_SecretosKeyVaultIdentidad_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-SEC-01_SecretosKeyVaultIdentidad_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-36); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-SEC-01 — Secretos en Key Vault con identidad administrada; gitleaks en CI

## 1. Datos generales

- **ID de la tarea:** TT-SEC-01
- **Puntos:** 2
- **Título:** Secretos en Key Vault con identidad administrada; gitleaks en CI
- **Área:** SEC · Seguridad (DevSecOps)
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Lunes 19
- **Clave de Jira:** KNOCK-36
- **Rama:** chore/KNOCK-36-secretos-keyvault  →  PR a sprint-01
- **Depende de:** TT-AZ-02
- **Terminada cuando:** No hay secretos en el repositorio; CI lo verifica.

## 2. Objetivo

Que ningún secreto viva en el código ni en archivos del repositorio.

## 3. Alcance

- Key Vault como fuente de secretos de la API en Azure.
- user-secrets en local.
- gitleaks en el CI.

## 4. Fuera de alcance

- CodeQL y escaneo de dependencias (TT-SEC-02).

## 5. Pasos

1. Agregar Azure.Extensions.AspNetCore.Configuration.Secrets y cargar Key Vault con la identidad administrada cuando el entorno no es Development.
1. Guardar en Key Vault: cadena de PostgreSQL, Redis, clave del JWT, cuenta de servicio de Firebase y clave del hash de la CURP.
1. Documentar en CONTRIBUTING.md el uso de dotnet user-secrets en local.
1. Agregar el job gitleaks al workflow de PR y un .gitleaks.toml con las reglas.
1. Revisar el historial del repositorio con gitleaks y rotar cualquier secreto encontrado.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Key Vault | Secretos en Azure |
| dotnet user-secrets | Secretos en local |
| gitleaks | Detectar secretos en PR |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | La API en Staging arranca leyendo Key Vault | Logs de arranque sin secretos |
| V2 | Un PR con una clave falsa falla en gitleaks | PR de prueba |

## 8. Seguridad y reglas

- Si un secreto se filtra, se rota de inmediato.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura del job gitleaks fallando con la clave de prueba.

## 11. Referencias

- SEGURIDAD_Y_PRIVACIDAD.md §5.
