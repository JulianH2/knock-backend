---
id: "TT-PRU-02"
tipo: "tarea técnica"
jira: "KNOCK-30"
titulo: "Ejercicio fuera de rol (Guillermo): pantalla Flutter que lista los oficios"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Guillermo Rangel Hernández"
puntos: "1"
fuente: "TT-PRU-02_EjercicioFueraRolPantalla_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-PRU-02_EjercicioFueraRolPantalla_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-30); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-PRU-02 — Ejercicio fuera de rol (Guillermo): pantalla Flutter que lista los oficios

## 1. Datos generales

- **ID de la tarea:** TT-PRU-02
- **Puntos:** 1
- **Título:** Ejercicio fuera de rol (Guillermo): pantalla Flutter que lista los oficios
- **Área:** PRU · Prueba de roles
- **Épica:** EP-13
- **Responsable:** José Guillermo Rangel Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Jueves 15
- **Clave de Jira:** KNOCK-30
- **Rama:** feature/KNOCK-30-pantalla-oficios  →  PR a sprint-00
- **Depende de:** TT-MOB-01, TT-PRU-01
- **Terminada cuando:** Pantalla consume el endpoint real.

## 2. Objetivo

Ejercicio fuera de rol para Guillermo: hacer una pantalla pequeña en Flutter y ver qué tan cómodo se siente en el frontend.

## 3. Alcance

- Pantalla que lista los oficios desde GET /api/v1/oficios.
- Estados de carga, error y vacío.

## 4. Fuera de alcance

- Diseño final.

## 5. Pasos

1. Crear features/oficios con sus tres capas (data: llamada HTTP con dio; domain: entidad Oficio; presentation: pantalla y proveedor de Riverpod).
1. Mostrar la lista con su ícono por modalidad (taller o domicilio).
1. Manejar carga, error con botón Reintentar y lista vacía.
1. Prueba de widget con un repositorio falso.
1. Abrir el PR y pedir revisión a Erick; anotar cuánto tardó y qué fue difícil.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| feature/KNOCK-30-pantalla-oficios | Rama del ejercicio |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | La pantalla muestra los 3 oficios desde la API local | Prueba manual |
| V2 | La prueba de widget pasa | flutter test |

## 8. Seguridad y reglas

- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Captura de la pantalla y PR con la nota.

## 11. Referencias

- TT-MOB-01.
