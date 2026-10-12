# TT-FB-01 — Proyecto de Firebase: autenticación por teléfono con números de prueba, FCM y apps Android/iOS registradas

> **Jira:** KNOCK-28 · **Área:** Firebase · **Responsable:** José Julián Hernández Hernández · **Sprint:** S0 · **Puntos:** 1
> **Repositorio:** knock-backend · **Ficha:** TT-FB-01_ProyectoFirebaseAutenticacionTelefono_Knock.docx
> **Skills:** story-workflow, vertical-slice-api, testing-strategy-backend, security-core
> **Estado:** Aprobada

## 1. Objetivo
Crear el proyecto de Firebase con autenticación por teléfono y números de prueba, Firebase Cloud Messaging (FCM) y las apps Android e iOS registradas.

## 2. Historia y casos de prueba
**TT-FB-01** — Tarea técnica (no es historia de usuario). Los casos se verifican con evidencia (comando o captura), no con pruebas de código.

| Caso | Criterio (Dado / Cuando / Entonces) | Verificación |
|---|---|---|
| TC-TT-FB-01-01 | Dado un número de prueba configurado en Firebase Authentication, cuando se solicita el código, entonces se recibe un código fijo sin enviar un SMS real. | Inicio de sesión con el número de prueba en la consola o con el emulador de Firebase Auth |
| TC-TT-FB-01-02 | Dado FCM habilitado, entonces las credenciales del servidor no están en el repositorio. | Revisión del diff y `.gitignore`/gitleaks |
| TC-TT-FB-01-03 | Dado el proyecto, entonces las apps Android e iOS están registradas y sus archivos de configuración (`google-services.json`, `GoogleService-Info.plist`) no se versionan. | Captura de Project settings → Your apps; los archivos están en `.gitignore` |
| TC-TT-FB-01-04 | Dado el plan de Firebase, entonces el costo y los límites de SMS están confirmados antes de la demo. | Nota con el plan, cuotas y costo (riesgo 1 de `docs/DECISION_TECNICA.md`) |

## 3. Alcance / No-alcance
- **In (este repositorio):**
  - Proyecto de Firebase, autenticación por teléfono con números de prueba, FCM y registro de las apps Android e iOS.
  - La misma tarea se cubre en `knock-frontend` con su propio PR y la misma clave.
- **Out:**
  - Validación del token en el backend y sesión propia (TT-API-02).
  - Integración en la app (TT-MOB-*).
  - Secretos en Key Vault (TT-SEC-01).

## 4. Contratos API y eventos
— (sin endpoints ni eventos)

## 5. Modelo de datos
— (sin tablas)

## 6. Seguridad, privacidad y amenazas
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| Abuso o costo de SMS | Solo números de prueba en desarrollo y demo; confirmar cuotas y límites | TC-TT-FB-01-04 |
| Fuga de credenciales o archivos de configuración | No se versionan; secretos fuera del repositorio | TC-TT-FB-01-02 · TC-TT-FB-01-03 |

## 7. Pantallas
—

## 8. Pruebas, gates y límites
**Plan de pruebas:** No aplican las pruebas unitarias, de integración, contrato, mutación, rendimiento ni de UI: es una tarea de configuración o gestión sin código de producto. Cada caso se verifica con la evidencia indicada (comando o captura), que se adjunta en el PR.
**Gates:** `AGENTS.md §5` en lo que aplique a una tarea sin código: `sdd_lint.py`, `sync_agents.py --check`, `codemap.py --check` y `check_exceptions.py` en OK, `Memoria.md` al día y «Uso de IA» completo en el PR. No hay build ni pruebas de código.
**Límites y dudas abiertas:**
- Identificadores de paquete (Android) y bundle (iOS): dependen de TT-MOB-01.
- Costo y límites de SMS por confirmar (riesgo 1).
- Un agente no opera la consola de Firebase: la ejecuta una persona.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-10-12
- [x] Sin conflicto con la constitución
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Firmada (tras el merge)
