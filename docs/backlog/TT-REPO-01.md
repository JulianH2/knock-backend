---
id: "TT-REPO-01"
tipo: "tarea técnica"
jira: "KNOCK-15"
titulo: "Crear el monorepo knock en GitHub (app/, panel/, api/, docs/, deploy/) e invitar al equipo y a los docentes"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "1"
fuente: "TT-REPO-01_CrearMonorepoKnockGithub_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-REPO-01_CrearMonorepoKnockGithub_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-15); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-REPO-01 — Crear el monorepo knock en GitHub (app/, panel/, api/, docs/, deploy/) e invitar al equipo y a los docentes

## 1. Datos generales

- **ID de la tarea:** TT-REPO-01
- **Puntos:** 1
- **Título:** Crear el monorepo knock en GitHub (app/, panel/, api/, docs/, deploy/) e invitar al equipo y a los docentes
- **Área:** REPO · Repositorio
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Lunes 12
- **Clave de Jira:** KNOCK-15
- **Rama:** chore/KNOCK-15-crear-monorepo  →  PR a sprint-00
- **Depende de:** —
- **Terminada cuando:** Repositorio creado con la estructura y accesos.

## 2. Objetivo

Tener un solo repositorio para todo Knock, con una carpeta por parte del sistema, para que el equipo y los agentes de IA trabajen con todo el contexto a la vista.

## 3. Alcance

- Repositorio privado "knock" en GitHub.
- Estructura de carpetas base con un README por carpeta.
- Accesos para el equipo y los docentes.

## 4. Fuera de alcance

- Código de la API o de la app (lo hacen TT-API-01 y TT-MOB-01).
- Flujos de CI (TT-CI-01).

## 5. Pasos

1. Crear el repositorio privado "knock" en la cuenta personal de Julián (con el GitHub Student Developer Pack, para poder proteger ramas en un repositorio privado; confirmarlo en la configuración).
1. Crear la estructura: api/ (backend .NET), app/ (Flutter móvil), panel/ (Angular), docs/ (documentos y specs), deploy/ (Bicep y scripts), .github/ (workflows y plantillas).
1. Agregar un README.md raíz con qué es Knock, cómo está organizado el repositorio y enlaces a docs/.
1. Agregar .gitignore combinado para .NET, Flutter, IDEs (Visual Studio, Rider, VS Code, Android Studio) y archivos de entorno (.env, appsettings.*.local.json, google-services.json, GoogleService-Info.plist).
1. Copiar a docs/ los documentos vigentes: FUNCIONES_Y_FLUJOS.md, DECISIONES.md, SEGURIDAD_Y_PRIVACIDAD.md y DECISION_TECNICA.md.
1. Invitar a Erick, Guillermo y Ángel con permiso de escritura, y a los docentes con permiso de lectura.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| README.md | Presentación y mapa del repositorio |
| .gitignore | Evitar subir binarios, secretos y archivos de Firebase |
| docs/ | Documentos del proyecto (fuente de las HU y specs) |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Las seis carpetas existen con su README | Vista del repositorio en GitHub |
| V2 | Los tres compañeros y los docentes tienen acceso | Settings → Collaborators |
| V3 | google-services.json y .env están ignorados | git check-ignore -v app/android/app/google-services.json |

## 8. Seguridad y reglas

- El repositorio es privado.
- Ningún archivo de credenciales de Firebase o Azure se sube; se reparten por un canal privado.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Enlace al repositorio y captura de colaboradores.

## 11. Referencias

- DECISION_TECNICA.md §2 (monorepo modular).
