---
id: "TT-MOB-04"
tipo: "tarea técnica"
jira: "KNOCK-39"
titulo: "Spike: Google Maps en Flutter con pines y agrupación"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "Erick Trejo Reséndiz"
puntos: "2"
fuente: "TT-MOB-04_SpikeGoogleMapsFlutter_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-MOB-04_SpikeGoogleMapsFlutter_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-39); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-MOB-04 — Spike: Google Maps en Flutter con pines y agrupación

## 1. Datos generales

- **ID de la tarea:** TT-MOB-04
- **Puntos:** 2
- **Título:** Spike: Google Maps en Flutter con pines y agrupación
- **Área:** MOB · App móvil base
- **Épica:** EP-13
- **Responsable:** Erick Trejo Reséndiz
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Miércoles 21 – jueves 22
- **Clave de Jira:** KNOCK-39
- **Rama:** chore/KNOCK-39-spike-google-maps  →  PR a sprint-01
- **Depende de:** TT-MOB-01
- **Terminada cuando:** Mapa con 50 pines de prueba fluido en Android.

## 2. Objetivo

Comprobar antes de HU-011 que Google Maps en Flutter aguanta los pines de expertos con fluidez y medir su costo.

## 3. Alcance

- Mapa con 50 pines de prueba, agrupación y botón «Mi ubicación».
- Clave de API restringida por app.

## 4. Fuera de alcance

- Panel de expertos y filtros (HU-011).

## 5. Pasos

1. Crear en Google Cloud la clave de Maps SDK for Android e iOS restringida al paquete mx.knock.app y a sus huellas.
1. Agregar google_maps_flutter y un paquete de agrupación de marcadores.
1. Pantalla de prueba con 50 pines alrededor de la ciudad piloto, agrupación al alejar y botón para centrar.
1. Medir fluidez en un Android de gama media.
1. Revisar el precio vigente de Maps SDK para móviles y anotarlo (pendiente #2 de DECISIONES).
1. Documentar resultados en docs/specs/spike-mapa.md.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Google Cloud Console → credenciales | Clave restringida |
| google_maps_flutter | Mapa |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | El mapa se mueve fluido con 50 pines | Video en un Android real |
| V2 | La clave no funciona desde otra app | Restricción comprobada |

## 8. Seguridad y reglas

- Clave restringida por aplicación; nunca sin restricciones.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Video y docs/specs/spike-mapa.md.

## 11. Referencias

- DECISIONES.md #22.
