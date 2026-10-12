<!-- Título: KNOCK-N · <ID> <título>   p. ej. «KNOCK-45 · HU-024 Pedir una cotización» -->

## Issue y spec
- Jira: KNOCK-
- Spec: `specs/<ID>-…/spec.md` (estado: Aprobada)

## Criterios
<!-- Cada caso TC de la spec que cubre este PR y cómo se verifica. -->
| Caso | Prueba / evidencia | Resultado |
|---|---|---|
| TC-… | `Suite/Clase.Metodo_TC_…` | ✅ / ❌ |

## Pruebas
<!-- Comandos ejecutados de verdad y su resultado. Lo que no se pudo ejecutar, con el motivo. -->
- [ ] Build, formato y análisis estático
- [ ] Pruebas de la suite afectada (unitarias, integración, widget/componente, E2E según el plan)
- [ ] Seguridad negativa / contrato (si aplica)
- [ ] `python scripts/sdd_lint.py --trace <ID>` · `python scripts/codemap.py --check` · `python scripts/ci/check_exceptions.py`

## Seguridad y amenazas
<!-- Resumen de la §6 de la spec: amenazas relevantes, controles y su prueba. Datos sensibles tocados. -->

## Riesgos y pendientes
<!-- Efectos secundarios, deuda, excepciones (EXC-NNN) y lo que queda fuera. -->

## Uso de IA
<!-- Obligatorio. Si no se usó, escribe «No se usó IA». Este es el único lugar donde se declara la IA: sin Co-Authored-By de agentes ni «Generated with…» en commits o PR. -->
- Herramienta:
- Qué generó:
- Cómo se validó (pruebas que fallarían si la lógica estuviera mal, revisión del diff):

## Evidencia
<!-- Capturas o video para cambios visuales; reporte del security-reviewer y del traceability-clerk. -->
