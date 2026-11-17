# Spec 055 — Biblioteca técnica compartida (`Knock.Hosting`)

> **Estado:** Borrador · **Repositorio:** knock-backend · **Rector:** `constitution.md` (requiere la enmienda 1.2.0 de este mismo cambio)
> **Clave de trabajo:** KNOCK-251 (TT-API-12 · Mover ForwardedHeadersExtensions y el redactor de registros a una biblioteca compartida). Las etapas 2 y 3 llevan su propia tarjeta (por crear).
> **Servicio:** gateway y los tres servicios (`Hosting/`); nueva biblioteca `src/Knock.Hosting` · **Colecciones:** ninguna
> **Sprint:** etapa 1 en Sprint 4 o Sprint 5, según decida el Tech Lead; etapas 2 y 3 en Sprint 5 · **Responsable sugerido:** Erick (etapa 1, como asigna Jira)

## 1. Objetivo
Tener una sola implementación de cada pieza técnica que hoy se copia entre el gateway y los tres servicios, para que un arreglo de seguridad o de privacidad se haga una vez y llegue a todos. Hoy hay 9 excepciones de jscpd y codemap vivas por esas copias (EXC-007, EXC-008, EXC-015, EXC-016, EXC-018, EXC-020, EXC-021 y EXC-025). Cuatro vencen el 2026-12-04 y dos el 2026-12-15. Además, el mismo cambio se tiene que repetir en cuatro archivos, como pasó con la redacción de consultas de la spec 035.

La constitución 1.1.0 solo permite compartir `Knock.Contracts`, «solo tipos y contratos, sin lógica», y EXC-008 pide «su spec y el cambio de la constitución que la permita». Esta spec es esa spec y trae la enmienda: define qué puede entrar en la biblioteca y qué no, cómo se prueba que sigue siendo técnica, y en qué orden se mueven las piezas.

## 2. Historias y alcance
**2.1 Historias**
- **TT-API-12 (KNOCK-251), etapa 1:** crear `Knock.Hosting`, mover `ForwardedHeadersExtensions` (y su equivalente del gateway) y el redactor de registros (`TelemetryRedactor`, `RedactingJsonFormatter` y sus procesadores), y retirar EXC-020. *Terminada cuando:* la constitución permite la biblioteca, existe una sola implementación de cada pieza, los cuatro proyectos la usan, jscpd no reporta esos clones, EXC-020 está retirada, EXC-007 ya no cubre `TelemetryRedactor` y siguen pasando las pruebas de las specs 006, 011, 035 y BUG-005.
- **Etapa 2 (tarjeta por crear, sigue a KNOCK-247):** mover el límite de peticiones (`SharedRateLimiter`, `RedisRequestCounter`, `RateLimitingExtensions`) y retirar EXC-025. *Terminada cuando:* hay un solo limitador, el límite compartido entre copias de la spec 006 (R-05) y los valores de la spec 009 no cambian, y EXC-025 está retirada.
- **Etapa 3 (tarjeta por crear):** mover el resto del catálogo de §3.2 y retirar EXC-007, EXC-008, EXC-015, EXC-016, EXC-018 y EXC-021. *Terminada cuando:* no queda ninguna copia de una pieza del catálogo en un servicio ni ninguna de esas excepciones.

**2.2 Dentro**
- La enmienda a la constitución (§4 y estructura del repositorio) y su número de versión.
- El proyecto `src/Knock.Hosting`: qué referencia, qué expone y qué tiene prohibido.
- El catálogo cerrado de piezas que pueden entrar y la etapa de cada una.
- Las pruebas de arquitectura que mantienen la biblioteca técnica.
- Los cambios de compilación, imágenes y CI que necesita un proyecto compartido nuevo.
- Cómo se declaran los puntos (spec 012) de un cambio en la biblioteca.
- El retiro de cada excepción ligada a KNOCK-251.

**2.3 Fuera**
- Cambiar el comportamiento de una pieza al moverla. Esta spec solo la mueve y la parametriza. Un cambio de comportamiento lleva su propia spec, como la spec 035 para la redacción.
- Los valores de los límites de peticiones (spec 009 y `docs/rate-limit-matrix.md`) y el diseño del contador compartido (spec 006, KNOCK-247).
- Publicar la biblioteca como paquete NuGet: se consume como `ProjectReference` dentro del repositorio.
- `Knock.Contracts`, que conserva su papel. El `CorrelationIdMiddleware` que ya vive ahí no se mueve en esta spec (ver §15).
- knock-frontend.

## 3. Requisitos
**3.1 La biblioteca**
- **R-01:** Existe `src/Knock.Hosting/Knock.Hosting.csproj` (`net11.0`, `FrameworkReference` a `Microsoft.AspNetCore.App`), incluido en `Knock.slnx`, con espacio de nombres raíz `Knock.Hosting`. Hereda `Directory.Build.props`: advertencias como errores, analizadores y complejidad máxima de 10.
- **R-02:** `Knock.Hosting` no referencia a ningún servicio, al gateway ni a `Knock.Contracts`. El gateway y los tres servicios la referencian con `ProjectReference`.
- **R-03:** `Knock.Hosting` no contiene reglas de negocio:
  - ningún tipo deriva de `ControllerBase`;
  - ningún tipo está en un espacio de nombres `*.Modules.*`;
  - no nombra colecciones de MongoDB ni claves de Redis de un módulo;
  - no contiene DTO de una API.

  Solo hace arranque, tubería HTTP, registro, observabilidad, límites, acceso técnico a datos (clientes y salud, nunca consultas de negocio) y seguridad transversal.
- **R-04:** Solo entran las piezas del catálogo de §3.2. Agregar una pieza fuera del catálogo requiere cambiar esta spec, y el cambio lo aprueba el Tech Lead.
- **R-05:** Cada pieza se configura por opciones o parámetros: nombre del servicio, categoría por defecto del registro, ruta de salud y nombres de las secciones de configuración. Nunca se pregunta en código qué servicio la usa. Las secciones de configuración conservan el nombre que tienen hoy (`ForwardedHeaders`, `Serilog`, `Observability`, `RateLimiting`…), para que no cambien `appsettings*.json` ni las variables de Terraform.
- **R-06:** La biblioteca expone como `public` solo lo que un proyecto consumidor llama. Lo demás queda `internal`, y las pruebas lo ven con `InternalsVisibleTo` hacia `Knock.UnitTest`, `Knock.IntegrationTest` y `Knock.SecurityTest`.
- **R-07:** Las dependencias NuGet de la biblioteca son solo las que ya usan los servicios para esas piezas, con la misma versión: Serilog, `Serilog.Formatting.Compact` y OpenTelemetry en la etapa 1, y StackExchange.Redis y MongoDB.Driver en las etapas 2 y 3. Se compila con `packages.lock.json` y `--locked-mode`. Si se agrega una dependencia nueva, se registra en `Memoria.md`.

**3.2 Catálogo cerrado y etapas**

| Pieza (hoy) | Copias | Excepción que se retira | Etapa |
|---|---|---|---|
| `ForwardedHeadersExtensions.AddKnockForwardedHeaders` (Identity, Messaging, Market) y `CorsExtensions.AddKnockGatewayForwardedHeaders` (gateway) | 4 | EXC-020 (y la parte de EXC-015 y EXC-016) | 1 |
| `TelemetryRedactor`, `CoordinatePairs`, `TelemetryRedactionProcessor`, `LogRedactionProcessor`, `RedactingJsonFormatter` (`Hosting/TelemetryRedactor.cs`) | 4 | EXC-007 (solo `TelemetryRedactor.cs`) y la cuarta copia de BUG-005 | 1 |
| `SharedRateLimiter`, `RedisRequestCounter`, `RateLimitingExtensions` | 3 a 4 | EXC-025 y la parte de EXC-021 | 2 |
| `LoggingExtensions`, `ObservabilityExtensions` | 4 | resto de EXC-007 | 3 |
| `MongoExtensions`, `RedisExtensions`, `HealthExtensions`, `DataExtensions` (solo la parte técnica) | 3 a 4 | EXC-008 y la parte de EXC-021 | 3 |
| `SecurityHeadersMiddleware`, `HstsExtensions`, `ProblemResponses`, `CorsExtensions`/`CorsMiddleware`, `RequestBodyLimitMiddleware`, `JwtSettings` y los parámetros de validación del JWT de `AuthenticationExtensions` | 2 a 4 | EXC-016, la parte de EXC-021 y la parte de EXC-015 | 3 |
| `JsonCompressionMiddleware` y la validación de opciones de caché | 2 | EXC-018 | 3 |
| El resto de `Common/` y `Hosting/` de Identity copiado de Market (versionado de la API, validación, ProblemDetails, OpenAPI y manejo de excepciones de dominio) | 2 | EXC-015 | 3 |

- **R-08:** Una pieza se mueve completa: se borran todas sus copias y sus marcadores `jscpd:ignore` en el mismo PR. No se aceptan dos implementaciones de una pieza que ya está en la biblioteca.
- **R-09:** Desde que la etapa 1 está fusionada, una copia nueva de una pieza del catálogo en un servicio no se acepta con una excepción nueva: la pieza se mueve a la biblioteca. Esto cumple el límite que ya fija EXC-021 («la siguiente copia obliga a hacer la biblioteca compartida»).

**3.3 Etapa 1 (KNOCK-251)**
- **R-10:** Hay un solo `AddKnockForwardedHeaders(IServiceCollection)` en `Knock.Hosting`, y lo usan el gateway y los tres servicios. El gateway deja de tener su propia lectura de `ForwardedHeaders` en `CorsExtensions`. El comportamiento es el de las specs 006 y 035 (R-09 a R-12): sin redes ni proxies no confía en nadie, solo procesa `X-Forwarded-For` y `X-Forwarded-Proto`, y rechaza entradas mal escritas.
- **R-11:** Hay una sola implementación del redactor (`TelemetryRedactor` y los tipos de su archivo) en `Knock.Hosting`. La usan el formateador de consola de Serilog y los procesadores de OpenTelemetry de los cuatro proyectos. Su comportamiento es el que esté fusionado en `sprint-04` al empezar la etapa 1, incluida la regla de consultas de la spec 035 (R-04, PR #121, ya fusionado).
- **R-12:** Las pruebas que hoy recorren las cuatro copias (`Spec011ObservabilityTests`, `Spec035RedaccionTests`, `TelemetryRedactorTests`, `MarketConsoleRedactionTests`, `Spec006ForwardedHeadersTests`) prueban la implementación compartida. Las pruebas que arrancan un host siguen haciéndolo con cada uno de los cuatro proyectos, para comprobar que cada uno la registra. No se borra ningún caso; los duplicados por copia se juntan en uno.
- **R-13:** Al terminar la etapa 1:
  - EXC-020 se borra de `security/exceptions.yaml`;
  - el alcance de EXC-007 deja de nombrar `TelemetryRedactor.cs`;
  - `docs/architecture/clones-baseline.json` se regenera con `python scripts/codemap.py --update-baseline` y pierde las huellas de las piezas movidas;
  - `npx jscpd --config .jscpd.json .` no reporta clones.

**3.4 Compilación, imágenes, CI y versiones**
- **R-14:** Cada `Dockerfile` del gateway y de los tres servicios copia `src/Knock.Hosting/Knock.Hosting.csproj` y su `packages.lock.json` antes del `restore`, y la carpeta completa antes del `publish`, igual que hace hoy con `Knock.Contracts`. La imagen se sigue construyendo sola, sin depender de otra imagen.
- **R-15:** En `ci-pr.yml` y `ci-deploy.yml`, la expresión `SHARED` incluye `^src/Knock\.Hosting/`, de modo que un cambio en la biblioteca construye, prueba y despliega los cuatro componentes. `CODEOWNERS` agrega `/src/Knock.Hosting/` con quien decida el Tech Lead. Son archivos protegidos: el cambio lo hace o lo autoriza una persona.
- **R-16:** Un PR que cambia `src/Knock.Hosting` declara puntos (`puntos-<componente>-N`, spec 012) para cada componente cuyo comportamiento cambia. Un PR que solo mueve una pieza sin cambiar su comportamiento, como la etapa 1, declara `puntos-<componente>-0` para cada componente que reconstruye.
- **R-17:** Cada servicio sigue construyéndose, probándose y desplegándose por separado (spec 001, R-06). La biblioteca no tiene imagen, despliegue ni configuración propios.

## 4. Criterios de aceptación
- **CA-01:** Dado `Knock.Hosting`, cuando se leen sus ensamblados referenciados, entonces no aparece `Knock.Gateway`, `Knock.Identity.Api`, `Knock.Messaging.Api`, `Knock.Market.Api` ni `Knock.Contracts`.
- **CA-02:** Dado `Knock.Hosting`, cuando corre la regla de arquitectura, entonces ningún tipo deriva de `ControllerBase` ni está en un espacio de nombres `*.Modules.*`. Dado un ensamblado de prueba que sí tiene un controlador, la misma regla falla (caso negativo).
- **CA-03:** Dados el gateway y los tres servicios, cuando se buscan sus tipos, entonces ninguno define `ForwardedHeadersExtensions`, `AddKnockGatewayForwardedHeaders`, `TelemetryRedactor`, `CoordinatePairs`, `TelemetryRedactionProcessor`, `LogRedactionProcessor` ni `RedactingJsonFormatter`, y cada uno referencia a `Knock.Hosting`.
- **CA-04:** Dado cada uno de los cuatro proyectos arrancado con `ForwardedHeaders:KnownNetworks` configurado, cuando llega una petición desde la red de confianza con `X-Forwarded-For`, entonces la IP resuelta es la reenviada. Sin configuración, la IP es la de la conexión. Con una red mal escrita, el arranque falla.
- **CA-05:** Dado cada uno de los cuatro proyectos arrancado, cuando registra un evento con datos sensibles (coordenadas, correo, token y una consulta de URL), entonces la línea de consola es JSON válido y sale redactada por la implementación compartida.
- **CA-06:** Dadas las pruebas de las specs 006 (`_R_006_03`), 011 (CA-02), 035 (CA-06) y BUG-005, cuando corren contra la implementación compartida, entonces pasan sin que se haya borrado ningún caso.
- **CA-07:** Dado el repositorio al terminar la etapa 1, cuando corre jscpd, entonces no hay clones; y cuando corre `codemap.py --check`, entonces no hay duplicados nuevos ni violaciones.
- **CA-08:** Dado `security/exceptions.yaml` al terminar la etapa 1, cuando corre `check_exceptions.py`, entonces pasa, EXC-020 no existe y EXC-007 no nombra `TelemetryRedactor.cs`.
- **CA-09:** Dadas las cuatro imágenes, cuando se construyen con su `Dockerfile` desde la raíz del repositorio, entonces compilan en modo `--locked-mode` y arrancan con `/health/ready` en 200.
- **CA-10:** Dado un PR que solo cambia `src/Knock.Hosting`, cuando corre el job `detect`, entonces marca los cuatro componentes como cambiados.
- **CA-11:** Dada la etapa 2 terminada, cuando corren las pruebas del límite de peticiones (spec 006 R-05 y spec 009), entonces pasan; los servicios no definen `SharedRateLimiter`, `RedisRequestCounter` ni `RateLimitingExtensions`; y EXC-025 no existe.
- **CA-12:** Dada la etapa 3 terminada, cuando se buscan las piezas del catálogo en los servicios y las excepciones en `security/exceptions.yaml`, entonces no queda ninguna copia ni ninguna de EXC-007, EXC-008, EXC-015, EXC-016, EXC-018 y EXC-021.

**4.1 Criterio → prueba prevista**

| Criterio | Prueba prevista |
|---|---|
| CA-01 | `HostingLibraryBoundaryTests.No_Referencia_Servicios_Ni_Contratos_CA_055_01` (ArchitectureTest) |
| CA-02 | `HostingLibraryBoundaryTests.Sin_Controladores_Ni_Modulos_CA_055_02` y `HostingLibraryBoundaryTests.La_Regla_Puede_Fallar_CA_055_02` (ArchitectureTest) |
| CA-03 | `HostingLibraryBoundaryTests.Servicios_Sin_Copias_De_La_Etapa_1_CA_055_03` (ArchitectureTest) |
| CA-04 | `Spec055ForwardedHeadersTests.Cada_Proyecto_Usa_La_Implementacion_Compartida_CA_055_04` (UnitTest) |
| CA-05 | `Spec055RedaccionTests.Cada_Proyecto_Redacta_Con_La_Implementacion_Compartida_CA_055_05` (UnitTest) |
| CA-06 | (comando) `dotnet test tests/UnitTest -c Release --filter "FullyQualifiedName~Spec006|FullyQualifiedName~Spec011|FullyQualifiedName~Spec035|FullyQualifiedName~TelemetryRedactor|FullyQualifiedName~MarketConsoleRedaction"`, con el conteo de casos antes y después en el PR |
| CA-07 | (comando) `npx --yes jscpd@4 --config .jscpd.json .` y `python scripts/codemap.py --check` |
| CA-08 | (comando) `python scripts/ci/check_exceptions.py` |
| CA-09 | (comando) job de contenedor del CI (`docker build` de las cuatro imágenes y arranque) |
| CA-10 | (comando) corrida del CI de un PR de la etapa 1: salida del job `detect` con los cuatro en `true` |
| CA-11 | `HostingLibraryBoundaryTests.Servicios_Sin_Copias_De_La_Etapa_2_CA_055_11` y las suites de límites de las specs 006 y 009 |
| CA-12 | `HostingLibraryBoundaryTests.Servicios_Sin_Copias_Del_Catalogo_CA_055_12` y `check_exceptions.py` |

## 5. Contrato
No hay endpoints nuevos ni cambios en el OpenAPI. Interfaces que fija:

| Interfaz | Forma |
|---|---|
| Proyecto | `src/Knock.Hosting/Knock.Hosting.csproj`, espacio de nombres `Knock.Hosting`, sin imagen ni despliegue propios |
| Referencias permitidas | Framework de ASP.NET Core y los paquetes de R-07; ninguna a servicios ni a `Knock.Contracts` |
| Encabezados reenviados (etapa 1) | `Knock.Hosting.ForwardedHeaders.ForwardedHeadersExtensions.AddKnockForwardedHeaders(this IServiceCollection)`; lee `ForwardedHeaders:KnownProxies` y `ForwardedHeaders:KnownNetworks` como hoy |
| Redacción (etapa 1) | `Knock.Hosting.Logging.TelemetryRedactor` (`Redact`, `RedactAttribute`, `RedactUrl`, `ExceptionEvent`, `Placeholder`), `RedactingJsonFormatter(ITextFormatter)` y los dos procesadores de OpenTelemetry |
| Configuración | Mismas secciones y llaves que hoy; ningún `appsettings*.json` ni variable de Terraform cambia de nombre |
| Puntos (spec 012) | `puntos-<componente>-N` por cada componente cuyo comportamiento cambia; `-0` si solo se mueve código |

**Códigos de error:** no hay. Un fallo de configuración sigue impidiendo arrancar, como hoy.

## 6. Modelo de datos
Sin colecciones, claves ni campos nuevos. No cambia ninguna configuración.

## 7. Arquitectura y componentes
- `src/Knock.Hosting/` se organiza por tema, con una carpeta por pieza del catálogo: `ForwardedHeaders/`, `Logging/`, y después `RateLimiting/`, `Data/`, `Security/` y `Http/`. No se organiza por servicio.
- Cada servicio conserva su `Hosting/` para lo que es solo suyo. Por ejemplo, `KnockApplicationExtensions` encadena las llamadas a la biblioteca, y `ModulesExtensions`, `SecuritySettings` y `RevokedSessions` del gateway se quedan en su proyecto.
- `LoggingExtensions` y `ObservabilityExtensions` siguen en cada servicio hasta la etapa 3, pero en la etapa 1 dejan de tener su propio redactor y usan el de la biblioteca.
- Nueva clase `tests/ArchitectureTest/HostingLibraryBoundaryTests.cs`, con el molde de `ServiceBoundaryTests`: reglas positivas, una regla que puede fallar y la lista de piezas prohibidas en los servicios por etapa.
- `Knock.UnitTest` agrega `ProjectReference` a `Knock.Hosting`. Los alias externos de los servicios siguen para las pruebas que arrancan cada `Program`.
- Las pruebas de redacción de las cuatro copias pasan a probar una sola implementación, más una prueba por proyecto que comprueba que la usa (CA-05).

## 8. Dependencias
- **Base:** specs 001 (R-04 y R-06; la enmienda amplía R-04 sin tocar R-06), 006 (encabezados reenviados y límite compartido), 011 (registro y redacción), 012 (puntos), 035 (redacción de consultas y confianza) y BUG-005 (redacción de consola en Market).
- **Antes de empezar la etapa 1:** que el Tech Lead apruebe esta spec y la enmienda (la constitución pide la aprobación de dos integrantes). El PR #121 (spec 035 T-03) ya está en `sprint-04`, así que la etapa 1 parte de las cuatro copias con la regla de consultas.
- **Etapa 2:** KNOCK-247 fusionada (el contador en Redis de Identity y Market, EXC-025).
- **Archivos protegidos:** `.github/workflows/ci-pr.yml`, `.github/workflows/ci-deploy.yml` y `.github/CODEOWNERS` (R-15) los cambia o autoriza una persona.
- **Sin dependencias nuevas:** los paquetes de la biblioteca ya están en los servicios.

## 9. Amenazas y controles
| Amenaza | Control | Caso que lo prueba |
|---|---|---|
| La biblioteca se vuelve un cajón de sastre con lógica de negocio que acopla a los servicios | Catálogo cerrado (R-04), sin referencias a servicios ni a `Knock.Contracts` y reglas de arquitectura | CA-01, CA-02 |
| Un cambio en la biblioteca altera a la vez la seguridad de los cuatro proyectos sin que se note | `SHARED` del CI construye y prueba los cuatro; las pruebas de arranque recorren los cuatro; puntos por componente | CA-04, CA-05, CA-10 |
| Al mover el redactor, un servicio deja de redactar y filtra datos personales en sus registros | Prueba por proyecto con datos sensibles y regresión de las specs 011, 035 y BUG-005 | CA-05, CA-06 |
| Al mover los encabezados reenviados, un servicio confía en cualquiera y se falsifica la IP del límite | Prueba por proyecto con red de confianza, sin configuración y con red mal escrita | CA-04, CA-06 |
| Una imagen sale sin la biblioteca o con otra versión de un paquete | `Dockerfile` con la biblioteca y `--locked-mode` | CA-09 |
| Cadena de suministro: dependencias nuevas o versiones distintas en la biblioteca | R-07: solo los paquetes y versiones ya usados, archivo de bloqueo y auditoría de NuGet | CA-09 |
| Quedan dos implementaciones de la misma pieza y una se queda sin el arreglo | R-08 y R-09, jscpd en cero y la regla de piezas prohibidas en los servicios | CA-03, CA-07, CA-11, CA-12 |

## 10. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Unitarias | Sí | La implementación compartida del redactor y de los encabezados reenviados, con los mismos casos que hoy |
| Integración | Sí | Cada uno de los cuatro proyectos arranca con la biblioteca y la usa (CA-04, CA-05) |
| Arquitectura | Sí | Fronteras de la biblioteca y piezas prohibidas en los servicios, con un caso negativo (CA-01 a CA-03) |
| Seguridad negativa | Sí | `X-Forwarded-For` falso desde fuera de la red de confianza, red mal escrita y datos sensibles en registros |
| Regresión | Sí | Suites de las specs 006, 011, 035 y BUG-005 sin perder casos (CA-06) |
| Contrato | No | No cambia el OpenAPI; el job de contrato del CI comprueba que sigue igual |
| Rendimiento | No | La prueba de tiempo del redactor (1 ms por 4 KB, `Category=Performance`) se conserva sin cambios; no hay carga nueva |
| Mutación | Sí | El redactor y la validación de redes pasan a la biblioteca, y la configuración de Stryker la incluye para no perder la meta de la spec 008 |
| CI/CD | Sí | `detect` con `SHARED` (CA-10), job de contenedor (CA-09) y gates de la spec 007 |
| Análisis estático y vulnerabilidades | Sí | jscpd y codemap en cero (CA-07), `check_exceptions.py` (CA-08), Semgrep, Gitleaks y CodeQL sin hallazgos nuevos |
| Accesibilidad | No | No hay interfaz |

## 11. Escenarios
**Positivos**
- Se corrige una regla del redactor una vez y los cuatro proyectos la tienen en el mismo PR.
- Un servicio nuevo se arma con la biblioteca sin copiar archivos.

**Negativos**
- Alguien intenta agregar a la biblioteca un servicio de un módulo (por ejemplo, el cálculo de oleadas): la regla de arquitectura o la revisión lo rechazan porque no está en el catálogo.
- Alguien copia de nuevo `TelemetryRedactor` en un servicio: la regla de piezas prohibidas falla.

**Errores esperados**
- Un `Dockerfile` sin la biblioteca no compila. Un PR que solo toca la biblioteca sin puntos lo rechaza el check de etiquetas.

**Casos límite**
- Otro PR cambia una copia del redactor o de los encabezados reenviados mientras la etapa 1 está abierta: la etapa 1 integra ese cambio en la implementación compartida y conserva sus pruebas.
- Una pieza tiene diferencias reales entre copias, por ejemplo la categoría del registro o la ruta de salud: se vuelven parámetros (R-05). Si la diferencia es de comportamiento, se pregunta al Tech Lead antes de unificar.

## 12. Definición de terminado
**Etapa 1 (KNOCK-251)**
- Enmienda de la constitución fusionada (versión 1.2.0) y esta spec Aprobada antes del primer commit de código.
- CA-01 a CA-10 con prueba o comando en verde. `python scripts/sdd_lint.py --trace 055` sin faltantes, salvo CA-11 y CA-12, que son de etapas posteriores, y los marcados «(comando)».
- EXC-020 retirada y EXC-007 acotada. `Memoria.md` con la decisión y `docs/arquitectura.md` con la biblioteca.
- Revisión del Tech Lead y sin hallazgos de seguridad abiertos.

**Etapas 2 y 3:** su criterio (CA-11 o CA-12) en verde, sus excepciones retiradas y `Memoria.md` al día.

## 13. Decisiones
- **Una biblioteca nueva y no ampliar `Knock.Contracts`:** los contratos siguen siendo «solo tipos» y la biblioteca técnica tiene sus propias reglas. Así se respeta la intención de la spec 001 (R-04).
- **Catálogo cerrado:** la biblioteca solo sirve si no acopla a los servicios; una lista explícita es más fácil de revisar que una regla general.
- **Mover sin cambiar comportamiento:** el PR de cada etapa se revisa comparando antes y después con las mismas pruebas, y un arreglo no se esconde dentro de un movimiento.
- **Por etapas:** la etapa 1 retira la excepción que vence antes por KNOCK-251 (EXC-020) y prueba el molde (proyecto, imágenes, CI). Las demás reutilizan el molde.

## 14. Decisiones tomadas sin consulta
Las tomó el agente con el criterio de la constitución. El Tech Lead puede cambiarlas antes de aprobar.

1. **Nombre `Knock.Hosting`.** Coincide con la carpeta `Hosting/` de donde salen las piezas. Alternativas: `Knock.BuildingBlocks` o `Knock.Platform`.
2. **La biblioteca no referencia `Knock.Contracts`**, aunque podría, para que cada biblioteca compartida tenga una sola razón de cambiar. Alternativa: permitir la referencia si una pieza la necesita.
3. **El gateway entra en la etapa 1 con sus encabezados reenviados.** `AddKnockGatewayForwardedHeaders` hace lo mismo que `AddKnockForwardedHeaders` (spec 035, R-09 a R-12), aunque KNOCK-251 solo nombra la copia de Mensajería. Alternativa: dejarlo para la etapa 3 con el resto de EXC-016.
4. **`LoggingExtensions` y `ObservabilityExtensions` esperan a la etapa 3**, porque KNOCK-251 solo nombra el redactor. Por eso EXC-007 se acota y no se retira en la etapa 1. Alternativa: moverlas en la etapa 1 y retirar EXC-007 completa.
5. **Puntos `-0` cuando solo se mueve código**, porque el comportamiento no cambia. Alternativa: `-1` por componente reconstruido.
6. **Las etapas 2 y 3 en esta misma spec, con tarjetas propias**, como pidió el Tech Lead («que cubra la biblioteca compartida completa, incluido luego el limitador de peticiones»). Alternativa: una spec por etapa.

## 15. Riesgos y dudas abiertas
- **EXC-015, EXC-016 y EXC-018 vencen el 2026-12-04, y EXC-020 y EXC-021 el 2026-12-15:** la etapa 3 difícilmente termina antes. El Tech Lead debe decidir si se renuevan, con la etapa como plan de salida, o si se adelantan piezas.
- **`Knock.Contracts` ya tiene lógica técnica** (`CorrelationIdMiddleware`, spec 001 R-05). Esta spec no la mueve. Queda para decidir si pasa a `Knock.Hosting` en la etapa 3 para que `Knock.Contracts` quede solo con tipos.
- **Cambiar la biblioteca reconstruye y despliega los cuatro componentes.** Es el precio de una sola implementación y se mitiga con las pruebas por proyecto y los puntos por componente.
- **Archivos protegidos (R-15):** sin el cambio de `SHARED`, un PR que solo toque la biblioteca no probaría los servicios. Debe entrar en el mismo PR de la etapa 1 o antes.
- **Mutación:** hay que confirmar que la configuración de Stryker de la spec 008 recorre `src/Knock.Hosting`. Si no, la meta baja sin que nadie lo note.

## Aprobación
- [ ] Revisor 1 — nombre: ________ fecha: ________
- [ ] Revisor 2 (enmienda a la constitución, §11) — nombre: ________ fecha: ________
- [ ] Sin conflicto con la constitución (con la enmienda 1.2.0)
- **Estados:** Borrador (agente) → Aprobada (persona; habilita el código) → Implementada (tras el merge)
