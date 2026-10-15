---
id: "TT-API-02"
tipo: "tarea técnica"
jira: "KNOCK-21"
titulo: "Spike: validar el token de Firebase de un número de prueba y emitir la sesión propia (JWT)"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "José Guillermo Rangel Hernández"
puntos: "2"
fuente: "TT-API-02_SpikeValidarTokenFirebase_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-API-02_SpikeValidarTokenFirebase_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-21); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-API-02 — Spike: validar el token de Firebase de un número de prueba y emitir la sesión propia (JWT)

## 1. Datos generales

- **ID de la tarea:** TT-API-02
- **Puntos:** 2
- **Título:** Spike: validar el token de Firebase de un número de prueba y emitir la sesión propia (JWT)
- **Área:** API · Backend base
- **Épica:** EP-13
- **Responsable:** José Guillermo Rangel Hernández
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Miércoles 14 – jueves 15
- **Clave de Jira:** KNOCK-21
- **Rama:** chore/KNOCK-21-spike-firebase-jwt  →  PR a sprint-00
- **Depende de:** TT-API-01, TT-FB-01
- **Terminada cuando:** Prueba de integración que canjea un token de prueba.

## 2. Objetivo

Comprobar desde el inicio la parte más riesgosa del acceso: que el servidor confíe en el código SMS validado por Firebase y entregue su propia sesión.

## 3. Alcance

- Endpoint POST /api/v1/auth/firebase que recibe el ID token de Firebase y devuelve access token (15 min) y refresh token.
- Prueba de integración.

## 4. Fuera de alcance

- Rotación y revocación del refresh token, cierre de sesión y límites (son HU-001 y HU-003 del Sprint 1).
- Pantallas de la app (las hace Erick en TT-PRU-04).

## 5. Pasos

1. Agregar FirebaseAdmin al módulo Identity e inicializarlo con la cuenta de servicio del proyecto de Firebase (ruta en user-secrets, nunca en el repositorio).
1. Endpoint POST /api/v1/auth/firebase: valida el ID token con VerifyIdTokenAsync, lee el número de teléfono, crea el usuario si no existe y responde { accessToken, refreshToken, esNuevo }.
1. Emitir el JWT propio (HS256, 15 min) con sub = id interno y la clave en user-secrets.
1. Proteger un endpoint GET /api/v1/me con [Authorize] que devuelva el id y si es nuevo.
1. Prueba de integración: con un ID token de un número de prueba de Firebase (obtenido con la REST API de Identity Toolkit para números de prueba) canjear y llamar /me.
1. Documentar en docs/specs/spike-firebase.md lo aprendido: qué funcionó, qué falta para HU-001 y riesgos.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| FirebaseAdmin (NuGet) | Validar el ID token de Firebase |
| Microsoft.AspNetCore.Authentication.JwtBearer | Validar la sesión propia |
| dotnet user-secrets | Clave del JWT y ruta de la cuenta de servicio |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Un ID token válido de número de prueba devuelve access y refresh token | Prueba de integración |
| V2 | Un token alterado o vencido responde 401 | Prueba negativa |
| V3 | /me sin token responde 401 y con token responde 200 | Prueba de integración |

## 8. Seguridad y reglas

- Solo números de prueba de Firebase; ningún SMS real.
- La cuenta de servicio de Firebase nunca se sube al repositorio ni se pega en la IA.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Salida de las pruebas y docs/specs/spike-firebase.md.

## 11. Referencias

- DECISION_TECNICA.md §8.
- DECISIONES.md #2 y #21.
