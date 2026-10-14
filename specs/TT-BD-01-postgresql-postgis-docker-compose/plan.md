# plan.md — TT-BD-01 — PostgreSQL/PostGIS local con Docker Compose

## 1. Mapeo de archivos
- **Reutilizar:** No se prevén cambios de código en esta spec; los detalles de implementación siguen abiertos.
- **Crear:** Pendiente de confirmar los archivos necesarios tras definir el comando único y la relación con TT-API-01.
- **Modificar:** Pendiente de confirmar.
- **No tocar:** Cadenas de conexión o secretos no deben incorporarse al repositorio. No modificar alcance ajeno a KNOCK-22.

## 2. Riesgos y efectos secundarios
- Divulgación accidental de la cadena de conexión: mantenerla fuera del repositorio mediante user-secrets o variables de entorno (TC-TT-BD-01-02).
- Aplicar migraciones a un destino distinto de la base local: el destino y su verificación concreta están pendientes de definición (TC-TT-BD-01-01).
- La tarea depende de TT-API-01 (KNOCK-20); el efecto de esa dependencia en el esquema y la migración debe aclararse antes de implementar.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-TT-BD-01-01 | Ejecutar el único comando acordado y comprobar que la base local queda migrada. Comando y verificación por confirmar. | Integración/base de datos; suite pendiente de identificar. |
| TC-TT-BD-01-02 | Confirmar que la cadena no aparece en archivos versionados y que la configuración externa permite conectar. | Revisión de seguridad y conexión local; procedimiento pendiente de confirmar. |

## 4. Secuencia
1. Resolver las dudas de §8 de la spec sobre el comando, la configuración y la dependencia.
2. Precisar el destino local y el criterio observable de migración.
3. Implementar solo tras aprobación humana de la spec.
4. Verificar TC-TT-BD-01-01 y TC-TT-BD-01-02 con la configuración externa, sin incorporar secretos al repositorio.
5. Ejecutar los gates aplicables y registrar resultados reales.

## 5. Verificación (comandos exactos)
```bash
python scripts/sdd_lint.py
```
El comando de migración y el de comprobación de la base local quedan por definir; no se inventan en este borrador.
