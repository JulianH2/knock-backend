---
id: "TT-MOB-01"
tipo: "tarea técnica"
jira: "KNOCK-24"
titulo: "Proyecto Flutter con Clean Architecture (core/features), Riverpod, go_router y las 5 pestañas de la maqueta"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "Erick Trejo Reséndiz"
puntos: "3"
fuente: "TT-MOB-01_ProyectoFlutterCleanArchitecture_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-MOB-01_ProyectoFlutterCleanArchitecture_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-24); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-MOB-01 — Proyecto Flutter con Clean Architecture (core/features), Riverpod, go_router y las 5 pestañas de la maqueta

## 1. Datos generales

- **ID de la tarea:** TT-MOB-01
- **Puntos:** 3
- **Título:** Proyecto Flutter con Clean Architecture (core/features), Riverpod, go_router y las 5 pestañas de la maqueta
- **Área:** MOB · App móvil base
- **Épica:** EP-13
- **Responsable:** Erick Trejo Reséndiz
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Martes 13 – miércoles 14
- **Clave de Jira:** KNOCK-24
- **Rama:** chore/KNOCK-24-proyecto-flutter-base  →  PR a sprint-00
- **Depende de:** TT-REPO-01, TT-STD-01
- **Terminada cuando:** La app navega entre las 5 pestañas en Android.

## 2. Objetivo

Tener la base de la app con la misma arquitectura del repo móvil del profesor y la navegación de la maqueta, para que cada HU solo agregue su carpeta.

## 3. Alcance

- Proyecto Flutter en app/ (Android e iOS).
- Clean Architecture por función: core/ y features/<función>/{presentation, domain, data}.
- Riverpod y go_router.
- Las 5 pestañas de la maqueta con pantallas vacías.

## 4. Fuera de alcance

- Tema e idiomas (TT-MOB-02).
- Llamadas a la API (TT-MOB-03, Sprint 1).

## 5. Pasos

1. Crear el proyecto: flutter create --org mx.knock --platforms android,ios app.
1. Estructura: lib/core/ (router, tema, errores, red) y lib/features/ con inicio, explorar, actividad, trabajo y cuenta.
1. Agregar flutter_riverpod (estado e inyección) y go_router.
1. Con StatefulShellRoute crear la barra inferior: Inicio · Explorar · Actividad · Trabajo · Mi cuenta. Trabajo solo se muestra si el usuario es técnico (por ahora con un interruptor de prueba).
1. Cada pestaña con una pantalla vacía y un título, con el estilo de la maqueta como referencia.
1. Prueba de widget: tocar cada pestaña muestra su pantalla.
1. Documentar en app/README.md cómo correr la app.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| flutter_riverpod · go_router | Estado, inyección y navegación |
| lib/features/<función>/ | Una carpeta por función con sus 3 capas |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | La app abre en un emulador Android y navega entre las 5 pestañas | Prueba manual |
| V2 | La pestaña Trabajo se oculta si el usuario no es técnico | Prueba de widget |
| V3 | flutter analyze sin hallazgos | Comando local |

## 8. Seguridad y reglas

- Ninguna clave en el código; los archivos de Firebase se agregan localmente.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Video corto navegando las pestañas.

## 11. Referencias

- DECISION_TECNICA.md §9.
- Maqueta (README, sección "Un técnico sigue siendo usuario").
