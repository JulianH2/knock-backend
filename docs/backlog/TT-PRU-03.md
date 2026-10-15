---
id: "TT-PRU-03"
tipo: "tarea técnica"
jira: "KNOCK-31"
titulo: "Ejercicio fuera de rol (Ángel): proyecto Angular del panel con una pantalla de inicio"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "Ángel Alexis Blas Castillo"
puntos: "1"
fuente: "TT-PRU-03_EjercicioFueraRolProyecto_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-PRU-03_EjercicioFueraRolProyecto_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-31); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-PRU-03 — Ejercicio fuera de rol (Ángel): proyecto Angular del panel con una pantalla de inicio

## 1. Datos generales

- **ID de la tarea:** TT-PRU-03
- **Puntos:** 1
- **Título:** Ejercicio fuera de rol (Ángel): proyecto Angular del panel con una pantalla de inicio
- **Área:** PRU · Prueba de roles
- **Épica:** EP-13
- **Responsable:** Ángel Alexis Blas Castillo
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Jueves 15
- **Clave de Jira:** KNOCK-31
- **Rama:** chore/KNOCK-31-panel-angular  →  PR a sprint-00
- **Depende de:** TT-STD-01
- **Terminada cuando:** Panel compila y abre en el navegador.

## 2. Objetivo

Ejercicio fuera de rol para Ángel: arrancar el panel interno en Angular y ver si puede apoyar en pantallas.

## 3. Alcance

- Proyecto Angular en panel/ con una pantalla de inicio "Panel Knock".

## 4. Fuera de alcance

- Inicio de sesión y doble factor (HU-066, Sprint 3).

## 5. Pasos

1. Crear el proyecto con Angular CLI: ng new panel (componentes standalone, enrutamiento, SCSS).
1. Estructura core/ y features/; agregar Angular Material y el cliente de la API generado desde OpenAPI (typescript-angular).
1. Pantalla de inicio con un menú lateral vacío: Verificaciones, Reportes, Métricas.
1. Prueba unitaria del componente del menú.
1. Abrir el PR y pedir revisión a Erick; anotar cuánto tardó y qué fue difícil.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| chore/KNOCK-31-panel-angular | Rama del ejercicio |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | ng serve abre el panel en el navegador | Prueba manual |
| V2 | La prueba del menú pasa | ng test |

## 8. Seguridad y reglas

- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura del panel y PR con la nota.

## 11. Referencias

- DECISIONES.md #19.
