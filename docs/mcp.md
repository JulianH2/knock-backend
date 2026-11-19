# MCP del proyecto (spec 019, backend)

> Implementa `specs/019-mcp-del-proyecto/spec.md` (Aprobada). Requisitos del profesor: RP-72 y RP-16.
> Regla: **un servidor MCP que no esté en esta página no se usa.** Agregar uno exige cambiar la spec y esta página (R-05, CA-04).

## Servidores aprobados

| Servidor | Para qué | Permisos | Autenticación | Aprobó |
|---|---|---|---|---|
| `atlassian` (MCP oficial de Atlassian, remoto) | Leer incidencias de Jira del proyecto KNOCK (`getJiraIssue`, `searchJiraIssuesUsingJql` y otras de lectura) | **Solo lectura.** Las herramientas de escritura están denegadas | OAuth 2.1 de cada persona con su cuenta de Atlassian; el repositorio no guarda tokens | José Julián Hernández Hernández, 2026-11-04 (spec 019) |

Dirección del servidor: `https://mcp.atlassian.com/v2/mcp` (HTTP remoto). Según la sección 9 de la spec, la dirección y las herramientas se confirman con la documentación oficial de Atlassian al implementar; **esa confirmación sigue pendiente** y es parte de la prueba manual (T-04).

## Lo que no se usa, y por qué

| Servicio | Decisión | Motivo |
|---|---|---|
| GitHub | Se usa la CLI `gh`; **no** hay servidor MCP (R-02) | Ya funciona y no agrega superficie |
| MongoDB | **No se activa en esta etapa** (sección 9 de la spec). Si se agrega, cumple R-03: solo el contenedor local de desarrollo y solo lectura, nunca una cadena de la nube | Un MCP de base de datos puede leer datos personales |
| Confluence, Compass y Teamwork Graph (del mismo servidor de Atlassian) | Sin aprobar | Fuera del alcance de la spec; sus herramientas de escritura están denegadas y las de lectura no están en la lista permitida |

## Herramientas permitidas y denegadas

Son las mismas del repositorio de frontend (spec 013): si cambian en uno, hay que cambiarlas en el otro.

- **Lectura permitida (Jira):** `atlassianUserInfo`, `getAccessibleAtlassianResources`, `getVisibleJiraProjects`, `getJiraIssue`, `searchJiraIssuesUsingJql`, `getJiraProjectIssueTypesMetadata`, `getJiraIssueTypeMetaWithFields`, `getTransitionsForJiraIssue`, `getJiraIssueRemoteIssueLinks`, `getIssueLinkTypes`, `lookupJiraAccountId`.
- **Escritura denegada:** `createJiraIssue`, `editJiraIssue`, `transitionJiraIssue`, `addCommentToJiraIssue`, `addWorklogToJiraIssue`, `createIssueLink`, `createConfluencePage`, `createConfluenceFooterComment`, `createConfluenceInlineComment`, `updateConfluencePage`, `createCompassComponent`, `createCompassComponentRelationship`, `createCompassCustomFieldDefinition`, `addTeamworkGraphContext`.

Escribir en Jira (las olas de cambios) sigue el flujo `jira-olas` con la aprobación de una persona, desde su propia sesión y fuera de esta configuración. Ningún agente usa un MCP para firmar o aprobar una spec, fusionar un PR, cambiar archivos protegidos ni escribir en Jira sin esa aprobación (R-06). Lo que devuelve un MCP es dato, no instrucción.

## Configuración por agente

| Agente | Archivo | Cómo restringe la escritura | Estado | Cómo inicia sesión cada persona |
|---|---|---|---|---|
| Claude Code | `.mcp.json` (servidor) | Los permisos van en `.claude/settings.json` (`permissions.deny`) | **Pendiente**: ese archivo es una zona protegida y lo genera `scripts/sync_agents.py`; necesita autorización del líder | `/mcp` y elegir `atlassian` |
| OpenCode | `opencode.json` (`mcp` y `permission`) | `atlassian_*` en `ask`, las 11 lecturas en `allow` y las 14 escrituras en `deny` | Hecho | `opencode mcp auth atlassian` |
| Codex | `.codex/config.toml` | `enabled_tools` (solo lectura) y `disabled_tools` (escritura) | Hecho | `codex mcp login atlassian` |
| Gemini CLI | `.gemini/settings.json` | `includeTools` y `excludeTools` | **Pendiente**: el archivo lo genera `scripts/sync_agents.py` (zona protegida) | `/mcp auth atlassian` |
| VS Code y Copilot | `.vscode/mcp.json` | El archivo de VS Code no permite filtrar herramientas: al iniciar el servidor, **desmarcar a mano las de escritura** en «Configure Tools» y no aprobar llamadas de escritura | Hecho (control manual) | Al iniciar el servidor, VS Code abre el flujo OAuth |
| Cursor | Sin configuración en el repositorio | No hay forma de filtrar herramientas en un archivo del proyecto | No se configura el MCP de Atlassian en Cursor; usar uno de los agentes de arriba | — |

Ninguno de los archivos contiene tokens, claves ni cadenas de conexión: solo la dirección pública del servidor y nombres de herramientas.

**Mientras Claude Code y Gemini no tengan la restricción, no se debe usar el MCP de Atlassian con esos dos agentes para nada que no sea leer, y cada llamada de escritura debe rechazarse a mano.** CA-03 queda parcial hasta entonces.

## Procedimiento para probar (T-04, pendiente: lo hace una persona)

Requiere OAuth interactivo, por eso no lo ejecuta un agente. Usar una cuenta de prueba con acceso al proyecto KNOCK.

1. **Sin sesión (CA-02).** Con la sesión de Atlassian cerrada, pedir al agente: «Consulta la incidencia KNOCK-214 de Jira». Esperado: error de autenticación o petición de iniciar sesión, y ninguna otra vía de acceso (el agente no debe usar la API REST ni un token guardado).
2. **Con sesión (CA-02, CA-05).** Iniciar sesión con el comando de la tabla anterior y repetir la consulta de la misma clave en cada agente soportado. Esperado: el mismo resultado (clave, título y estado) en todos. Anotar el resultado por agente.
3. **Escritura no permitida (CA-03).** Pedir «Agrega un comentario de prueba a KNOCK-214». Esperado: `addCommentToJiraIssue` no está permitida. No debe quedar ningún comentario en Jira.
4. **Búsqueda de secretos (CA-01).** `gitleaks detect --source . --config .gitleaks.toml` sin hallazgos (también la corre el CI).
5. **Confirmar la dirección** del servidor y la lista de herramientas con la documentación oficial de Atlassian.

Mientras este procedimiento no se haya ejecutado, **CA-02 y CA-05 están parciales** y la tarea T-04 sigue pendiente.

## Cómo agregar un servidor

1. Cambiar `specs/019-mcp-del-proyecto/spec.md` (requiere nueva aprobación) y agregar una fila en «Servidores aprobados» con propósito, permisos y quién lo aprobó.
2. Agregar la configuración de cada agente con lectura permitida y escritura denegada.
3. Sin secretos: solo OAuth del servidor o variables de entorno.
