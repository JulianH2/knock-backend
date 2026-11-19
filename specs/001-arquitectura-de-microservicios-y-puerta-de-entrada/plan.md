# Plan 001 — Arquitectura de microservicios y puerta de entrada

> Parte de `spec.md` (estado Aprobada). Este plan se confirmó con la aprobación de la spec.

## Enfoque
1. Crear la solución con los cuatro proyectos y la biblioteca de contratos.
2. Fijar la versión del SDK y la configuración común de compilación.
3. Implementar el gateway mínimo con enrutamiento y reenvío del identificador de correlación.
4. Agregar a cada servicio el esqueleto de arranque con `/health`.
5. Escribir las pruebas de arquitectura de frontera entre servicios.

## Archivos que se crean o cambian
- `Knock.slnx`, `global.json` y `Directory.Build.props`
- `src/Knock.Gateway/`, `src/Knock.Identity.Api/`, `src/Knock.Messaging.Api/`, `src/Knock.Market.Api/`, `src/Knock.Contracts/`
- `tests/ArchitectureTest/`
- `docs/arquitectura.md`

## Dependencias
- SDK de .NET 11 y ASP.NET Core 11
- Proxy inverso del gateway (YARP)
- NetArchTest

## Decisiones de la spec que este plan sigue
- **Tres servicios y no un solo programa:** usar microservicios es un requerimiento y un solo programa obliga a desplegar todo junto; un fallo en el chat no debe tirar las cotizaciones.
- **Tres servicios y no ocho:** ocho significan ocho pipelines y diez o más contenedores, lo que no cabe en un equipo de cuatro personas ni en el costo cero.
- **Una sola puerta de entrada:** cada servicio expuesto es una puerta más que defender; con una sola entrada, la seguridad, el límite de peticiones y el registro se aplican en un único punto.
- **Plan de emergencia:** si el tiempo se acaba, Mensajería y Mercado pueden unirse y quedarían dos servicios.

## Verificación
- Cada criterio de aceptación de `spec.md` se comprueba con una prueba o un comando antes de dar el cambio por terminado.
- Se ejecutan los comandos de `AGENTS.md` y se actualiza `Memoria.md`.
