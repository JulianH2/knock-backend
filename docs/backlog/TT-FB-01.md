---
id: "TT-FB-01"
tipo: "tarea técnica"
jira: "KNOCK-28"
titulo: "Proyecto de Firebase: autenticación por teléfono con números de prueba, FCM y apps Android/iOS registradas"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Julián Hernández Hernández"
puntos: "1"
fuente: "TT-FB-01_ProyectoFirebaseAutenticacionTelefono_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-FB-01_ProyectoFirebaseAutenticacionTelefono_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-28); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-FB-01 — Proyecto de Firebase: autenticación por teléfono con números de prueba, FCM y apps Android/iOS registradas

## 1. Datos generales

- **ID de la tarea:** TT-FB-01
- **Puntos:** 1
- **Título:** Proyecto de Firebase: autenticación por teléfono con números de prueba, FCM y apps Android/iOS registradas
- **Área:** FB · Firebase
- **Épica:** EP-13
- **Responsable:** José Julián Hernández Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Lunes 12
- **Clave de Jira:** KNOCK-28
- **Rama:** chore/KNOCK-28-firebase-proyecto  →  PR a sprint-00
- **Depende de:** —
- **Terminada cuando:** Un número de prueba recibe código sin SMS real.

## 2. Objetivo

Tener Firebase listo para que el inicio de sesión con celular y las notificaciones se puedan probar sin mandar SMS reales.

## 3. Alcance

- Proyecto de Firebase.
- Autenticación por teléfono con números de prueba.
- Apps Android e iOS registradas.
- Cloud Messaging activo.

## 4. Fuera de alcance

- Envío de notificaciones (HU-050, Sprint 2).
- APNs para iOS (TT-TIE-01, Sprint 2).

## 5. Pasos

1. Crear el proyecto "knock" en la consola de Firebase.
1. Activar Authentication → Sign-in method → Teléfono.
1. Agregar 4 números de prueba con su código fijo (uno por integrante) en "Números de teléfono para pruebas"; registrarlos en docs/infra.md.
1. Registrar la app Android con el paquete mx.knock.app y las huellas SHA-1 y SHA-256 de depuración de cada integrante; registrar la app iOS con el bundle mx.knock.app.
1. Descargar google-services.json y GoogleService-Info.plist y repartirlos por un canal privado (no al repositorio).
1. Generar una cuenta de servicio para el backend (TT-API-02) y entregarla a Guillermo por un canal privado.
1. Revisar en la documentación de Firebase los límites y el costo por SMS real (pendiente #1 de DECISIONES) y anotarlo.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Consola de Firebase | Proyecto, autenticación y apps |
| Números de prueba | Entrar sin SMS reales |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Un número de prueba recibe su código fijo desde la app de ejemplo de Firebase o desde la app de Knock | Prueba manual |
| V2 | El backend puede validar un ID token con la cuenta de servicio | Lo comprueba TT-API-02 |

## 8. Seguridad y reglas

- Los archivos de configuración y la cuenta de servicio nunca van al repositorio ni a la IA.
- Solo números de prueba en desarrollo y demo.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura de los números de prueba (sin códigos visibles) y de las apps registradas.

## 11. Referencias

- DECISIONES.md #21.
- SEGURIDAD_Y_PRIVACIDAD.md §8.
