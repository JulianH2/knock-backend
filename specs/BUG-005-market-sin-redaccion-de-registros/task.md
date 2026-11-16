# BUG-005 — tareas

> **Estado:** T1 y T2 verificadas; T3 parcial por duplicación; T4 con evidencia local, pendiente de PR y decisión del Tech Lead.

## T1 — Confirmar y decidir [Docs]
- **Estado:** hecha; diagnóstico y decisión de Julián en la spec §8, reproducción sintética en T2.
- **Verificar:** diagnóstico y reproducción sintética; resolver con Julián la coordinación con KNOCK-251 y las dudas de §8.
- **Cubre:** TC-BUG-005-01 a 06.
- **No hacer:** tomar la propuesta de biblioteca como decisión aprobada.

## T2 — Pruebas de consola [Pruebas]
- **Estado:** hecha; TC-01 a 05 fallaron antes del arreglo y pasan después, junto con TC-07.
- **Crear o extender:** pruebas que capturen el camino real de consola de Market.
- **Verificar:** TC-BUG-005-01 a 05 y que fallen al omitir la redacción.
- **No hacer:** usar datos reales ni limitarse a probar el redactor aislado.

## T3 — Reutilizar y corregir [Back]
- **Estado:** parcial; corrección y pruebas listas, pero el formateador es una copia identificada por codemap y espera decisión del Tech Lead; KNOCK-251 contempla extraerlo.
- **Modificar:** configuración de consola de Market y, según decisión aprobada, ubicación compartida y consumidores del redactor.
- **Verificar:** TC-BUG-005-01 a 06, fronteras y regresión de servicios afectados.
- **No hacer:** copiar código ni introducir un clon nuevo o referencias entre servicios.

## T4 — Evidencia y memoria [Docs]
- **Estado:** verificación local y memoria listas; falta adjuntar resultados al PR y resolver la duplicación con el Tech Lead.
- **Verificar:** build, pruebas, formato, trazabilidad y jscpd; adjuntar resultados al PR.
- **Modificar:** documentación pertinente y `Memoria.md` con la decisión y resultados reales.
