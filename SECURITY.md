# Política de seguridad — Knock

## Reportar una vulnerabilidad
**No abras un issue público.** Usa **Security → Report a vulnerability** (reporte privado de GitHub) en este repositorio.
Incluye: componente y versión, pasos para reproducir, impacto y, si puedes, una prueba de concepto sin datos reales.

| Paso | Plazo objetivo |
|---|---|
| Acuse de recibo | 2 días hábiles |
| Clasificación (severidad y explotabilidad, CVSS 4.0) | 5 días hábiles |
| Corrección de críticas / altas | 7 / 14 días |
| Aviso a quien reportó y crédito (si lo desea) | al publicar la corrección |

Proyecto académico en piloto: no hay programa de recompensas. No hagas pruebas contra producción ni con datos de otras personas;
usa Staging y cuentas de prueba.

## Versiones con soporte
Solo la versión más reciente de cada componente (`api-v*`, `app-v*`, `panel-v*`).

## Cómo gestionamos hallazgos
- **Bloqueo:** una vulnerabilidad crítica o alta explotable en el contexto de ejecución bloquea el merge (CI: CodeQL, Semgrep, OSV-Scanner, dependency-review, Trivy, ZAP).
- **Falsos positivos y riesgos aceptados:** solo con evidencia y registro en [`security/exceptions.yaml`](security/exceptions.yaml) (responsable, alcance, vencimiento ≤ 90 días, plan). El CI falla con excepciones vencidas o supresiones sin `EXC-NNN`.
- **Datos sensibles:** INE, selfie, CURP, dirección y teléfono se tratan según la constitución §6. Un incidente con estos datos se escala de inmediato al Tech Lead.
- Un escáner sin alertas no prueba que el sistema sea seguro: cada historia incluye su análisis de amenazas y pruebas de seguridad negativa.
