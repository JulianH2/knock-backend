---
id: "TT-BD-03"
tipo: "tarea técnica"
jira: "KNOCK-40"
titulo: "Semillas ficticias: clientes, técnicos y solicitudes de prueba"
epica: "EP-13"
sprint: "Sprint 1 (19/10 – 23/10/2026)"
responsable: "Ángel Alexis Blas Castillo"
puntos: "1"
fuente: "TT-BD-03_SemillasFicticiasClientesTecnicos_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-BD-03_SemillasFicticiasClientesTecnicos_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-40); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-BD-03 — Semillas ficticias: clientes, técnicos y solicitudes de prueba

## 1. Datos generales

- **ID de la tarea:** TT-BD-03
- **Puntos:** 1
- **Título:** Semillas ficticias: clientes, técnicos y solicitudes de prueba
- **Área:** BD · Base de datos
- **Épica:** EP-13
- **Responsable:** Ángel Alexis Blas Castillo
- **Sprint:** Sprint 1 (19/10 – 23/10/2026)
- **Día sugerido:** Martes 20
- **Clave de Jira:** KNOCK-40
- **Rama:** chore/KNOCK-40-semillas-ficticias  →  PR a sprint-01
- **Depende de:** TT-BD-02
- **Terminada cuando:** Staging tiene datos de demostración.

## 2. Objetivo

Que Staging y las demos tengan datos creíbles sin usar datos de personas reales.

## 3. Alcance

- Clientes, técnicos y solicitudes ficticios.
- Script repetible.

## 4. Fuera de alcance

- Datos de producción.

## 5. Pasos

1. Crear deploy/seeds/ficticios.sql (o un comando de la API) con 20 técnicos y 10 clientes ficticios en colonias piloto.
1. Celulares con los números de prueba de Firebase o con un prefijo inválido evidente.
1. Nombres generados (Bogus) sin parecerse a personas reales conocidas.
1. Que el script se pueda correr varias veces sin duplicar.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Bogus (NuGet) | Datos ficticios |
| deploy/seeds/ | Scripts versionados |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Staging muestra técnicos en el mapa de prueba | Prueba manual |
| V2 | Correr el script dos veces no duplica | Conteo de registros |

## 8. Seguridad y reglas

- Nada de datos reales (SEGURIDAD_Y_PRIVACIDAD.md §8).
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Conteos después de cargar.

## 11. Referencias

- SEGURIDAD_Y_PRIVACIDAD.md §8.
