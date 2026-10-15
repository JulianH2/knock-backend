---
id: "TT-MOB-02"
tipo: "tarea técnica"
jira: "KNOCK-25"
titulo: "Tema claro y oscuro con la paleta de la maqueta y archivos de traducción (ARB es/en)"
epica: "EP-13"
sprint: "Semana de prueba de roles (12/10 – 16/10/2026)"
responsable: "Erick Trejo Reséndiz"
puntos: "2"
fuente: "TT-MOB-02_TemaClaroOscuroPaleta_Knock.docx"
---

> Documento **generado** desde la ficha del backlog (`TT-MOB-02_TemaClaroOscuroPaleta_Knock.docx`) con `scripts/export_backlog_md.py`. No lo edites a mano: corrige la ficha y vuelve a generar. El **estado** de la tarea vive en Jira (KNOCK-25); el contrato aprobado de la tarea es su spec en `specs/`.

# TT-MOB-02 — Tema claro y oscuro con la paleta de la maqueta y archivos de traducción (ARB es/en)

## 1. Datos generales

- **ID de la tarea:** TT-MOB-02
- **Puntos:** 2
- **Título:** Tema claro y oscuro con la paleta de la maqueta y archivos de traducción (ARB es/en)
- **Área:** MOB · App móvil base
- **Épica:** EP-13
- **Responsable:** Erick Trejo Reséndiz
- **Sprint:** Semana de prueba de roles (12/10 – 16/10/2026)
- **Día sugerido:** Jueves 15
- **Clave de Jira:** KNOCK-25
- **Rama:** chore/KNOCK-25-tema-e-idiomas  →  PR a sprint-00
- **Depende de:** TT-MOB-01
- **Terminada cuando:** Cambiar tema e idioma funciona sin reiniciar.

## 2. Objetivo

Que la app nazca con el tema claro y oscuro de la maqueta y con todos los textos en archivos de traducción, para no tener que rehacer pantallas después.

## 3. Alcance

- Tema claro y oscuro con la paleta de la maqueta.
- Traducciones con gen-l10n (es y en).

## 4. Fuera de alcance

- Pantalla de Configuración para elegir idioma y tema (HU-052, Sprint 1).
- Traducir toda la app al inglés (HU-053, Media).

## 5. Pasos

1. Crear lib/core/theme con los colores de la maqueta: naranja #E9682D (acciones), azul petróleo #1C3A4C (apoyo), crema #FBF5E9 (fondo) y tinta #231A15 (texto), más sus equivalentes oscuros.
1. Usar la fuente de la maqueta si su licencia lo permite; si no, una de Google Fonts parecida.
1. Agregar flutter_localizations e intl; crear l10n.yaml y lib/l10n/app_es.arb (fuente) y app_en.arb.
1. Pasar los títulos de las 5 pestañas a claves estables (tab.inicio, tab.explorar…).
1. Un proveedor de Riverpod guarda el tema (sistema, claro, oscuro) y el idioma; por ahora se cambia desde un menú de prueba.
1. Prueba de widget: al cambiar el idioma a inglés las pestañas cambian de texto.

## 6. Configuración, archivos y herramientas

| Archivo, comando o herramienta | Para qué |
|---|---|
| l10n.yaml · lib/l10n/*.arb | Textos por idioma |
| lib/core/theme/ | Colores y tipografía de la maqueta |

## 7. Cómo se verifica (criterios de terminado)

| # | Qué se comprueba | Cómo |
|---|---|---|
| V1 | Cambiar tema e idioma funciona sin reiniciar la app | Prueba manual |
| V2 | No hay textos visibles escritos directo en los widgets | Revisión del PR |
| V3 | El contraste del texto sobre el fondo crema cumple al menos 4.5:1 | Revisión con un verificador de contraste |

## 8. Seguridad y reglas

- Nunca pasar a la IA secretos, tokens, contraseñas, credenciales, cadenas de conexión, documentos de identidad ni datos personales reales.

## 9. Uso de IA

- Se permite y se espera usar IA (Claude Code, opencode u otra) siguiendo AGENTS.md y la constitución del proyecto.
- Antes de crear algo, buscar en el repositorio si ya existe; no duplicar funciones ni configuraciones.
- Entender y poder explicar en la revisión todo lo generado; validarlo con las comprobaciones de la sección 7.
- Declarar en la sección «Uso de IA» del PR qué herramienta se usó, qué generó y cómo se verificó.

## 10. Evidencia para el PR

- Capturas en claro, oscuro, español e inglés.

## 11. Referencias

- Maqueta: css/knock.css (variables de color) y js/i18n/.
- DECISIONES.md #14.
