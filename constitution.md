---
version: 1.2.1
status: borrador
methodology: SDD (Spec Driven Development)
applies_to: knock-backend
---

# Constitución — Knock · Backend

> Reglas no negociables de este repositorio. Si otro documento (spec, plan, tareas, AGENTS.md o código) la contradice, **manda esta constitución**.
> Fuente: `docs/descripcion-del-proyecto.md`.

## 1. Propósito y alcance
- Knock conecta a personas con técnicos de confianza (mecánica, plomería, electricidad) de su colonia, empezando por una ciudad piloto. Este repositorio es el **backend**: los servicios que usan la app móvil y el panel web, su infraestructura en la nube y sus bases de datos. La descripción completa del proyecto está en `docs/descripcion-del-proyecto.md`.
- Se trabaja con **Spec Driven Development**: primero se escribe qué se va a construir y por qué (spec), después cómo (plan), después los pasos (tareas) y solo entonces se escribe código.
- Esta primera versión define la **base**: arquitectura, estructura del repositorio e infraestructura. No incluye funcionalidad de negocio.

## 2. Principios
1. **Spec primero:** no se escribe código sin una spec aprobada que lo justifique.
2. **Una fuente de verdad:** cada decisión vive en un solo documento; los demás la referencian.
3. **Cambios pequeños y trazables:** cada cambio parte de una spec, tiene su plan y sus tareas y termina con pruebas.
4. **Pruebas que prueban:** cada criterio de aceptación tiene una prueba que falla si la lógica está mal.
5. **Seguridad y privacidad desde el diseño:** validar en la frontera, denegar por defecto, mínimo privilegio, sin secretos en el repositorio.
6. **Simplicidad:** se elige la solución más simple que cumpla la spec; no se construye para necesidades futuras.
7. **Honestidad:** no se declara que algo pasó si no se ejecutó, y las limitaciones se informan.
8. **La IA propone, la persona decide:** los agentes redactan y verifican, pero una persona aprueba cada spec y cada cambio.
9. **Caché en todo acceso a datos:** toda lectura pasa por una capa con caché (Redis) y toda escritura invalida sus claves; lo personal que cambia al crear algo no se cachea.
10. **Respuestas rápidas:** todo listado se pagina por cursor, todo campo por el que se filtra u ordena lleva índice y las respuestas grandes se comprimen.
11. **Versionado por puntos:** cada componente (servicios, gateway, app, panel y landing) lleva su versión `X.Y.Z`, calculada con los puntos de cada cambio; la primera versión publicada es `1.0.0`.
12. **Observabilidad:** toda petición lleva `X-Correlation-Id` y los registros van a un servicio central sin datos personales.
13. **Contenido moderado:** todo texto e imagen que sube una persona pasa por la API de moderación antes de guardarse; si la API no responde, no se publica.
14. **Uso de IA declarado:** todo pull request incluye una sección «Uso de IA» con qué generó la IA y qué revisó una persona (la sección va en la plantilla del PR, no en cada spec); la IA nunca recibe datos personales sensibles.

## 3. Stack
| Capa | Tecnología |
|---|---|
| Plataforma | .NET 11 / ASP.NET Core 11 con Controllers (siempre la última versión estable; mientras .NET 11 esté en candidata a versión final se usa el RC y se pasa a la versión final cuando salga) |
| API | REST con OpenAPI 3.1, rutas versionadas (`/api/v1`), FluentValidation, ProblemDetails (RFC 9457) |
| Servicios | Tres servicios (Identidad y confianza, Mensajería y notificaciones, Mercado) y una puerta de entrada (gateway) |
| Base de datos principal | MongoDB en Azure DocumentDB (plan B: MongoDB Atlas); una base lógica por servicio |
| Datos compartidos | Redis en contenedor propio: tokens cancelados, caché, contadores del límite de peticiones y mensajes entre copias |
| Nube | Azure: Container Apps, DocumentDB, Blob Storage, Key Vault y Application Insights |
| Infraestructura como código | Terraform con estado remoto |
| Registro | Serilog con `X-Correlation-Id` en toda petición, sin datos personales |
| Pruebas | xUnit, Testcontainers y NetArchTest |
| Integración y entrega continua | GitHub Actions |

No se agregan dependencias sin evaluar necesidad, licencia, mantenimiento y vulnerabilidades, y se registran en la memoria del proyecto (`Memoria.md` y `docs/memoria/`).

## 4. Arquitectura
- **Microservicios con una puerta de entrada.** Tres servicios pequeños —Identidad y confianza, Mensajería y notificaciones y Mercado— y un gateway que recibe todo el tráfico, valida el token, limita las peticiones y reparte la petición al servicio correcto. Los servicios internos no tienen dirección pública.
- **Por qué tres servicios:** usar microservicios es un requerimiento; un solo programa obliga a desplegar todo junto y un error en una parte puede tirar las demás; ocho servicios (uno por módulo) no caben en un equipo de cuatro personas ni en el costo cero. Tres agrupan los módulos que cambian juntos.
- **Cada servicio es dueño de lo suyo:** tiene su base lógica, su imagen de contenedor, su módulo de infraestructura y su despliegue. Ningún servicio lee la base de datos de otro: se piden lo que necesitan por la interfaz pública del otro servicio.
- **Contratos compartidos sin lógica:** una biblioteca solo con tipos y contratos describe lo que los servicios se intercambian, para no acoplarlos.
- **Biblioteca técnica compartida:** `Knock.Hosting` reúne solo piezas técnicas de arranque y de la tubería HTTP que se repetían en los servicios y el gateway: encabezados reenviados, redacción y formato de registros, observabilidad, límite de peticiones, clientes de Redis y de MongoDB (estos, en `Knock.Hosting.Data`, que el gateway no usa), salud, encabezados de seguridad, respuestas de error y compresión. No lleva reglas de negocio, entidades, DTO ni colecciones de ningún módulo, y no conoce a ningún servicio ni a `Knock.Contracts`. Cada servicio la configura con sus propias opciones y se sigue construyendo y desplegando por separado. La spec 055 fija qué puede entrar y en qué orden: agregar algo fuera de esa lista requiere cambiar esa spec con las mismas dos aprobaciones que una enmienda de esta constitución (§11). Las pruebas de arquitectura verifican estas fronteras.
- **Capas dentro de cada servicio:** `Controllers` (reciben, validan y responden) → `Services` (reglas de negocio y coordinación) → `Domain` (entidades y sus reglas) → `Data` (MongoDB y Redis). Cada función se construye de punta a punta (rebanada vertical) y los módulos tienen fronteras que las pruebas de arquitectura verifican.
- **Sin estado en el proceso:** nada de negocio vive en la memoria de una copia; lo compartido vive en MongoDB o en Redis, de modo que el servicio puede correr en varias copias.
- **Eventos fiables:** un cambio importante guarda su aviso en la misma operación (colección `outbox`) y un proceso de fondo lo entrega, para que no se pierda ni se avise algo que no ocurrió.

**Estructura del repositorio**

```
src/
  Knock.Gateway/              puerta de entrada
  Knock.Identity.Api/         servicio Identidad y confianza
  Knock.Messaging.Api/        servicio Mensajería y notificaciones
  Knock.Market.Api/           servicio Mercado (incluye el módulo Asistente)
  Knock.Contracts/            solo tipos y contratos, sin lógica
  Knock.Hosting/              piezas técnicas compartidas, sin lógica de negocio (spec 055)
  Knock.Hosting.Data/         parte técnica de MongoDB de Knock.Hosting, solo para los servicios (spec 055)
  cada servicio: Modules/<Modulo>/{Controllers,Dtos,Validators,Services,Domain,Data,Events}
                 + Common/, Hosting/, Middleware/
tests/                        UnitTest, IntegrationTest, SecurityTest, ContractTest, DatabaseTest, MutationTest y PerformanceTest
db/                           scripts de índices y datos de ejemplo
deploy/                       Terraform: un módulo por servicio y uno compartido
docs/                         descripción del proyecto y decisiones
specs/                        spec.md, plan.md y task.md de cada cambio
```

## 5. Infraestructura y escalabilidad
- **Nube:** Azure con la suscripción gratuita de estudiante: Container Apps (ejecuta los servicios y el gateway y reparte las peticiones), DocumentDB (MongoDB), Blob Storage, Key Vault y Application Insights. El backend corre en una sola región de las cinco que permite la suscripción.
- **Infraestructura como código:** todo se describe con Terraform (un módulo por servicio y uno compartido, con estado remoto) y se crea y se borra con cuatro pasos manuales: crear, desplegar, eliminar lo desplegado y destruir.
- **Escalado, mínimo dos instancias:** durante la ventana de evaluación cada servicio de negocio y el gateway corren con al menos dos copias (`minReplicas = maxReplicas = 2` o más), fijadas por variable de Terraform; es escalado y no autoescalado, porque no es un requerimiento tener reglas que las suban solas.
- **Balanceador:** la entrada de Container Apps reparte las peticiones entre las copias, saca de rotación a una copia enferma (usa `/health/ready`) y termina el cifrado HTTPS.
- **Sin estado en el proceso:** tokens, caché, límite de peticiones y mensajes entre copias viven en Redis o en MongoDB, para que cualquier copia atienda cualquier petición.
- **Costo cero:** niveles gratuitos y crédito de estudiante; fuera de la ventana de evaluación el número de copias baja y el consumo se revisa cada semana.
- **Entrega del proyecto:** el proyecto se entrega con el script de la base de datos (`db/`) y la infraestructura se puede levantar y tumbar con comandos.

## 6. Código y convenciones
- Código y comentarios técnicos en inglés; specs, documentación y textos de interfaz en español.
- Warnings como errores; complejidad máxima de 10 por método o función.
- Sin `TODO` sin explicación, sin código muerto y sin secretos.
- Nombres claros: el nombre explica la intención, no la implementación.

## 7. Seguridad y privacidad
- Ningún secreto, clave, dato personal real ni documento de identidad vive en el repositorio, en los registros ni en un prompt.
- Se sigue OWASP Top 10 y OWASP API Security Top 10 como lista mínima.
- Toda entrada se valida; toda autorización se decide en el servidor.

## 8. Pruebas y calidad
- Tipos de prueba posibles: unitarias, integración, arquitectura, seguridad negativa, contrato, accesibilidad y de aceptación; cada spec declara cuáles aplican y por qué.
- Cobertura mínima definida en la spec de calidad; los umbrales no se bajan para que el CI pase.
- Un cambio está terminado cuando cumple sus criterios, sus pruebas pasan, el CI está en verde, un par lo revisó y la memoria del proyecto (`Memoria.md` y `docs/memoria/`) está al día.

## 9. Flujo de trabajo
- Ramas: `main` (listo para publicar) ← `develop` (integración) ← `<tipo>/<descripcion-en-ingles>` (una por cambio).
- Commits en inglés con Conventional Commits: `tipo(alcance): descripcion`.
- Todo cambio entra por pull request con descripción, pruebas ejecutadas y revisión de otra persona.
- Ciclo SDD de cada cambio: **spec → plan → tareas → implementación → verificación → memoria**.
- Cada PR declara los puntos que aporta a cada componente que toca (`puntos-<componente>-N`); al desplegar se suman, se calcula la versión nueva y se crea una etiqueta de Git y una imagen con ese número. Un despliegue con cero puntos no cambia la versión.
- La plantilla de PR tiene las secciones Descripción, Pruebas, Seguridad, Riesgos y «Uso de IA».

## 10. Agentes de IA
- Los agentes leen `AGENTS.md`, esta constitución y la memoria del proyecto (`Memoria.md` y `docs/memoria/`) antes de actuar.
- Un agente nunca aprueba una spec, nunca fusiona y nunca opera sobre producción.
- Prohibido: omitir pruebas, desactivar reglas o escáneres, exponer secretos, afirmar ejecuciones que no se hicieron y agregar dependencias sin evaluar.

## 11. Gobernanza
- Estados de una spec: **Borrador** (la escribe un agente o una persona) → **Aprobada** (una persona; habilita el código) → **Implementada** (tras el merge).
- Cambios a esta constitución: pull request con la aprobación de al menos dos integrantes, `version` incrementada y registro en la memoria del proyecto (`Memoria.md` y `docs/memoria/`).
