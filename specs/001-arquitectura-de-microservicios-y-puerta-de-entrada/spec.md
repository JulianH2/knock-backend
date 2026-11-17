# Spec 001 — Arquitectura de microservicios y puerta de entrada

> **Estado:** Aprobada · **Repositorio:** knock-backend · **Rector:** `constitution.md`

## 1. Objetivo
Definir el tipo de arquitectura del backend: qué servicios existen, qué responsabilidad tiene cada uno, cómo entra el tráfico y cómo se relacionan entre sí.

## 2. Alcance
**Dentro**
- Los tres servicios (Identidad y confianza, Mensajería y notificaciones, Mercado) y su responsabilidad.
- La puerta de entrada (gateway): validación del token, límite de peticiones, enrutamiento y registro.
- Reglas de convivencia entre servicios y biblioteca de contratos.
- Identificador de correlación en toda petición.
- Esqueleto vacío de los cuatro proyectos que compila y arranca.

**Fuera**
- Funcionalidad de negocio de los servicios.
- Autenticación concreta y reglas de seguridad (spec 009).
- Infraestructura en la nube (spec 005).

## 3. Requisitos
- **R-01:** Existen cuatro proyectos desplegables: gateway, Identidad y confianza, Mensajería y notificaciones y Mercado, más dos bibliotecas compartidas que no se despliegan: la de contratos (`Knock.Contracts`) y la técnica (`Knock.Hosting`, spec 055).
- **R-02:** Los servicios internos no tienen dirección pública; solo el gateway recibe tráfico externo.
- **R-03:** Ningún servicio accede a la base de datos de otro; se comunican por su interfaz pública (REST) o por eventos.
- **R-04:** Ninguna biblioteca compartida tiene lógica de negocio. La de contratos solo contiene tipos y contratos. La técnica solo contiene las piezas de arranque y de la tubería HTTP del catálogo cerrado de la spec 055 (constitución 1.2.0, §4).
- **R-05:** Todo servicio genera o reenvía `X-Correlation-Id` y lo escribe en sus registros.
- **R-06:** Cada servicio se construye, prueba y despliega por separado.
- **R-07:** El módulo Asistente vive dentro del servicio Mercado.

## 4. Criterios de aceptación
- **CA-01:** Dado el repositorio recién clonado, cuando se ejecuta `dotnet build` y `dotnet test`, entonces los cuatro proyectos compilan y las pruebas pasan.
- **CA-02:** Dada una petición externa, cuando llega al gateway, entonces se enruta al servicio correcto y lleva `X-Correlation-Id`.
- **CA-03:** Dada una prueba de arquitectura, cuando un servicio referencia la capa de datos de otro, entonces la prueba falla.
- **CA-04:** Dado un servicio interno, cuando se intenta llamarlo desde fuera de la red, entonces no es accesible.

## 5. Contrato
No expone endpoints de negocio. Interfaces que fija: el gateway como única entrada pública, la biblioteca `Knock.Contracts` como único lugar de los acuerdos entre servicios y el encabezado `X-Correlation-Id`.

## 6. Amenazas y controles
| Amenaza | Control |
|---|---|
| Un servicio interno queda expuesto a internet | Ingreso interno en Container Apps; solo el gateway tiene dirección pública |
| Un servicio lee la base de otro y rompe su propiedad de los datos | Prueba de arquitectura que prohíbe referencias entre servicios y credenciales de base distintas por servicio |
| El gateway deja pasar tráfico sin identificar a la persona | Validación del token y límite de peticiones en el gateway antes de enrutar (spec 009) |

## 7. Plan de pruebas
| Tipo | ¿Aplica? | Motivo |
|---|---|---|
| Unitarias | Sí | Middleware de correlación y enrutamiento |
| Integración | Sí | Gateway y un servicio levantados juntos |
| Arquitectura | Sí | Fronteras entre servicios y entre módulos |
| Seguridad negativa | Sí | Llamar a un servicio interno desde fuera de la red |
| Contrato | No | No hay contrato de negocio todavía |
| Humo / extremo a extremo | Sí | Arranque de los cuatro proyectos y petición por el gateway |

## 8. Decisiones
- **Tres servicios y no un solo programa:** usar microservicios es un requerimiento y un solo programa obliga a desplegar todo junto; un fallo en el chat no debe tirar las cotizaciones.
- **Tres servicios y no ocho:** ocho significan ocho pipelines y diez o más contenedores, lo que no cabe en un equipo de cuatro personas ni en el costo cero.
- **Una sola puerta de entrada:** cada servicio expuesto es una puerta más que defender; con una sola entrada, la seguridad, el límite de peticiones y el registro se aplican en un único punto.
- **Biblioteca técnica además de la de contratos (2026-11-16, KNOCK-251):** las piezas técnicas copiadas entre los servicios y el gateway se mueven a `Knock.Hosting` para corregirlas una sola vez. Los contratos siguen siendo solo tipos; la spec 055 fija el catálogo y las pruebas de frontera.
- **Plan de emergencia:** si el tiempo se acaba, Mensajería y Mercado pueden unirse y quedarían dos servicios.

## 9. Riesgos y dudas abiertas
- Ninguna duda abierta por ahora; se agregan aquí las que surjan al revisar la spec.

## Aprobación
- [x] Revisor 1 — nombre: José Julián Hernández Hernández fecha: 2026-11-04
- [x] Sin conflicto con la constitución
- **Estados:** Borrador → Aprobada (una persona; habilita el código) → Implementada (tras el merge)
