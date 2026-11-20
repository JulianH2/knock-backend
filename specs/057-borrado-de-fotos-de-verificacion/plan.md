# plan.md — 057 Borrado de fotos de verificación (KNOCK-87)

> Estado: Borrador. Se confirma con la aprobación de la spec.

## 1. Mapeo de archivos
- **Reutilizar (verificar con `python scripts/codemap.py --query` antes de crear):**
  - Ejecutor de tareas programadas de Identidad e `IManejadorDeTarea` (spec 054; hoy puede no existir en Identidad).
  - `outbox` y su escritura transaccional (spec 054 R-02), `TimeProvider`.
  - `VerificacionStore`, `FotoVerificacionStore`, `CifradoDeVerificacion`, `VerificacionDecisionService` (spec 025, aún sin implementar).
  - Escritura de `panel_auditoria` (spec 033 R-23).
  - Manejador de `EliminarCuenta` (spec 039).
- **Crear:**
  - `src/Knock.Identity.Api/Modules/Trust/Services/PurgaDeVerificacion.cs`
  - `src/Knock.Identity.Api/Modules/Trust/Tareas/AvisarAlVerificadorHandler.cs` y `BorrarFotosDeVerificacionHandler.cs`
  - `src/Knock.Identity.Api/Modules/Trust/Events/VerificacionSinRevisarAlertada.cs` y `FotosDeVerificacionBorradas.cs`
  - `db/057-identity-verificaciones-purga.js`
  - `tests/{UnitTest,IntegrationTest,SecurityTest,DatabaseTest}/Trust/Spec057*Tests.cs`
- **Modificar:** `VerificacionStore` (campos nuevos, transición condicionada), `VerificacionService.Enviar` (programa las dos tareas), `VerificacionDecisionService` (llama a la purga), manejador de `EliminarCuenta`, registro de manejadores del ejecutor, `docs/events.md`, `Memoria.md`.
- **No tocar:** `.github/`, `scripts/`, configuración de analizadores, constitución, scripts de `db/` ya fusionados (incluido el de la spec 025), `docs/openapi*.json` a mano, módulos o colecciones de otros servicios.

## 2. Riesgos y efectos secundarios
- **Prerrequisitos:** spec 025 (T-01, T-03, T-04, T-06), spec 054 (ejecutor de Identidad) y spec 053 (decisión) deben estar fusionadas o coordinarse en el mismo sprint.
- **Orden de la purga:** primero se destruye la llave en la misma escritura que el estado (lo irreversible, que vuelve ilegible todo); después se borran los blobs. Invertirlo deja fotos legibles si falla a medias.
- **Concurrencia:** decisión contra vencimiento (TC-07) y dos instancias (TC-04); la transición debe ser un `findOneAndUpdate` condicionado por estado.
- **Respaldos y llave en Key Vault:** dudas §8-2 y §8-4 de la spec; pueden cambiar el diseño.
- **Datos:** la migración solo agrega campos; no borra datos existentes.
- **Eventos:** el consumo de los dos eventos no existe todavía (la spec 037 difiere `VerificacionDecidida`); comprobar que emitirlos sin consumidor no rompe el procesador del buzón.

## 3. Pruebas por caso
| Caso | Prueba | Suite |
|---|---|---|
| TC-HU-068-01 | `Spec057PurgaTests.Decision_Destruye_Llave_Y_Fotos_TC_HU_068_01` | IntegrationTest |
| TC-HU-068-02 | `Spec057AlertaTests.Alerta_A_Las_24_Horas_Una_Sola_Vez_TC_HU_068_02` | IntegrationTest |
| TC-HU-068-03 | `Spec057PurgaTests.Vence_A_Los_Siete_Dias_Y_Vuelve_A_Sin_Verificar_TC_HU_068_03` | IntegrationTest |
| TC-HU-068-04 | `Spec057ConcurrenciaTests.Dos_Instancias_Purgan_Una_Sola_Vez_TC_HU_068_04` | IntegrationTest |
| TC-HU-068-05 | `Spec057AuditoriaTests.Auditoria_Sin_Contenido_Del_Documento_TC_HU_068_05` | IntegrationTest |
| TC-HU-068-06 | `Spec057PurgaIdempotenteTests.Repetir_La_Purga_No_Cambia_Nada_TC_HU_068_06` | UnitTest |
| TC-HU-068-07 | `Spec057ConcurrenciaTests.Decision_Y_Vencimiento_Simultaneos_TC_HU_068_07` | IntegrationTest |
| TC-HU-068-08 | `Spec057InvariantesTests.Documento_Purgado_Solo_Conserva_El_Resultado_TC_HU_068_08` | DatabaseTest |
| TC-HU-068-09 | `Spec057CopiasTests.Purga_Alcanza_Cuarentena_Definitivo_Y_Enlaces_TC_HU_068_09` | IntegrationTest |
| TC-HU-068-10 | `Spec057FallosTests.Fallo_Al_Borrar_Blob_Reintenta_Y_Ya_Es_Ilegible_TC_HU_068_10` | IntegrationTest |
| TC-HU-068-11 | `Spec057BajaDeCuentaTests.Baja_De_Cuenta_Purga_Las_Fotos_TC_HU_068_11` | IntegrationTest |
| TC-HU-068-12 | `Spec057AccesoTests.Sin_Rutas_Publicas_Ni_Fotos_En_El_Estado_TC_HU_068_12` | SecurityTest |
| TC-HU-068-13 | `Spec057ProgramacionTests.Envio_Programa_Alerta_Y_Borrado_Sin_Duplicar_TC_HU_068_13` | IntegrationTest |
| TC-HU-068-14 | `Spec057RegistrosTests.Sin_Datos_Sensibles_En_Registros_Eventos_Y_Tareas_TC_HU_068_14` | SecurityTest |

## 4. Secuencia
1. Contrato y modelo: campos, script de `db/`, eventos y tipos de tarea; prueba de base de datos.
2. Dominio y servicio: `PurgaDeVerificacion` e idempotencia (unitarias).
3. Manejadores de tarea, programación al enviar y cierre al decidir; baja de cuenta (integración).
4. Seguridad negativa, concurrencia y registros.
5. `docs/events.md`, gates, `Memoria.md` e informe del PR.

## 5. Verificación (comandos exactos)
```bash
dotnet build Knock.slnx -c Release --no-restore
dotnet format Knock.slnx --verify-no-changes
dotnet test tests/UnitTest -c Release --no-build
dotnet test tests/IntegrationTest -c Release --no-build
dotnet test tests/SecurityTest tests/DatabaseTest -c Release --no-build
python scripts/sdd_lint.py
python scripts/codemap.py --check
python scripts/ci/check_exceptions.py
python scripts/sync_agents.py --check
semgrep scan --config .semgrep/semgrep.yaml --error
dotnet tool run dotnet-stryker -- --config-file stryker-config.json --since:sprint-05
```
