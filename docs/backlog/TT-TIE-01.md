---
id: "TT-TIE-01"
tipo: "tarea técnica"
jira: "KNOCK-52"
titulo: "Cuentas de Google Play y Apple Developer; primer build firmado en la pista interna"
epica: "EP-13"
sprint: "Sprint 2 (26/10 – 30/10/2026)"
responsable: "Erick Trejo Reséndiz"
puntos: "2"
fuente: "TT-TIE-01_CuentasGooglePlayApple_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-TIE-01_CuentasGooglePlayApple_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-52); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-TIE-01 — Cuentas de Google Play y Apple Developer; primer build firmado en la pista interna

## 1. Datos generales

- **ID de la tarea:** TT-TIE-01
- **Puntos:** 2
- **Título:** Cuentas de Google Play y Apple Developer; primer build firmado en la pista interna
- **Área:** TIE · Tiendas
- **Épica:** EP-13
- **Responsable:** Erick Trejo Reséndiz
- **Sprint:** Sprint 2 (26/10 – 30/10/2026)
- **Día sugerido:** Lunes 26 – viernes 30
- **Clave de Jira:** KNOCK-52
- **Rama:** chore/KNOCK-52-cuentas-tiendas  →  PR a sprint-02
- **Depende de:** TT-MOB-01
- **Terminada cuando:** El equipo instala la app desde la pista interna.

## 2. Objetivo

Tener las cuentas de tienda listas temprano, porque la prueba cerrada de Google Play exige tiempo y testers.

## 3. Alcance

- Google Play Console y Apple Developer.
- Primer build firmado en la pista interna.
- APNs para notificaciones en iOS.

## 4. Fuera de alcance

- Fichas de tienda (TT-TIE-03).

## 5. Pasos

1. Dar de alta las cuentas (anotar titular y costo).
1. Generar la llave de firma de Android y guardarla fuera del repositorio.
1. Subir el primer AAB a la pista interna.
1. Configurar la llave de APNs en Firebase.
1. Subir un build a TestFlight si hay Mac disponible.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| Play Console | Pista interna |
| App Store Connect / TestFlight | iOS |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | El equipo instala la app desde la pista interna | Prueba manual |
| V2 | Un aviso push llega a un iPhone | Prueba manual |

## 8. Seguridad y reglas

- La llave de firma nunca va al repositorio ni a la IA.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Capturas de las consolas.

## 11. Referencias

- DECISIONES.md punto 6 (requisitos de tiendas).
