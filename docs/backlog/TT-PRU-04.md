---
id: "TT-PRU-04"
tipo: "tarea técnica"
jira: "KNOCK-32"
titulo: "Demo integrada del viernes: entrar con número de prueba y ver tu colonia en la app"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "Todo el equipo"
puntos: "2"
fuente: "TT-PRU-04_DemoIntegradaViernesEntrar_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-PRU-04_DemoIntegradaViernesEntrar_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-32); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-PRU-04 — Demo integrada del viernes: entrar con número de prueba y ver tu colonia en la app

## 1. Datos generales

- **ID de la tarea:** TT-PRU-04
- **Puntos:** 2
- **Título:** Demo integrada del viernes: entrar con número de prueba y ver tu colonia en la app
- **Área:** PRU · Prueba de roles
- **Épica:** EP-13
- **Responsable:** Todo el equipo
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Viernes 16
- **Clave de Jira:** KNOCK-32
- **Rama:** chore/KNOCK-32-demo-integrada  →  PR a sprint-00
- **Depende de:** TT-API-02, TT-BD-02, TT-MOB-01, TT-FB-01
- **Terminada cuando:** Demo grabada; las tres capas conectadas.

## 2. Objetivo

Comprobar que las tres capas funcionan juntas y que el equipo sabe integrarse: entrar con un número de prueba y ver tu colonia en la app.

## 3. Alcance

- En la app: entrar con un número de prueba de Firebase y elegir "Usar mi ubicación" o escribir un código postal.
- En la API: canjear el token (TT-API-02) y devolver la colonia (TT-BD-02).
- Mostrar la colonia en la pestaña Inicio.

## 4. Fuera de alcance

- Diseño final de las pantallas de acceso y zona (HU-001, HU-002 y HU-009 del Sprint 1).

## 5. Pasos

1. Jueves: acordar el contrato de los dos endpoints (POST /api/v1/auth/firebase y GET /api/v1/zonas/colonia?lat=&lng= o ?cp=) en docs/specs/demo-s0.md.
1. Erick agrega firebase_auth y la pantalla mínima de celular y código.
1. Guillermo expone el endpoint de zona usando la consulta de Ángel.
1. Ángel prepara 3 puntos y 2 códigos postales de prueba (uno fuera de cobertura).
1. Julián integra en sprint-00, corre todo localmente y conduce la demo.
1. Grabar un video corto de la demo para la evidencia.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| docs/specs/demo-s0.md | Contrato y guion de la demo |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Con un número de prueba se entra y se ve la colonia correcta | Demo en vivo |
| V2 | Un CP fuera de la ciudad piloto muestra "Fuera de cobertura" | Demo en vivo |
| V3 | Todo lo usado está fusionado en sprint-00 | GitHub |

## 8. Seguridad y reglas

- Solo números de prueba.
- La ubicación se convierte a colonia y no se guarda.
- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Video de la demo.

## 11. Referencias

- TT-API-02 · TT-BD-02 · TT-MOB-01 · TT-FB-01.
