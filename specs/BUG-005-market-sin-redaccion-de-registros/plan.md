# plan.md — BUG-005 Market sin redacción de registros

> **Estado:** Aprobada. Las tareas se pueden ejecutar.

## 1. Mapeo de archivos
- **Reutilizar:** configuración de consola y `TelemetryRedactor` de los servicios, y pruebas existentes de observabilidad; buscar en el mapa de código antes de crear.
- **Modificar:** `src/Knock.Market.Api/Hosting/LoggingExtensions.cs` y, según la decisión de §8 de la spec, la ubicación compartida del redactor y sus consumidores.
- **Crear o extender:** pruebas de consola de Market en `tests/UnitTest/Hosting/`, según estructuras existentes; biblioteca técnica compartida solo si se decide y aprueba.
- **Documentar:** decisión de reutilización, verificación y `Memoria.md` al finalizar la corrección.

## 2. Riesgos y efectos secundarios
- Extraer el redactor puede afectar a los demás servicios: ejecutar sus pruebas de regresión y respetar sus fronteras.
- Cambiar el formateador puede romper JSON o perder propiedades: TC-05 lo detecta.
- Los patrones pueden no cubrir todas las representaciones de datos sensibles; resolver el alcance de los ejemplos en la spec sin usar datos reales.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-BUG-005-01 a 04 | Capturar la consola real de Market para celular, correo, token y coordenadas | UnitTest / seguridad negativa |
| TC-BUG-005-05 | Analizar JSON y comprobar datos inocuos | UnitTest |
| TC-BUG-005-06 | Análisis de clones antes y después | jscpd (comando) |

## 4. Secuencia
1. Contrastar diagnóstico, reproducir con datos sintéticos y resolver las dudas con Julián.
2. Buscar y elegir reutilización o extracción coordinada con KNOCK-251, sin copiar código.
3. Añadir pruebas que detecten el defecto y aplicar la corrección tras la aprobación.
4. Ejecutar regresión, arquitectura si aplica y jscpd; registrar resultados y memoria.

## 5. Verificación (comandos exactos)
```bash
dotnet build Knock.slnx -c Release
dotnet test Knock.slnx -c Release
dotnet format Knock.slnx --verify-no-changes
python scripts/sdd_lint.py --trace BUG-005
```
El comando jscpd del repositorio queda pendiente de confirmar en §8; comparar resultados antes y después y adjuntarlos al PR.
