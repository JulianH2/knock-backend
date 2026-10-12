# Knock — Decisiones abiertas: análisis, implicaciones y recomendación

Este documento convierte las **10 decisiones abiertas** de [`FUNCIONES_Y_FLUJOS.md`](FUNCIONES_Y_FLUJOS.md) en propuestas concretas. Para cada una: contexto, opciones con sus implicaciones, una **recomendación** y **qué falta validar con una persona experta**.

> **Aviso importante**
> - Las **recomendaciones** de cada punto son el análisis original; las **decisiones tomadas** están en la sección «Decisiones cerradas».
> - Lo **legal y fiscal** es solo orientación para saber qué preguntar. **Hace falta un abogado y un contador** (ver [punto 10](#10-cumplimiento-y-textos-legales)).
> - Los datos externos vienen de **búsquedas web de octubre de 2026**, casi todas de fuentes secundarias (despachos, blogs, foros). Cada dato importante está marcado con **«verificar»** y trae su enlace en [Fuentes](#fuentes). No se incluyen precios de proveedores: hay que **cotizar**.

## ✅ Decisiones cerradas (octubre 2026)

> **Esta sección manda.** El análisis de los puntos 0 a 10 que viene después es el estudio original; **donde difiere de lo de aquí, vale lo de aquí**. La numeración de esta tabla (1 a 24) es la del equipo y sirve para rastrear cada decisión hacia las historias de usuario (HU).

### Producto

| # | Tema | Decisión |
|---|---|---|
| 1 | **Oficios** | **Tres para el MVP** (mecánica, plomería, electricidad), guardados **como datos en una tabla** y no fijos en el código. «Otro oficio» se guarda como una **lista de oficios pedidos**. Gestionar categorías: **Media**. |
| 2 | **Inicio de sesión** | Solo **celular y código SMS, sin contraseña**; registro e inicio son el mismo flujo. **No hay HU de contraseña ni de recuperarla.** Se agrega la HU **«Cambiar de número y recuperar el acceso»** (**Media**): se verifica el número nuevo con un código y se avisa al anterior; si es **técnico verificado**, repite la selfie y se compara con la que ya validó; si es **cliente**, soporte lo revisa con datos de sus servicios. |
| 3 | **Modos** | **Una sola cuenta.** El técnico gana la pestaña **Trabajo** y la sección **Mi negocio**. |
| 4 | **Verificación** | **Opcional, con beneficios:** insignia, filtro «solo verificados» y una ventaja leve en el orden, explicada al usuario. **En el MVP la revisa una persona del equipo** con INE (frente y reverso) y selfie; un proveedor automático (KYC) queda para la versión real. **Las fotos del INE se guardan 30 días, cifradas, con acceso restringido y registro de quién las ve.** Ya se cambió el texto de la maqueta; faltan el aviso de privacidad y el consentimiento definitivos (**riesgo legal**). |
| 5 | **Chat** | Chat dentro de la app **antes de elegir**, con el número oculto; **después de elegir se abre WhatsApp**. Prioridad **Alta**. |
| 6 | **A quién llega cada solicitud** | **Por oleadas:** hasta **5 técnicos** con «Disponible hoy» activado (3 mejor ordenados, 1 nuevo y 1 cercano). Si en **15 min** hay menos de 2 cotizaciones, se amplía. El cliente ve **máximo 5**. La solicitud **vence a las 48 h**. |
| 7 | **Fecha de la visita** | La solicitud lleva el **día y la franja** preferidos, la cotización propone **día y hora** y, al elegirla, **la cita queda confirmada**. HU pequeña para **reprogramar** (Media). |
| 8 | **Terminar el servicio** | El técnico termina con **fotos obligatorias** y la descripción de lo que hizo; el cliente califica. **Se quitan** la confirmación del cliente y la autoconfirmación a las 72 h. El cliente puede **reportar «no se terminó» hasta 7 días después**. |
| 9 | **Reseñas** | Calificación **general de 1 a 5 (obligatoria)** y, opcionalmente, **Puntualidad, Calidad, Precio justo y Trato**, cada una de 1 a 5. Lleva comentario, **hasta 3 fotos**, respuesta del técnico y opción de guardar al técnico como «de confianza»; sale como «Servicio verificado». Se puede reseñar **hasta 30 días después**. **Niveles** por trabajos terminados: Nuevo 0 · Confiable 1 · Experto Knock 5 · Maestro Knock 15. |
| 10 | **Reportes** | **Por gravedad:** los críticos suspenden de forma preventiva; los altos ocultan al técnico de nuevas solicitudes; los medios suman advertencias; los bajos van a mediación. **Apelación: Media.** |
| 11 | **Eliminar la cuenta** | Se muestra lo pendiente y se ofrece **«Cancelar lo pendiente y eliminar»** con **confirmación por SMS** y **7 días de gracia**. Las reseñas se **anonimizan**. |
| 12 | **Ciudad** | **Pachuca, colonia por colonia**, con **lista de espera** fuera de cobertura. |
| 13 | **Funciones de la maqueta** | **Alta:** entrar sin cuenta, mapa de inicio, Explorar, avisar a alguien de confianza, «Disponible hoy» y fotos al terminar. **Media:** «Mi casa y mi auto», la capa «Mi agenda» y las ideas de Explorar. |
| 14 | **Idioma y tema** | **Textos en archivos de traducción desde el día uno** (ya hecho en la maqueta) y tema claro/oscuro (**Alta**). La traducción completa al inglés queda en **Media**. |

### IA y aprendizaje automático

| # | Función | Prioridad |
|---|---|---|
| 15 | **IA generativa:** asistente Knock (texto y foto), resumen del chat y bitácora del técnico, cotización con IA | **Alta** |
| 16 | **ML (ML.NET):** orden «Recomendadas» por cumplimiento y rango de precio de referencia (en Comparar y en el borrador de cotización) | **Alta** |
| 17 | Detección de anomalías, explicar la cotización, resumen de reseñas, constancia del servicio | **Media** |

### Técnico

| # | Tema | Decisión |
|---|---|---|
| 18 | **Plataforma** | **Flutter + ASP.NET Core + GraphQL + PostgreSQL/PostGIS.** **No se hace** la prueba técnica propuesta en el punto 8: Flutter encaja con la materia de Aplicaciones Móviles y tiene paquetes de mapas, cámara y notificaciones. La maqueta queda como **especificación visual** (no se reutiliza su código). |
| 19 | **Panel interno** | **Flutter web**, para usar un solo lenguaje. |
| 20 | **Chat en tiempo real** | **Suscripciones de GraphQL**, con un servicio que reparta los mensajes entre instancias del servidor. Si la otra persona no está conectada, recibe una **notificación**. |
| 21 | **Código SMS** | **Firebase Phone Auth** con **números de prueba** para desarrollo y demo. El servidor valida el token de Firebase y emite **sus propios tokens de sesión**. Firebase también envía las notificaciones (**FCM**). ⚠️ **Pendiente: confirmar límites y costo por SMS** antes de la demo. |
| 22 | **Mapa y zona** | **Google Maps para Flutter.** La colonia se calcula con **nuestra propia tabla de colonias de Pachuca** (catálogo SEPOMEX + PostGIS), sin pagar geocodificación. ⚠️ **Pendiente: confirmar el precio actual de Google.** |
| 23 | **Nube e IA** | **Azure:** Azure OpenAI y almacenamiento de archivos privados con **enlaces temporales**. |
| 24 | **Negocio y legal** | Va **solo en la propuesta, sin HU:** gratis en el piloto, suscripción y destacados después, **sin pagos dentro de la app**. Los textos legales son de demostración y la **revisión con abogado queda anotada como riesgo**. |

### En qué difiere de las recomendaciones originales

| Tema | Recomendación original | Decisión cerrada | Qué implica |
|---|---|---|---|
| Oficios | 5 a 8 presenciales en la fase 1 | **3 en el MVP**, como datos | Las demás categorías se abren después con la tabla ya lista |
| Verificación | Proveedor KYC; **no guardar las fotos** | **Revisión manual del equipo; fotos 30 días cifradas** | Se guardan **datos biométricos**: hace falta **cifrado, accesos con bitácora, borrado automático a los 30 días** (HU del panel), **consentimiento de conservación** y revisión legal. También **capacidad de revisión**: alguien debe cubrir la cola (la maqueta promete respuesta en menos de 24 h) |
| Plataforma | Prueba técnica entre dos candidatos | **Flutter directo** | La maqueta no se reutiliza como código; hay que confirmar que los **paquetes de Flutter** cubren cámara, mapas, push y la captura de verificación |
| Solicitudes | Vigencia de 24–48 h | **48 h** | Hay que programar el vencimiento y avisar al cliente |
| Terminar el trabajo | Opcionalmente confirmación del cliente | **Sin confirmación**; el cliente puede reportar «no se terminó» 7 días | Más peso en el sistema de **reportes y disputas** |
| Idiomas | Preparar la arquitectura | **Ya preparado** (`js/i18n/`) | En Flutter se replican los archivos de traducción desde el inicio |
| SMS, mapa y nube | Lista de opciones | **Firebase, Google Maps + tabla propia, Azure** | Dos costos por confirmar (SMS y Google) |

### Pendientes que siguen abiertos
1. **Costo y límites de Firebase Phone Auth** (SMS) antes de la demo.
2. **Precio actual de Google Maps** para Flutter.
3. **Revisión legal** del aviso de privacidad y del consentimiento de verificación, sobre todo la **conservación de 30 días de datos biométricos**.
4. **Quién revisa las verificaciones** y en qué horario (capacidad del equipo).
5. **Política de retención** completa y el **borrado automático** de las fotos.
6. En Flutter: confirmar el paquete/SDK para **captura de INE y selfie** y para el **mapa con pines y capas**.

---

## Resumen del análisis original

> Estado de cada punto del estudio que sigue. Las decisiones finales están en la sección anterior.

> La numeración sigue el orden de este documento. Equivale a la lista original así: **0** = «nuevas categorías», **1 y 9** = modelo de negocio y pagos, **2** = cobertura, **3** = verificación, **4** = técnicos por solicitud, **5** = reportes, **6** = cuentas con trabajos activos, **7** = idiomas, **8** = plataforma, **10** = cumplimiento y textos legales.

| # | Decisión | Propuesta | Estado |
|---|---|---|---|
| 0 | ¿Solo 3 oficios? | **No.** Solo es la maqueta. Catálogo administrable por categorías, empezando con oficios del hogar y el auto y dejando lo remoto para una fase 2 | ✅ Cerrada (ver arriba) |
| 1 | Modelo de negocio | Todo gratis para el cliente. Para el técnico, empezar gratis y luego **suscripción + destacados**; pagos con comisión solo cuando haya confianza y densidad | ✅ Cerrada (ver arriba) |
| 2 | Cobertura | **Empezar en una ciudad, zona por zona** (densidad antes que amplitud) | ✅ Cerrada |
| 3 | Verificación | Opcional, pero con **beneficios reales** (insignia, filtro, garantía, prioridad leve) | ✅ Cerrada (ver arriba) |
| 4 | Técnicos por solicitud | **Híbrido por oleadas** (3–5 al inicio y ampliar si no responden) | ✅ Cerrada (ver arriba) |
| 5 | Reportes y suspensión | Política **por gravedad** con suspensión preventiva solo en casos de seguridad | ✅ Cerrada (ver arriba) |
| 6 | Eliminar con trabajos activos | **Cancelar y avisar**, con eliminación programada y plazo de gracia | ✅ Cerrada (ver arriba) |
| 7 | Idiomas | **Español + inglés, con arquitectura abierta** | ✅ Cerrada y ya preparada |
| 8 | Plataforma | Multiplataforma obligatoria → decidir con una **prueba técnica corta** entre 2 candidatos | ✅ Cerrada / ⚠️ pendiente de asesoría |
| 9 | Pagos en la plataforma | Dentro de la decisión 1: empezar **sin pagos** | — |
| 10 | Legal y cumplimiento | **Abogado de datos personales + contador** antes de publicar | ✅ Cerrada / ⚠️ pendiente de asesoría |

---

## 0. ¿Los oficios están limitados a tres?

**Respuesta corta: solo en la maqueta.** Mecánica, plomería y electricidad son las categorías de ejemplo que trae la demostración. El producto real debe tener un **catálogo administrable** (sin programar cada oficio) y pensar desde ya en oficios como **pintor, herrero, carpintero, cerrajero, jardinero, técnico en sistemas** o **programador**.

### Lo que cambia según el tipo de servicio

El flujo actual (visita a domicilio con código de 4 dígitos, dirección, «cómo llegar») está pensado para servicios **presenciales**. Otros oficios rompen supuestos distintos:

| Modalidad | Ejemplos | Qué cambia |
|---|---|---|
| **A domicilio** | Plomero, electricista, pintor, cerrajero, **técnico en sistemas** (instalar redes, reparar equipos) | Es el flujo actual. El riesgo principal es **entrar a una casa**: pide verificación fuerte. |
| **En taller o local** | Mecánico, reparación de electrónicos, herrería | Cita en el local; sin dirección del cliente. |
| **Remoto** | **Programador**, diseño, soporte remoto, tutorías | **No hay visita, ni dirección ni código de llegada.** Se necesita entrega por hitos, archivos, tiempos y casi siempre **pagos en garantía (escrow)**, porque no hay un «momento de la visita» que valide el trabajo. |
| **Por proyecto o por visita** | Pintura de una casa vs. revisión de 1 hora | La cotización pasa de «precio de la visita» a **presupuesto por partidas, por m² o por hitos**. |

### Qué debe poder configurarse por categoría

Una tabla de **categorías** (con jerarquía: *Hogar → Pintura*), cada una con:

| Atributo | Para qué |
|---|---|
| Modalidad (domicilio / taller / remoto) | Decide qué pasos se muestran (dirección, código de visita, entregables) |
| Plantilla de cotización | Por visita, por hora, por proyecto/hitos |
| Campos y fotos requeridas | Ej. pintura pide medidas y fotos de paredes; remoto pide archivos o enlaces |
| Precios de referencia | Rangos por servicio para el técnico y para Explorar |
| Verificación mínima | Ej. visitas a domicilio exigen verificación; remoto otra |
| Nivel de riesgo y acreditaciones | Algunos oficios regulados pueden exigir **certificaciones o licencias** (*verificar con el asesor legal qué oficios requieren acreditación*) |
| Zonas donde está activa | Abrir categorías por ciudad |

### Recomendación ☐

1. **Fase 1:** 5 a 8 oficios **presenciales** del hogar y el auto (los 3 actuales + pintura, herrería/cerrajería, carpintería, jardinería, **técnico en sistemas a domicilio**). Son el mismo flujo con pequeños cambios.
2. **Fase 2:** servicios **remotos** (programador, diseño…). Es otro producto: requiere pagos en garantía, entregables y disputas, y compite con plataformas de freelancers. No conviene mezclarlo en el lanzamiento.
3. En la maqueta, el botón «Otro oficio» ya muestra la idea: queda en el perfil pero **no recibe solicitudes** hasta que exista la categoría. En el producto real, ese texto libre debería alimentar una **lista de oficios solicitados** para decidir qué categoría abrir después.

---

## 1. Modelo de negocio

### Principios propuestos
- **El cliente nunca paga por pedir, comparar o chatear.** Es lo que atrae demanda.
- **El técnico no paga por registrarse ni por existir.** Se cobra donde ya le generamos valor medible.
- Nada que **degrade la confianza** (por ejemplo, ordenar por quien más paga sin decirlo).

### De dónde puede venir el dinero

| Fuente | Cómo funciona | A favor | En contra / riesgos |
|---|---|---|---|
| **Comisión por trabajo** | % del valor del servicio cuando se cierra | Ingreso proporcional al valor; los técnicos solo pagan si ganan | **Exige cobrar dentro de la plataforma**; si el pago es en efectivo o directo, las partes se salen de Knock («fuga») y no se cobra |
| **Pago por contacto o cotización** (*lead fee*, créditos) | El técnico paga por responder a una solicitud | Ingreso aunque el cliente no cierre; funciona **sin pagos en plataforma** | Los técnicos pagan por solicitudes que no cierran → frustración; hay que **garantizar la calidad** de cada solicitud |
| **Suscripción del técnico** | Plan mensual con herramientas (agenda, cotizador con IA, visibilidad extra, estadísticas) | Ingreso **predecible**; no depende de cerrar cada trabajo | Hace falta valor claro para que paguen; plan gratis bien delimitado |
| **Destacados / publicidad** | Mejor posición en el mapa o en Explorar, claramente etiquetado *«Patrocinado»* | Fácil de vender | Si no se etiqueta o se abusa, **daña la confianza** |
| **Membresía del cliente** | Garantía, soporte prioritario, descuentos | Ingreso del lado demanda | Solo tiene sentido con una **garantía real** detrás |
| **Garantía de servicio / seguro** | Protección por daños o incumplimiento | Diferenciador fuerte | Requiere aseguradora y reglas claras; se cobra como comisión o prima |
| **Afiliación de materiales** | Referidos a ferreterías, refaccionarias o talleres («llevo los materiales») | Casi no requiere cambiar el flujo | Ingreso pequeño al inicio |
| **B2B** | Plan para administradoras de condominios, inmobiliarias y flotillas | Contratos recurrentes y volumen | Ciclo de venta más largo; requiere factura, SLA |

**Qué hacen otros (referencia, fuentes secundarias → verificar):** Thumbtack ha cobrado sobre todo **por contacto (*pay-per-lead*)** a los profesionales, sin comisión por el trabajo, y añadió suscripciones y una membresía para clientes; TaskRabbit se describe como plataforma que cobra principalmente por **transacción**; Angi se asocia a **membresías**. No son cifras comparables a México.

### Costos que hay que cubrir (no solo gastos de «tener la app»)

| Costo | Cómo escala |
|---|---|
| SMS/OTP | Por registro y por reintento |
| **Verificación de identidad (KYC)** | **Por verificación** (cotizar con cada proveedor) |
| Mapas y geocodificación | Por consulta/carga |
| IA (resumen y cotización) | Por uso |
| Almacenamiento y moderación de fotos | Por GB y por imagen |
| Chat, push, correo | Por mensaje/usuario activo |
| Procesamiento de pagos (si hay) | % + cuota fija por transacción |
| Soporte y moderación | **El mayor costo humano**: reportes, disputas, verificaciones manuales |
| Adquisición de técnicos (oferta) y de clientes (demanda) | Marketing local, alianzas |

### Hoja de cálculo mínima para decidir (rellenar con datos reales)

| Variable | Qué medir |
|---|---|
| Solicitudes por semana | Demanda real en la zona piloto |
| % solicitudes que se convierten en trabajo | Conversión |
| Valor medio de un trabajo | Para estimar comisión |
| Costo por solicitud (SMS + IA + soporte + KYC prorrateado) | Costo variable |
| Técnicos activos y % que pagaría suscripción | Disposición a pagar (entrevistas) |
| Costo de adquirir un técnico y un cliente | CAC por lado |

**Regla simple:** si `ingreso por solicitud − costo por solicitud < 0`, el modelo solo se sostiene con inversión; hay que saber **cuántos meses** y **qué hito** lo cambia.

### Pagos dentro de la plataforma (decisión clave dentro de esta)

- **Sin pagos en la app** (hoy): solo sirven *leads*, suscripción, destacados y afiliación. Es lo más simple y encaja con la cultura de efectivo.
- **Con pagos en la app:** habilita **comisión, garantía y disputas**, pero trae **obligaciones fiscales y de cumplimiento** (retenciones, comprobantes, quién es el vendedor). Según fuentes secundarias, el **régimen de plataformas digitales del SAT** cambió para 2026: se reportan retenciones de **ISR** más altas (hay fuentes que dicen 2.5 %, otras 1 % → **verificar**) y retención de **IVA**, además de **retener más si el prestador no da su RFC**. **Hace falta un contador** antes de decidir.
- **Reforma laboral de plataformas (DOF, 24-dic-2024):** según las fuentes, hoy cubre **transporte de pasajeros y reparto**, no servicios del hogar; conviene **vigilar su alcance** y cuidar que los técnicos sean claramente **independientes**.

### Recomendación por fases ☐

| Fase | Qué se cobra | Meta |
|---|---|---|
| **1. Lanzamiento (piloto)** | **Nada**: validar que hay demanda y que cierran trabajos | Densidad de oferta y demanda |
| **2. Monetización ligera** | **Suscripción opcional del técnico** (herramientas + visibilidad) y **destacados etiquetados** | Ingreso recurrente sin tocar la confianza |
| **3. Con confianza ya ganada** | **Pagos en la app con comisión y garantía** (solo si fiscal y operativamente se resuelve) | Ingreso proporcional al valor y diferenciador |
| **En paralelo** | Afiliación de materiales y piloto B2B | Diversificar |

**Por qué:** en un mercado donde mucho se paga en efectivo, la comisión sin pagos en plataforma casi no se puede cobrar; la suscripción es predecible, y los pagos en app solo se justifican cuando la garantía aporta algo que la gente valore.

**Validar con expertos:** contador (régimen de plataformas, retenciones, CFDI), abogado (relación con técnicos independientes, términos), y **entrevistas con 15–20 técnicos** sobre cuánto pagarían y por qué.

---

## 2. Cobertura: empezar en un solo lugar

**Sí: empezar en una zona es lo ideal** para un marketplace. El valor depende de que **cada solicitud reciba rápido varias cotizaciones**; con técnicos repartidos en muchas ciudades eso no ocurre en ninguna.

### Por qué una zona
- **Densidad:** más técnicos cerca ⇒ cotizaciones más rápidas ⇒ clientes satisfechos ⇒ más demanda.
- **Operación:** soporte, verificación, moderación y marketing se pueden hacer con un equipo pequeño.
- **Aprendizaje:** se ajustan precios, reglas y producto con datos reales antes de replicar.

### Cómo hacerlo ☐
1. **Ciudad piloto** (la maqueta usa Pachuca) y, dentro, **colonia por colonia**: abrir demanda solo donde ya hay técnicos.
2. **Primero la oferta:** reclutar técnicos antes de abrir a clientes (alianzas con talleres, ferreterías y refaccionarias).
3. **Lista de espera** para quien esté fuera de la zona (la maqueta ya avisa «solo Pachuca y alrededores»; en real se guarda su correo/celular).
4. **Criterios para abrir otra zona (hipótesis a calibrar):** tiempo medio a la primera cotización, cotizaciones promedio por solicitud (≥ 3), porcentaje de solicitudes con al menos una cotización, repetición de clientes y calificación media.

### En la app
- Tabla de **zonas activas** (catálogo oficial de códigos postales y colonias, **SEPOMEX / Correos de México**), con categorías activas por zona.
- Mensaje y captura de **lista de espera** fuera de cobertura.
- Los pines, el panel y las solicitudes ya respetan la colonia; falta el **polígono/radio real**.

---

## 3. Verificación: confianza y beneficios

**Objetivo (como lo planteas):** que la persona **sepa que es una persona real y con quién va a trabajar**. Se mantiene **opcional**, pero la verificación debe **dar algo a cambio** para que valga la pena.

### Niveles (ya en la maqueta)
- **Básica:** INE por el frente + selfie (la cara coincide con el documento).
- **Completa:** + reverso (se validan los datos con el INE). *Los proveedores de verificación ofrecen validar la credencial contra la **lista nominal** (vigencia, si fue reportada como robada…), pero dependen de integraciones propias y cada uno pide campos distintos (CIC, OCR, clave de elector…) → **verificar** qué incluye cada uno.*

### Beneficios propuestos para quien se verifica ☐

| Beneficio | Cómo funciona | Riesgo a cuidar |
|---|---|---|
| **Insignia verde** | Ya existe | — |
| **Filtro «solo verificados»** | El cliente puede filtrar | Que no se vuelva obligatorio de hecho |
| **Mejor posición (leve y transparente)** | A igualdad de calificación, el verificado aparece antes | **No** hundir a los no verificados; explicarlo en los términos |
| **Garantía Knock / soporte prioritario** | La garantía o el soporte rápido solo aplican a trabajos de técnicos verificados | Debe ser una garantía real |
| **Visitas a domicilio** | Que verificarse sea requisito para **entrar a domicilios** (el riesgo más alto), y opcional para taller/remoto | Es la decisión más fuerte; el equipo debe aceptarla |
| **Más capacidad** | Límite mayor de cotizaciones activas o de zonas | Evitar abuso |
| **Comisión o suscripción reducida** | Incentivo económico (cuando exista) | Costear la verificación |

### Quién conoce a quién (también del lado del cliente)
Un técnico también quiere saber con quién va a tratar. Propuesta: **celular verificado** (ya lo hace el OTP) para todos y una **verificación opcional del cliente** para trabajos de alto valor, mostrada solo al técnico que fue elegido.

### Política recomendada ☐
- **Opcional al registrarse**, **requerida para visitas a domicilio** (a decidir) y **condición de la garantía**.
- **Re-verificación** cuando venza la credencial.
- Reglas antifraude: **nombre del INE = nombre del registro**; **una sola cuenta por CURP**.
- **Quién paga el costo de verificar:** absorberlo al inicio (es parte de adquirir técnicos), con **límite de reintentos**.

**Validar con:** proveedor de verificación (qué valida, tasa de aprobación, retención de imágenes) y abogado (datos biométricos, consentimiento y retención; ver punto 10).

---

## 4. ¿Cuántos técnicos reciben cada solicitud?

Es una decisión de **equilibrio entre rapidez, calidad, justicia y ruido**. Opciones:

| Opción | Cómo funciona | A favor | En contra |
|---|---|---|---|
| **A. A todos los de la zona** | Se avisa a todos los técnicos de la categoría y zona | Más cotizaciones, más rápido | **Spam** para técnicos (tasa de cierre muy baja → se desmotivan), saturación al cliente, **más exposición de datos** del cliente, más costo de notificaciones |
| **B. Un tope fijo (N = 3–5)** | Solo los mejor ordenados | Calidad y menos ruido | Los **nuevos nunca reciben** (arranque en frío), riesgo de favoritismo, si no responden no hay cotizaciones |
| **C. Elige el cliente** | «Pedir a este experto» (+ otros) | Máximo control y confianza | Más fricción; no sirve si no conoce a nadie |
| **D. Los primeros en responder** | Se cierra al llegar N cotizaciones | Incentiva la rapidez | Calidad desigual; los técnicos viven pegados al celular |
| **E. Híbrido por oleadas** | Oleada 1 con N técnicos; si no hay suficientes cotizaciones en un tiempo, se **amplía** a más | Equilibra todo | Más reglas que construir y calibrar |

### Parámetros que hay que definir
- **N inicial** y **N máximo de cotizaciones que ve el cliente** (muchas abruman).
- **Ventana de respuesta** (la maqueta sugiere 15 min como buena práctica) y **vigencia** de la solicitud (24–48 h).
- **Criterios de selección:** cercanía/radio, **disponible hoy**, categoría, calificación, verificación, tasa de respuesta, **carga actual** (tope de cotizaciones activas por técnico).
- **Cupo para nuevos técnicos** (para que no queden fuera para siempre).
- **Penalización por no responder o por no presentarse** (reduce prioridad).
- Qué datos ve el técnico antes de cotizar (hoy: solo la colonia).

### Recomendación ☐
**Híbrido por oleadas (E):**
1. **Oleada 1:** hasta **5 técnicos**: 3 mejor posicionados + 1 nuevo (cupo de arranque) + 1 más cercano.
2. **Oleada 2:** si a los **15 min** hay menos de 2 cotizaciones, se amplía a los siguientes.
3. El cliente ve **máximo 5** cotizaciones ordenables.
4. Siempre disponible **«Pedir a este experto»** desde su perfil.
5. Registrar tiempo a la primera cotización, cotizaciones por solicitud, tasa de cierre por técnico y quejas de spam, y **probar variantes** (N=3 vs 5, ventanas de tiempo).

*La maqueta hoy envía a 3 técnicos con respuestas escalonadas; es solo una simulación.*

---

## 5. Política de reportes y suspensión

El equilibrio: **proteger rápido a las personas** sin permitir que un reporte falso destruya a un técnico.

### Principios ☐
- **Gravedad primero:** la reacción depende del tipo de caso.
- **Suspensión preventiva solo en casos de seguridad** (no por cualquier queja).
- **Debido proceso:** razón comunicada, evidencia revisada y **una apelación**.
- **Trazabilidad:** todo queda registrado (chat, hora del código de visita, fotos).
- **Sin represalias** y **sanción por reportes falsos**.

### Matriz propuesta (a calibrar)

| Gravedad | Ejemplos | Acción inmediata | Plazo de revisión |
|---|---|---|---|
| **Crítica (seguridad)** | Violencia, acoso, robo, suplantación de identidad, daños graves | **Suspensión preventiva** + escalamiento; contactar a las autoridades si procede | Mismo día |
| **Alta** | Fraude, cobro distinto a lo acordado, pedir pago por adelantado, otra persona llegó | **Se oculta de nuevas solicitudes** hasta revisar | 24 h |
| **Media** | No se presentó, trabajo mal hecho, mala comunicación | **Advertencia** y registro (sistema de *strikes*) | 72 h |
| **Baja** | Retraso menor, desacuerdo de precio | Mediación; sin sanción | 5 días |

### Proceso
1. **Recepción** con motivo, texto y evidencia (la maqueta ya lo pide).
2. **Clasificación** por gravedad y **asignación**.
3. **Investigación:** chat, código de visita (si no se validó, el técnico probablemente no llegó), fotos y antecedentes.
4. **Decisión:** desestimar, advertencia, suspensión temporal (7 / 30 días) o **baja definitiva**.
5. **Comunicación a ambas partes** con el motivo (sin revelar datos de la otra).
6. **Apelación única** revisada por **otra persona** en 7–14 días.
7. **Cierre y métricas** (casos por tipo, tiempos, reincidencia).

### Implicaciones
- La maqueta dice *«Suspendimos la cuenta de X mientras revisamos»*: **eso solo debe aplicar a casos críticos**; para el resto basta ocultar de nuevas solicitudes.
- **Reportes de técnicos hacia clientes** también deben existir.
- Hace falta un **panel interno de moderación** y personal con **horarios cubiertos**; es el principal costo operativo.
- Los **términos y condiciones** deben describir estas sanciones y el derecho de apelación (ver punto 10).
- Conservación de evidencias: el tiempo mínimo necesario, con acceso restringido.

---

## 6. Eliminar la cuenta con trabajos activos

### Opciones

| Opción | Qué pasa | A favor | En contra |
|---|---|---|---|
| **A. Bloquear la eliminación** hasta cerrar todo | «Termina o cancela tus trabajos primero» | Simple y sin cabos sueltos | **Puede considerarse un obstáculo** para eliminar; las tiendas piden que sea fácil |
| **B. Cancelar y eliminar** | Se cancelan los trabajos y se **avisa a la otra parte** | Respeta la voluntad | Afecta a terceros; hay que gestionarlo |
| **C. Eliminación programada** | Se pide ya; se ejecuta al terminar lo pendiente o tras un **plazo de gracia** | Equilibrio | Más lógica |

### Reglas por tipo de caso (propuesta) ☐

| Situación | Tratamiento |
|---|---|
| **Cliente con cotizaciones abiertas** | Se cierran y se avisa a los técnicos |
| **Visita próxima (< 24 h)** | Se cancela con aviso inmediato; se advierte el impacto |
| **Técnico con citas** | Se avisa a los clientes y se ofrece reprogramar |
| **Reporte o disputa abierta** | **No se pierde la evidencia**: la cuenta se cierra y los datos mínimos necesarios se **conservan restringidos hasta resolver** |
| **Opiniones publicadas** | Se **anonimizan** («Usuario eliminado»); no se borran |
| **Pagos o impuestos pendientes** (si hay pagos) | Se conservan los registros que exija la ley |
| **Mensajes** | Se borran o anonimizan; la contraparte conserva solo lo estrictamente necesario |

### Recomendación ☐
**C con plazo de gracia:** mostrar una lista de lo pendiente, permitir **«Cancelar lo pendiente y eliminar»**, **programar** la eliminación con **7–30 días de arrepentimiento** y borrar o anonimizar al terminar, **conservando solo lo que exija la ley** y diciéndolo claramente.

### Lo que exigen las tiendas de apps (verificar al publicar)
- **Apple:** las apps que permiten crear cuenta deben permitir **iniciar la eliminación dentro de la app**; **desactivar no basta**; puede pedirse **re-autenticación**; **no** debe exigirse llamada o correo a soporte para completarla; y si hay *Inicio con Apple*, hay que revocar sus tokens.
- **Google Play:** la app debe ofrecer **eliminar la cuenta y sus datos desde la app** y además un **enlace web** donde iniciar la solicitud; permite conservar datos por **fraude o cumplimiento legal** si se **explica claramente**.

---

## 7. Idiomas: español + inglés con la puerta abierta

✅ **Ya preparado en la maqueta.** Cambios hechos:
- El texto fuente sigue en español, pero **cada idioma vive en su propio archivo** (`js/i18n/en.js`) y se **registra** en `js/i18n.js`.
- **Agregar un idioma = crear un archivo** y añadir su `<script>`; el selector de idioma (en Configuración y en el panel de pruebas) lo toma del registro automáticamente. Lo probé registrando un tercer idioma de prueba sin tocar `knock.js`.
- Lo no traducido **cae al español** (cadena de respaldo) y hay un **reporte de textos sin traducir** (`i18nReport()` en la consola del navegador) para pasárselo a quien traduce.
- El paquete admite `locale` (formatos de fecha, número y moneda) y `dir` (**derecha a izquierda**, por si algún día se agrega un idioma como el árabe), y actualiza `<html lang>` y `<html dir>`.
- Un idioma guardado que ya no exista vuelve al español.

### Para el producto real ☐
- Pasar de «el texto en español es la clave» a **claves estables** (`home.search.title`) con herramienta de traducción (Crowdin, Lokalise, Phrase…), **plurales y variables** (formato ICU), y pruebas con **pseudo-localización** (textos más largos).
- **Fechas, números y moneda** con `Intl` según `locale` (MXN en pesos).
- **Idioma por defecto** según el dispositivo, con cambio manual (la maqueta no lo detecta a propósito para que la demo siempre salga en español).
- **Contenido escrito por usuarios** (descripciones, mensajes): no se traduce solo; valorar traducción automática opcional con aviso.
- **Textos legales y plantillas de SMS/correo** también por idioma (cada uno revisado por un humano).
- **Variantes regionales** (es-MX) y, según el mercado, **lenguas indígenas** de la región (por ejemplo náhuatl y otomí en Hidalgo) como posibilidad futura; la arquitectura ya no lo impide.

---

## 8. Plataforma: app multiplataforma (requisito)

Si el requisito es **una app para iOS y Android** (y quizá web), hay que elegir **cómo** hacerla. Opciones principales:

| Opción | Idea | Reuso de la maqueta | A favor | En contra |
|---|---|---|---|---|
| **PWA** | Sitio instalable | Total | Rapidísimo, un solo código | Limitaciones en iOS (notificaciones, cámara/SDK de verificación), **no es «app» de tienda** salvo empaquetado |
| **Capacitor / híbrida** (web dentro de un contenedor nativo) | Se **envuelve el HTML/JS** en una app y se accede a funciones nativas con plugins | **Alto**: se conserva la UI actual | Ruta más rápida desde la maqueta; un solo código para web, iOS y Android | Rendimiento y «sensación nativa» más limitados (mapas, animaciones); depende de que existan plugins para los SDK de **verificación**, **mapas** y **push** |
| **React Native** | JavaScript/React con componentes nativos | **Medio**: se reutiliza lógica y conocimiento, no la UI | Gran ecosistema, apariencia nativa | Se reescribe la interfaz |
| **Flutter** | Dart, dibuja su propia interfaz | **Bajo**: reescritura total | UI consistente, buen rendimiento | Equipo nuevo en Dart; el web no es el foco |
| **Nativo (Swift/Kotlin)** | Dos apps | Nulo | Máximo control | El doble de trabajo y de costo |

*(Las comparativas encontradas son de blogs; sirven como orientación, no como benchmark.)*

### Criterios para decidir
1. **Integración con los SDK de terceros:** ¿el proveedor de **verificación de identidad**, el de **mapas** y el de **push** ofrecen SDK o plugin para la opción elegida? **Es el criterio más importante**.
2. **Rendimiento del mapa** y de la cámara.
3. **Aceptación en las tiendas:** una app que solo envuelve un sitio puede ser rechazada si no aporta funciones propias (**verificar** las guías vigentes de Apple y Google).
4. **Habilidades del equipo** y tiempo disponible.
5. **Mantenimiento** y costo a 2–3 años.

### Recomendación ☐ — **prueba técnica de 1–2 semanas**
Antes de decidir, construir en **dos candidatos** (por ejemplo **Capacitor** y **React Native**) un mismo prototipo con lo más riesgoso: **captura con el SDK de verificación**, **mapa con pines y zoom**, **notificaciones push** y **cámara/fotos**. Se mide lo que cuesta cada uno y se decide con datos.
- Si el **tiempo y el reuso** mandan: **Capacitor** (aprovecha esta maqueta).
- Si manda la **calidad nativa y el ecosistema**: **React Native** (la maqueta pasa a ser el prototipo/especificación).

**Independiente de la opción:** construir primero el **backend/API** (autenticación, solicitudes, cotizaciones, chat, verificación). Es lo mismo para cualquier app.

**Necesita asesoría de:** un arquitecto móvil o *tech lead* con experiencia en las tiendas.

---

## 9. Pagos en la plataforma

Se decide dentro del [punto 1](#1-modelo-de-negocio). En una línea: **empezar sin pagos**, y añadirlos (con comisión y garantía) solo cuando haya **confianza, densidad y una solución fiscal clara**.

---

## 10. Cumplimiento y textos legales

> **No es asesoría legal.** Es la lista de lo que hay que llevar a un abogado y a un contador.

### A quién contratar
| Perfil | Para qué |
|---|---|
| **Abogado de protección de datos y comercio electrónico/plataformas** | Aviso de privacidad, términos, contratos con proveedores, política de reportes, cumplimiento de tiendas |
| **Contador/fiscalista** | Régimen de plataformas, retenciones, comprobantes (CFDI), obligaciones del técnico y de Knock |
| **Especialista en seguridad** (puede ser una firma o un asesor) | Evaluación de riesgos, cifrado y manejo de datos biométricos |

*Dónde buscarlos:* despachos con práctica en **protección de datos y tecnología**, asesores de incubadoras o aceleradoras locales, y la vinculación de tu institución. Pide **cotización por entregable** y pide ejemplos previos con *startups* de plataformas.

### Marco que conviene conocer (verificar con el abogado)
- **Nueva Ley Federal de Protección de Datos Personales en Posesión de los Particulares:** publicada en el **DOF el 20 de marzo de 2025**, en vigor al día siguiente, y **abroga la de 2010**. Con la desaparición del INAI, sus funciones en el sector privado pasan a la **Secretaría Anticorrupción y Buen Gobierno**. Según los análisis: el **consentimiento debe ser libre, específico e informado**; el **aviso integral debe identificar los datos sensibles** y **distinguir** las finalidades que requieren consentimiento de las que no; y los **datos biométricos** suelen tratarse como **sensibles**, con **sanciones más altas** (*verificar* el texto y la lista exacta de datos sensibles en el DOF).
- **Datos biométricos (selfie y comparación facial):** tratarlos con **consentimiento expreso**, finalidad limitada, medidas reforzadas y evaluación de riesgos antes de lanzar.
- **Plataformas digitales (fiscal):** régimen del SAT para plataformas, con **retenciones de ISR e IVA** y reglas si el prestador no da RFC. Se reporta que **cambió para 2026**; las cifras difieren entre fuentes → **contador**.
- **Reforma laboral de plataformas (DOF 24-dic-2024):** por ahora enfocada en **transporte y reparto**; vigilar su alcance y mantener claro que los técnicos son **independientes**.
- **Tiendas de apps:** política de privacidad, declaración de datos recopilados y **eliminación de cuenta dentro de la app** (Apple y Google, ver punto 6).
- **Protección al consumidor y comercio electrónico (PROFECO):** reglas para plataformas que intermedian; **preguntar al abogado qué aplica** (no se investigó en esta ronda).

### Documentos a elaborar ☐

| # | Documento | Notas |
|---|---|---|
| 1 | **Aviso de privacidad** integral, simplificado y corto | Datos tratados (marcando **sensibles**), finalidades con y sin consentimiento, derechos ARCO, contacto |
| 2 | **Términos y condiciones de usuarios** | Knock como **intermediario**, responsabilidades, conducta, suspensión, reportes |
| 3 | **Términos para técnicos** | Independencia, verificación, reglas de reputación, sanciones y apelación |
| 4 | **Consentimiento específico de verificación** | Biométricos: finalidad, retención, proveedor, revocación |
| 5 | **Política de reportes y moderación** | Basada en el punto 5 |
| 6 | **Política de retención y borrado** | Plazos por tipo de dato; proceso de eliminación (punto 6) |
| 7 | **Contratos con proveedores (encargados de datos)** | KYC, SMS, nube, IA, chat: qué datos ven, dónde, cuánto tiempo, transferencias a otros países |
| 8 | **Política de cookies y analítica** | Para web |
| 9 | **Página web de eliminación de cuenta** | La pide Google Play |
| 10 | **Registro de consentimientos** | Qué, cuándo y con qué versión del aviso |
| 11 | **Evaluación de impacto en privacidad** | Recomendable para biométricos |

### Preguntas para la primera reunión con el abogado
1. ¿Qué datos de nuestra operación son **sensibles** según la ley vigente y qué consentimiento exige cada uno?
2. ¿Podemos prometer que **no guardamos las fotos del INE**, y en qué condiciones, si el proveedor las retiene un tiempo?
3. ¿Qué responsabilidad tiene Knock por lo que pase en una visita? ¿Cómo redactar el rol de **intermediario**?
4. ¿Cómo estructurar la relación con técnicos para que sea de **independientes**?
5. ¿Qué plazos aplican para atender **derechos ARCO** y para **conservar** evidencia de reportes?
6. ¿Qué cambia si agregamos **pagos** dentro de la app?
7. ¿Qué obligaciones nos impone **PROFECO** como plataforma?
8. ¿Podemos usar **proveedores en el extranjero** (almacenamiento, IA) y bajo qué condiciones?

---

## Siguientes pasos sugeridos

1. **Hoy:** aceptar o ajustar las propuestas ☐ de los puntos 0 a 6 (decisiones de producto que no requieren expertos para empezar).
2. **Esta semana:** entrevistas con **técnicos** (precios, suscripción, spam) y con **clientes** (confianza, verificación).
3. **Contratar** asesoría legal y fiscal (punto 10) y pedir **cotizaciones a 2–3 proveedores de verificación** (punto 3).
4. **Prueba técnica de plataforma** de 1–2 semanas (punto 8).
5. **Definir el piloto:** ciudad, colonias iniciales y primeros oficios (puntos 0 y 2).

---

## Fuentes

Búsquedas web realizadas en octubre de 2026. **Son fuentes secundarias** (despachos, blogs y foros): sirven para orientarse, **no** para citar como texto legal. Verificar siempre el texto oficial (DOF, SAT, documentación del proveedor).

**Protección de datos (LFPDPPP 2025)**
- [Garrigues — nueva LFPDPPP](https://www.garrigues.com/es_ES/noticia/mexico-nueva-ley-federal-proteccion-datos-personales-posesion-particulares-introduce)
- [Hogan Lovells — What it means for companies](https://www.hoganlovells.com/es/publications/mexicos-new-federal-data-protection-law-what-it-means-for-companies)
- [KPMG México — flash sobre la nueva ley](https://kpmg.com/mx/es/tendencias/2025/04/flash-nueva-ley-federal-de-proteccion-de-datos-personales-en-posesion-de-los-particulares.html)
- [IDC Online — 5 cambios clave](https://idconline.mx/corporativo/2025/03/28/lfpdppp-5-cambios-clave-en-el-manejo-de-datos-personales)
- [Portal ERP — cambios en datos biométricos](https://portalerp.com/mx/noticia/cambios-a-la-lfpdppp-en-proteccion-de-datos-biometricos)

**Tiendas de apps**
- [Apple — Guideline 5.1.1(v), eliminación de cuenta](https://developer.apple.com/help/app-review/guideline-reference/5-1-1-account-deletion)
- [Google Play — requisitos de eliminación de cuenta](https://support.google.com/googleplay/android-developer/answer/13327111)

**Fiscal y laboral**
- [Mercado Pago — cambios fiscales 2026](https://www.mercadopago.com.mx/blog/cambios-fiscales-mexico)
- [Siigo — régimen de plataformas digitales](https://www.siigo.com/mx/blog/obligaciones-fiscales/regimen-plataformas-digitales-sat-obligaciones/)
- [El Contribuyente — paquete económico 2026](https://www.elcontribuyente.mx/2025/11/paquete-economico-2026-asi-aumentaran-impuestos-y-retenciones-para-personas-fisicas/)
- [KPMG México — regulación de plataformas digitales](https://kpmg.com/mx/es/tendencias/2025/08/imagen-regulacion-en-materia-de-plataformas-digitales.html)
- [Cámara de Diputados — dictamen de la reforma de plataformas](https://comunicacion.diputados.gob.mx/sintesis/LINKSIN3/111224_M_UNIVERSAL_0_DIPUTADOSAPRUEBAN_REFORMAPLATAFORMASDIGITALESXFOTOSG_SG_.pdf)

**Modelos de ingreso de marketplaces de servicios**
- [ProductMint — cómo gana dinero Thumbtack](https://productmint.com/thumbtack-business-model-how-does-thumbtack-make-money/)
- [Kreezalid — modelo de negocio de Thumbtack](https://www.kreezalid.com/blog/78499-thumbtack-business-model)
- [Vator — cómo gana dinero TaskRabbit](https://vator.tv/2018-07-20-how-does-taskrabbit-make-money/)

**Verificación de identidad (INE)**
- [Veriff — validación contra la base del INE](https://devdocs.veriff.com/docs/ine-database-verification)
- [Jumio — validación de credencial INE (Lista Nominal)](https://docs.jumio.com/production/Content/References/Risk%20Signals/Mexican%20INE%20Credential%20Validation%20(Lista%20Nominal).htm)
- [Incode — validación gubernamental](https://docs.incode.com/docs/omni-api/api/government-validation)
- [Didit — validación de vigencia del INE](https://didit.me/hi/blog/mexico-ine-vigencia-database-validation/)

**Plataforma multiplataforma**
- [Comparativa Flutter vs React Native vs Capacitor (Medium)](https://medium.com/@dev.sreerages/flutter-vs-react-native-vs-capacitor-js-choosing-the-best-cross-platform-framework-84ba8ef497b6)
- [Guía de Capacitor (Capgo)](https://capgo.app/blog/gu%C3%ADa-completa-de-capacitor)
