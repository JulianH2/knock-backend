# Knock: descripción del proyecto

Este documento cuenta, con nuestras palabras y con el mayor detalle posible, qué es Knock, qué hace cada parte y cómo la vamos a construir. Empieza con una descripción general del proyecto y sigue con la descripción del backend, la de la aplicación móvil y la de la landing page; es un requerimiento describir por separado el backend y el móvil, y por eso cada uno tiene su propia sección completa. Al final se explica dónde está cada tema que se estableció como requerimiento, se resumen las reglas que el proyecto nunca rompe y se incluye un glosario.

Sirve de contexto para generar el prompt y, a partir de él, la constitución del proyecto. Por eso cada decisión viene con su razón: se explica qué se decidió, por qué se decidió y qué alternativas se descartaron. Las hojas de cálculo de componentes y de endpoints, que son documentos aparte, bajan al detalle de cada tabla, cada campo y cada índice. Aquí se describe todo en general.

---

# DESCRIPCIÓN GENERAL DEL PROYECTO

## 1. Qué es Knock

### 1.1 El problema
En una ciudad mediana como Tula de Allende, cuando a alguien se le descompone el boiler, se le va la luz en un contacto o le falla el auto, casi nunca tiene un técnico de confianza a quien llamar. Lo que hace es pedir recomendaciones en grupos de Facebook y de WhatsApp, y eso trae cuatro problemas. El primero es que no sabe quién es la persona: no hay forma de comprobar su nombre, su experiencia ni si de verdad trabaja en eso. El segundo es que no puede comparar, porque recibe uno o dos precios por mensaje sin saber qué incluye cada uno, si es solo la visita, el arreglo completo o si las refacciones van aparte. El tercero es que se arriesga en su casa, porque en plomería y electricidad el trabajo es a domicilio y deja entrar a un desconocido, a veces estando sola o solo. El cuarto es que se expone a que le cobren de más, ya que el precio se acuerda de palabra y, cuando aparece un problema nuevo a mitad del trabajo, no hay un tope ni un registro de lo acordado.

Del lado del técnico también hay un problema. Depende de que alguien lo recomiende, no tiene forma de demostrar su trabajo ni su reputación y pierde tiempo contestando mensajes de gente que solo pregunta precios.

### 1.2 La solución
Knock es una aplicación móvil para Android que ordena ese proceso. La persona describe su problema escribiendo qué pasa y subiendo de una a cinco fotos; la foto es obligatoria porque le dice al técnico qué herramientas y piezas debe llevar. Knock envía la solicitud a los técnicos adecuados de su colonia, de forma ordenada y sin saturar a nadie. Los técnicos cotizan, y cada cotización dice el precio, si es fijo o un rango, el día y la hora en que podría ir y una nota, además de un tope: "No pasará de tanto sin tu aprobación". La persona compara hasta cinco cotizaciones, con la calificación del técnico, su distancia, si está verificado y qué cubre cada precio. Cuando elige, se confirma la cita, se revelan el teléfono y la dirección y se cierran las conversaciones con los demás técnicos. Finalmente recibe el servicio y deja su opinión; en la visita a domicilio usa un código de cuatro dígitos para comprobar que quien llega es el técnico elegido.

### 1.3 Servicios que cubre
Empezamos con tres oficios, guardados como datos y no escritos en el código, para poder agregar más sin reprogramar. La mecánica automotriz se atiende en el taller del técnico, donde el cliente lleva su auto. La plomería y la electricidad se atienden a domicilio. Quien se registra como técnico y elige "Otro oficio" queda registrado y su oficio aparece en su perfil, pero no recibe solicitudes, porque todavía no existe esa categoría. Lo que escribe se guarda en una lista de oficios pedidos, que sirve para decidir qué categoría abrir después.

### 1.4 Dónde empezamos
La ciudad piloto es Tula de Allende, Hidalgo, abierta colonia por colonia. Se empieza en una sola zona porque un servicio como este solo funciona si cada solicitud recibe rápido varias cotizaciones, y eso requiere densidad, es decir, muchos técnicos cerca de los clientes. Fuera de la zona, la app avisa que Knock todavía no llega ahí y ofrece apuntarse a una lista de espera.

### 1.5 Lema
Nuestro lema es "Técnicos de confianza, a un toque".

## 2. Quiénes lo usan

### 2.1 El cliente
Es cualquier persona que necesita un servicio del hogar o del auto. Puede entrar sin cuenta a ver el mapa y los técnicos, pero para pedir una cotización, escribir a un técnico, guardar a un técnico de confianza o ver su actividad necesita iniciar sesión. Después de iniciar sesión, la app lo regresa exactamente a donde estaba. Con su cuenta puede guardar su dirección, que se pide la primera vez que solicita un servicio a domicilio, registrar su auto y datos de su casa, guardar técnicos de confianza y contactos de confianza, ver su historial de servicios y volver a contratar a un técnico con un toque.

### 2.2 El técnico
Es un taller, un plomero o un electricista. Puede atender en su taller o ir a domicilio dentro de un radio que él elige, de 3, 5, 8 o 12 kilómetros. Recibe solicitudes, cotiza, atiende al cliente y va construyendo una reputación que se ve en su perfil público: calificación promedio, opiniones, trabajos terminados, nivel y, si lo desea, una insignia de identidad verificada.

### 2.3 Una sola cuenta para los dos papeles
No existen una cuenta de cliente y una de técnico por separado. Si un cliente decide ofrecer sus servicios, conserva todo lo que ya tenía, como su historial, su casa y sus contactos, y gana la pestaña Trabajo y la sección Mi negocio. Se hizo así porque una persona puede ser ambas cosas, por ejemplo un plomero que necesita un mecánico, y tener una sola cuenta evita duplicar datos y que un número de celular quede ligado a dos identidades distintas. Un celular corresponde a una sola cuenta.

### 2.4 El personal interno
Son las personas del equipo que operan Knock desde un panel de administración web. El verificador de identidad revisa las fotos de INE y selfie y decide si se aprueba o se rechaza la verificación. La moderación y el soporte atienden los reportes, deciden las sanciones, revisan las apelaciones y ayudan a quien perdió el acceso a su número. La administración de catálogos mantiene los oficios y los parámetros de precio. El panel exige doble verificación y cada acceso a información sensible queda registrado.

## 3. Por qué la gente puede confiar en Knock
Toda la propuesta se apoya en la confianza, y esa confianza se construye con medidas concretas.

La primera es la identidad verificada, que es opcional. El técnico envía el frente y el reverso de su INE y una selfie, con su consentimiento, y una persona de nuestro equipo las revisa. Las fotos se guardan cifradas con una llave propia que se destruye en cuanto se decide, o a los siete días como máximo, así que quedan ilegibles incluso en las copias de seguridad. Lo único que se conserva es el resultado, la fecha, quién decidió y una huella no reversible de la CURP, que impide que una misma persona abra dos cuentas. Verificarse trae beneficios: una insignia visible, un filtro "solo verificados" para el cliente y una ventaja leve en el orden de los resultados que se explica con claridad al usuario. A quien no se verifica no se le castiga.

La segunda medida es que las opiniones sean solo de servicios reales. Una opinión únicamente puede dejarla quien tuvo un trabajo terminado por Knock, y sale marcada como "Servicio verificado", de modo que no se pueden inflar con reseñas falsas.

La tercera es que los datos queden ocultos hasta elegir. Mientras cotiza, el técnico solo ve la colonia del cliente; el teléfono y la dirección exacta se revelan únicamente al técnico elegido, cuando el cliente acepta su cotización.

La cuarta es el código de visita. En las visitas a domicilio la app muestra al cliente un código de cuatro dígitos, que el cliente le pide al técnico en la puerta. Si el técnico no lo sabe, no se abre. El código es de un solo uso y lo valida el servidor.

La quinta es poder avisar a alguien de confianza. El cliente puede enviar por WhatsApp o por SMS quién va, cuándo y la identidad verificada del técnico, y el mensaje nunca incluye el código.

La sexta es el precio protegido. Cada cotización lleva un tope, y si el técnico descubre un problema que obliga a cobrar más, debe pedir la aprobación del cliente explicando el motivo.

La séptima son los reportes con respuesta. Cualquier persona puede reportar un problema y la respuesta depende de la gravedad, desde una mediación hasta una suspensión preventiva en casos de seguridad.

La octava es que no hay pagos adelantados. La app no cobra, y el chat advierte "no hagas pagos por adelantado". Los problemas se resuelven con reportes, el historial del chat y la reputación.

## 4. Lo que aprendimos y qué vamos a corregir
Revisamos la maqueta completa, recorriéndola como lo haría cada tipo de persona: clientes con distintas necesidades y técnicos nuevos y con experiencia. Esa revisión encontró detalles que una persona que prueba la app rápido y sin intención de revisarla no notaría. Cada hallazgo indica su fuente, para distinguir lo que sale de la revisión de la maqueta de lo que dijeron personas reales en las entrevistas.

Encontramos que el código de cuatro dígitos se explicaba al revés para el cliente y para el técnico, y que el mensaje para avisar a alguien de confianza lo incluía, de modo que la medida de seguridad central no funcionaba como estaba escrita. Vamos a dejar un solo procedimiento, igual en ambos lados y en todos los textos, y el aviso a un contacto nunca llevará el código. Fuente: revisión de la maqueta.

Encontramos que al escribir "fuga de gas" la búsqueda mandaba a plomeros sin ningún aviso. Vamos a detectar el gas y otros riesgos y a mostrar primero las instrucciones de seguridad, antes de buscar o cotizar. Fuente: revisión de la maqueta.

Encontramos que el precio tenía contradicciones: un tope mayor que el rango, un "precio fijo" que podía subir y un "Mejor precio" que podía salir más caro. Vamos a mostrar y ordenar por "máximo sin tu aprobación", a definir qué es un precio cerrado y a quitar los textos que se contradicen. Fuente: revisión de la maqueta.

Encontramos que el cambio de precio por encima del tope aparecía al terminar el trabajo, y que si el cliente lo rechazaba el técnico perdía el trabajo hecho. Vamos a hacer que el técnico proponga el cambio cuando lo detecta y no al final, y a dejar claro qué se paga si se rechaza. Fuente: revisión de la maqueta.

Encontramos que el técnico no veía las fotos que el cliente está obligado a subir. Vamos a mostrar las fotos del cliente al técnico antes de cotizar. Fuente: revisión de la maqueta.

Encontramos que la verificación prometía más de lo que hace, porque decía "validada con el INE" y "prueba de vida" cuando en realidad la revisa una persona y tarda hasta siete días. Vamos a usar textos honestos que expliquen qué se revisa y cuánto tarda. Fuente: revisión de la maqueta.

Encontramos que el técnico no sabía cuánta competencia tiene una solicitud ni cuándo vence. Vamos a mostrar cuántos técnicos ya cotizaron y cuándo vence cada solicitud. Fuente: revisión de la maqueta.

Encontramos que los reportes no coincidían con la política de gravedad y que no había forma de que un técnico reportara a un cliente. Vamos a alinear los reportes con la política, a dar derecho de respuesta y a permitir el reporte del técnico al cliente. Fuente: revisión de la maqueta.

Encontramos que las fotos de trabajos terminados se mostraban en Explorar sin permiso del cliente. Vamos a pedir el permiso del cliente antes de publicarlas. Fuente: revisión de la maqueta.

Encontramos que quien no sabe qué oficio necesita no veía en el teléfono el botón "No sé cuál". Vamos a hacerlo visible en pantallas pequeñas. Fuente: revisión de la maqueta.

Lo que funcionó y se conserva es poder ver técnicos y perfiles sin crear cuenta, la comparación de cotizaciones con la explicación de qué cubre cada una, el tope de precio como idea, el teléfono y la dirección ocultos hasta elegir y el registro rápido del técnico, que en la prueba tardó unos 28 segundos.

Lo que todavía no podemos afirmar es si hay suficientes técnicos en Tula de Allende dispuestos a cotizar sin garantía de trabajo, si los clientes confían en una app nueva más que en la recomendación de un vecino y si alguien pagaría por usarla. Eso solo lo dicen las personas reales.

[PENDIENTE: agregar aquí los hallazgos de las entrevistas a personas reales, cada uno con la fuente "Entrevista": a quién se entrevistó, qué dijeron y qué cambiamos por eso.]

## 5. Las cinco partes del proyecto

### 5.1 La nube
Usamos Azure con la suscripción gratuita para estudiantes. Ahí corre todo el backend, y ahí viven la base de datos, el almacenamiento de fotos, los secretos, los registros y la inteligencia artificial. La infraestructura se crea y se borra con Terraform.

### 5.2 Las bases de datos
Usamos dos, y las dos son NoSQL. MongoDB, de tipo documental, guarda la información principal, y cada servicio tiene su propia base lógica. Redis, de tipo clave-valor, guarda lo temporal y lo que comparten las copias del backend: los tokens cancelados, la caché, los contadores del límite de peticiones y los mensajes del chat entre copias.

### 5.3 El backend
Son tres servicios pequeños, cada uno con una responsabilidad, y una puerta de entrada que los protege a todos. Todo se construye en ASP.NET Core 11. Se describe en la sección Backend.

### 5.4 La app móvil
Es una aplicación Flutter para Android, publicada en Google Play. Se describe en la sección Aplicación móvil.

### 5.5 La landing page
Es una página web a donde llega el tráfico de las redes sociales y desde donde se redirige a Google Play. También aloja el aviso de privacidad y la página para eliminar la cuenta. Se describe al final.

### 5.6 El panel de administración
Es una aplicación web interna, hecha con Angular, para el personal: la cola de verificaciones, la cola de reportes, los oficios y los parámetros de precio. No es parte de los requerimientos evaluados, así que tiene la prioridad más baja, pero es necesario para operar la verificación y la moderación.

## 6. Cómo encajan las piezas

### 6.1 El recorrido de la información
La gente se entera de Knock en una red social y llega a la landing page, que la manda a Google Play. Ya instalada, la app no habla directamente con cada servicio, sino que todas sus peticiones llegan a una sola puerta de entrada. Esa puerta recibe el tráfico por HTTPS, comprueba quién es la persona validando su token, limita cuántas peticiones puede hacer en un periodo y reparte la petición al servicio correcto. Los servicios internos no tienen dirección pública. Se hizo así porque cada servicio expuesto es una puerta más que defender; con una sola entrada, la seguridad, el límite de peticiones y el registro se aplican en un único punto.

### 6.2 Los tres servicios
El servicio de Identidad y confianza es el dueño de las personas. Se encarga del inicio de sesión, de la cuenta de cada persona, de las sesiones por dispositivo, de la verificación de identidad, de los reportes y de los bloqueos. Se llama de confianza porque ahí vive todo lo que decide si alguien es confiable: verificaciones, reportes y sanciones.

El servicio de Mensajería y notificaciones es el dueño de la conversación. Maneja el chat en tiempo real, con mensajes, quién está en línea, quién está escribiendo y las marcas de enviado y leído, además de los avisos dentro de la app y las notificaciones al teléfono.

El servicio de Mercado es el dueño del negocio. Maneja el catálogo de oficios y colonias, los perfiles de técnicos, la búsqueda, las solicitudes, las cotizaciones, las visitas y citas, y las opiniones con la reputación. También aloja el módulo Asistente, que es el dueño de la inteligencia artificial y del aprendizaje automático: el asistente que ayuda a armar una solicitud, el resumen del chat, el borrador de cotización, el orden "Recomendadas" y el rango de precio sugerido.

### 6.3 Por qué tres servicios
No elegimos un solo programa grande porque usar microservicios es un requerimiento, y además un solo programa obliga a desplegar todo junto, hace que un error en el chat pueda tirar las cotizaciones y complica repartir el trabajo entre cuatro personas. Tampoco elegimos ocho servicios, uno por módulo, porque somos un equipo de cuatro personas con unas cinco semanas, y ocho servicios significan ocho procesos automáticos, diez o más contenedores en ejecución y mucho más costo en la nube, lo que no cabe en el costo cero.

Elegimos tres porque agrupan los módulos que cambian juntos. El Asistente trabaja sobre las solicitudes y cotizaciones de Mercado y casi no tiene datos propios, así que va dentro de Mercado como un módulo más. Cada servicio tiene su propio responsable, su propia base de datos, su propia imagen de contenedor y su propio módulo de Terraform. Dentro de cada servicio los módulos siguen separados con fronteras estrictas, así que si el tiempo lo permite o la carga lo exige, uno se puede separar sin reescribirlo. Como plan de emergencia, si el tiempo se acaba, se pueden juntar Mensajería y Mercado en uno solo y quedarían dos servicios.

### 6.4 Reglas de convivencia entre servicios
Ningún servicio lee la base de datos de otro; si necesita algo, lo pide por la interfaz del otro servicio. Los acuerdos entre servicios, es decir, qué datos se intercambian, viven en una biblioteca compartida que solo contiene tipos y contratos, sin lógica de negocio, para no acoplarlos. Un identificador de correlación viaja en cada petición para poder seguirla de un servicio a otro en los registros. Y cada servicio se construye, prueba y despliega por separado.

## 7. Cómo vamos a trabajar

### 7.1 Primero la especificación, después el código
Usamos la metodología Spec-Driven Development: antes de programar algo, se escribe y se aprueba una especificación que dice qué debe hacer, cómo se prueba y qué riesgos tiene. Lo hacemos así porque todo el código lo escriben agentes de inteligencia artificial, y un agente sin una especificación clara inventa cosas que no encajan con el resto del proyecto. La especificación le dice qué construir y las pruebas le dicen cuándo terminó. Con este método el trabajo se concentra al inicio, que es donde se define bien, y luego se acelera; así se gasta mucho menos del límite de uso de la herramienta de IA.

La constitución es el documento con las reglas que nunca se rompen, y si hay un conflicto entre cualquier documento y la constitución, manda la constitución. Hay una especificación por cada función: una para el inicio de sesión, otra para el registro, otra para cada endpoint importante, y así. Cada una dice qué hace, cuál es el contrato, qué amenazas existen, cuáles son los casos de prueba y cuándo se considera terminada. Si una especificación se queda corta, por ejemplo porque olvida el autocompletado o los filtros, se crea otra, y por eso conviene definir bien cada endpoint desde las hojas de cálculo.

Existe además un manual para agentes, que es un archivo único que cualquier agente de IA lee antes de trabajar y que explica la estructura del proyecto, los comandos y las reglas. Hay habilidades reutilizables, que son guías por tema, como construir una función completa del backend, aplicar la seguridad, manejar eventos o escribir pruebas, y cada agente carga solo la que necesita para gastar menos contexto. Los agentes tienen permisos distintos: los que construyen pueden editar archivos, y los que revisan, como el de seguridad o el de trazabilidad entre especificación, código y pruebas, no pueden editar y solo comentan. Ningún agente aprueba ni firma una especificación; eso lo hace el líder. Se lleva también una bitácora de decisiones, donde se anota cada decisión importante con su fecha y su razón, y comprobaciones automáticas en cada cambio, de modo que el repositorio no acepta un cambio que no cumpla las reglas.

### 7.2 Seguridad desde el principio
DevSecOps significa que la seguridad y la calidad se revisan en cada cambio, de forma automática, durante todo el desarrollo y no al final. En la práctica, cada cambio dispara pruebas, análisis del código, búsqueda de claves olvidadas, revisión de dependencias vulnerables y revisión de contenedores. Si algo falla, el cambio no se acepta. El detalle de cada comprobación está en las secciones de pruebas de los documentos de backend y móvil.

### 7.3 Desarrollo con inteligencia artificial
El backend y la app se desarrollan por completo con agentes de IA. Usamos OpenCode con la interfaz Warp, herramientas establecidas como requerimiento del proyecto. También se pueden usar otras herramientas, como Claude Code, Codex, Copilot, Cursor o Gemini, porque las instrucciones se escriben una sola vez en un formato neutro y un programa las genera para cada herramienta, así que el equipo puede cambiar de herramienta sin reescribir nada.

Para que la IA no cause daños, un vigilante automático bloquea los cambios que rompen la arquitectura, por ejemplo un servicio que lee la base de otro, y no deja terminar una tarea sin verificarla. Cada cambio incluye una sección llamada "Uso de IA" que declara qué parte generó la IA y qué revisó una persona. La IA nunca aprueba ni fusiona cambios y nunca recibe datos personales sensibles. El flujo de trabajo con el agente consiste en definir el contexto del proyecto, que es este documento, generar la constitución, generar la especificación de cada función, construir, probar y revisar.

### 7.4 Conectar las herramientas con MCP
MCP, por las siglas de Model Context Protocol, permite que el agente de IA opere directamente sobre una herramienta externa sin que tengamos que abrirla. Lo usamos donde el servicio lo ofrece. Con GitHub, revisa los cambios y el estado de las comprobaciones. Con MongoDB, revisa colecciones e índices del entorno de desarrollo, con una cuenta de solo lectura, para que un error del agente no pueda dañar datos.

## 8. Cómo numeramos las versiones

### 8.1 Para qué sirve
Cada vez que publicamos algo le damos un número de tres partes, X.Y.Z. Sirve para que el equipo sepa qué cambio salió en qué versión, y para que, cuando alguien tiene un problema, sepamos qué versión usa y si está desactualizada. Por ejemplo, una persona usuaria puede decirle a soporte "tengo la versión 1.2.3".

### 8.2 Cómo se reparten los números
El primer número, X, sube cuando un cambio rompe lo anterior, por ejemplo cuando se borra un endpoint o se cambia lo que devuelve de forma que la versión vieja de la app ya no funciona. El número de en medio, Y, sube cuando se acumulan cambios que agregan funciones. El último, Z, sube con los cambios pequeños, como correcciones y ajustes. La primera versión publicada es la 1.0.0.

### 8.3 Cuántos puntos vale cada cambio
Cada cambio vale una cantidad de puntos según su tipo, y los puntos se acumulan y mueven los números. Corregir un error sin cambiar lo que devuelve vale un punto, y lo mismo vale cambiar el tipo o el formato de un dato existente sin romper a nadie, por ejemplo que el precio pase de entero a decimal, ajustar un texto, una traducción o un estilo, y agregar un índice o mejorar el rendimiento o la caché. Agregar un campo nuevo a una colección o a una respuesta, como los años de experiencia del técnico, vale dos puntos, y también cambiar una regla de negocio existente, como pasar de 48 a 72 horas el vencimiento de una solicitud. Agregar un endpoint o una pantalla nueva vale tres puntos. Agregar una colección nueva vale cinco puntos, y también terminar una función completa, es decir, una historia de usuario de punta a punta. Agregar un servicio, una integración externa o un permiso nuevo del teléfono vale diez puntos. Un cambio que rompe lo anterior no suma puntos, sino que sube X directamente.

### 8.4 Cómo se acumulan
Al preparar un despliegue se suman los puntos de todos los cambios incluidos desde la versión anterior, y esos puntos se agregan al último número, Z. Cuando Z llega a diez o más, vuelve a cero conservando el sobrante y Y sube uno por cada diez. Cuando Y llega a diez o más, vuelve a cero y X sube uno. Un cambio que rompe lo anterior sube X y deja Y y Z en cero.

Por ejemplo, de la 1.0.0, un cambio de tipo de dato, que vale un punto, y un endpoint nuevo, que vale tres, suman cuatro y dan la 1.0.4. Después, una pantalla nueva, de tres puntos, y una colección nueva, de cinco, suman ocho; cuatro más ocho son doce, así que Z se queda en dos, Y sube uno y se llega a la 1.1.2.

### 8.5 Dónde se aplica
Cada pieza lleva su propia versión: cada uno de los tres servicios, la puerta de entrada, la app, el panel y la landing, de modo que un cambio en la landing no cambia el número del backend. Cada despliegue crea una etiqueta de Git con la versión y una imagen de contenedor con ese mismo número. En la app de Android, el número visible es X.Y.Z, y Google Play exige además un número interno que siempre crezca, llamado versionCode, que se calcula como X por 1,000,000, más Y por 10,000, más Z por 100, más n, donde n es el número de subida de esa misma versión (de 0 a 99), porque Google Play no acepta dos veces el mismo número. Para la 1.1.2, en su primera subida, es 1,010,200. Un despliegue con cero puntos no cambia la versión.

Para automatizarlo, cada cambio declara sus puntos por cada componente que toca, por ejemplo puntos-backend-3, y la comprobación automática exigirá la etiqueta (hasta que exista, la revisa una persona). Al desplegar, el proceso suma las etiquetas de los cambios incluidos, calcula la versión nueva y la aplica a todo. Se lleva además un registro de versiones desplegadas con la versión, la fecha, los cambios incluidos y los puntos.

## 9. Cómo nos organizamos

### 9.1 Cuentas y herramientas compartidas
Todos los repositorios están en una sola cuenta de GitHub (hoy la del líder; pasan a una organización gratuita del proyecto), y usamos una sola cuenta de Google exclusiva del proyecto, con un correo del dominio del proyecto, para dar de alta Google Play, Firebase, Google Cloud y Google Analytics, protegida con verificación en dos pasos. Azure usa la suscripción de estudiante. Lo hacemos así porque en algún momento hay que compartir credenciales y propiedad de los servicios, y con cuentas personales se pierde el control si una persona deja el equipo. Además, el líder del proyecto comparte con todos una carpeta donde está toda la información que se va generando.

### 9.2 Repositorios
Hay dos repositorios. El de backend contiene los tres servicios, la puerta de entrada, la infraestructura con Terraform, los scripts de la base de datos y las pruebas. El de frontend contiene la app Flutter, el panel Angular y la landing. Cada repositorio tiene su propio proceso automático y su propia numeración de versiones.

### 9.3 Flujo de cambios
Cada tarea se hace en una rama de trabajo con un nombre ordenado que incluye el tipo de cambio y la clave de la tarea. La rama se une a una rama de integración del periodo de trabajo, y de ahí pasa a la rama principal. Cada cambio se presenta como una solicitud de cambio, con una plantilla que incluye la sección de uso de IA. El proceso automático corre las pruebas y las revisiones. Después, otro integrante revisa y comenta qué revisó; esta revisión de pares no aprueba ni fusiona. Por último, el líder hace la revisión final, aprueba y fusiona.

### 9.4 Roles del equipo
El líder del proyecto coordina, mantiene la infraestructura, el proceso automático de despliegue, la seguridad y la documentación, revisa y fusiona todo, y apoya a los demás. En el backend trabajan dos personas: una se encarga de Identidad y de Mensajería, y la otra de Mercado y de Asistente, además de la base de datos y el aprendizaje automático; la puerta de entrada se comparte entre el líder y quien lleva Identidad. Una persona lleva la app Flutter y también revisa los cambios del backend. El panel de administración y la landing no tienen dueño fijo, sino que se asignan según la carga de cada quien al planear cada periodo, con un máximo de dos tareas en curso por persona. Lo que tiene fecha límite, como la landing con la página de privacidad que exige Google Play, siempre lleva dueño y fecha desde el principio.

## 10. Cómo mantenemos el costo en cero

### 10.1 Los principios
Usamos el crédito gratuito de la suscripción de estudiante, que es de 100 dólares y no pide tarjeta, y el nivel gratuito de cada servicio. "Costo cero" y "gasto del crédito" no son lo mismo: lo que se pase del nivel gratuito se descuenta del crédito, y las alertas de presupuesto solo avisan, no detienen el gasto, por eso el consumo se revisa cada semana.

Los servicios del backend se apagan o bajan a lo mínimo cuando nadie los usa. La ventana de evaluación va del 25 de noviembre al 5 de diciembre de 2026, y solo en esos días cada servicio corre con dos copias fijas. Se hace así porque con cuatro contenedores de aplicación a dos copias son ocho copias encendidas todo el mes, más Redis, y eso supera con mucho la cantidad mensual gratuita de Container Apps, que es de 180,000 segundos de procesador, 360,000 segundos-gigabyte de memoria y 2 millones de solicitudes. Al terminar la evaluación se ejecuta la eliminación de toda la infraestructura, con la excepción de la base de datos gratuita, que se conserva.

### 10.2 Qué es gratis y qué no
Container Apps es gratis hasta una cantidad mensual; fuera de la evaluación el número de copias baja a una (y a cero solo en un servicio que no ejecute procesos con tiempo). MongoDB en DocumentDB tiene un nivel gratuito que es único por suscripción, así que se reutiliza y no se borra. Content Safety, que modera el contenido, tiene un nivel gratuito, y se usa dentro de su cuota. Blob Storage, Key Vault y Application Insights tienen un costo muy bajo por uso que se revisa cada semana. Para la CDN de las imágenes públicas el producto y su costo se deciden antes de la evaluación (Front Door Standard o el servicio de CDN que Azure permita crear), y se enciende solo cuando hace falta. Redis corre como contenedor propio dentro de la cuota de Container Apps, porque la versión administrada de Azure no tiene nivel gratuito. Para guardar las imágenes de los contenedores se usa GitHub Container Registry, que es gratuito, y se descartó el de Azure porque cobra por día. La landing page vive en Railway, en un espacio que ya tiene el líder del proyecto.

Hay tres servicios que pueden salirse del costo cero y tienen su plan de respaldo. Los SMS reales de Firebase requieren un plan de pago por uso que se activa antes de abrir la prueba cerrada de Google Play, con presupuesto y alertas. Azure OpenAI no tiene nivel gratuito y su cuota en esta suscripción es cero, por lo que hasta que se amplíe se usan reglas locales. Google Maps exige una cuenta de facturación, que se crea junto con el plan de Firebase al final.

Hay además dos costos fuera de Azure que no se pueden evitar y que el equipo reparte en partes iguales: la cuota única de la cuenta de desarrollador de Google Play, de 25 dólares, y el buzón de correo del dominio del proyecto, de unos 42 pesos al mes, por uno o dos meses. El dominio ya existía. Estos costos se registran en la revisión de costos, sin datos de pago.

---

# BACKEND

## 1. La nube

### 1.1 Por qué Azure
Los requerimientos permiten elegir entre AWS, Azure y Google Cloud, y elegimos Azure por cuatro razones. La primera es que el backend está hecho en .NET, que es de Microsoft y funciona de forma muy natural en Azure. La segunda es que la suscripción de estudiante da 100 dólares de crédito sin pedir tarjeta. La tercera es que Azure Container Apps incluye una cantidad mensual de uso gratuito para los contenedores. La cuarta es que en la misma nube están la inteligencia artificial, los secretos, el almacenamiento, la moderación de contenido y el registro de eventos, lo que cumple que todas las funciones del backend estén alojadas en la nube elegida.

### 1.2 Servicios de Azure que usamos
Container Apps ejecuta los tres servicios y la puerta de entrada en contenedores, reparte las peticiones entre las copias y termina el cifrado HTTPS. DocumentDB ofrece la base de datos MongoDB en su nivel gratuito. Blob Storage guarda las fotos, tanto las de solicitudes y trabajos como las de verificación, estas últimas en un contenedor privado y separado. La CDN sirve las imágenes públicas desde puntos cercanos a cada persona. Key Vault es la caja fuerte donde viven las claves, las cadenas de conexión y la clave que firma los tokens. Application Insights y Log Analytics guardan los registros de eventos y errores de todos los servicios. Azure OpenAI aporta la inteligencia artificial generativa, y Content Safety detecta texto e imágenes inapropiadas antes de guardarlos.

### 1.3 Los límites reales de la suscripción
Una política de la suscripción solo permite crear recursos en cinco regiones: Chile Central, Norway East, West US, France Central y North Central US. El nivel gratuito de la base de datos existe únicamente en West US, France Central y Norway East, no tiene alta disponibilidad ni respaldo automático, y se pausa si pasa 60 días sin uso. Elegimos West US, que es la más cercana a México de las tres. Ahí ya está creado el clúster, y ahí ponemos también los servicios para que no haya demora entre el servicio y su base de datos. La consecuencia es que los datos quedan fuera de México, y lo declaramos en el aviso de privacidad. Antes de usar cada tipo de recurso con Terraform hay que registrar su proveedor en la suscripción.

### 1.4 Latencia para quien está lejos
Un requerimiento es que las imágenes se repliquen por las regiones de la nube para que quien las consulta desde otro lugar del mundo no espere. Eso lo hace la CDN, que copia las imágenes públicas a puntos cercanos a cada persona. El backend corre en una sola región, porque replicarlo multiplicaría el costo y Knock empieza en una sola ciudad.

## 2. Crear y borrar todo con Terraform

### 2.1 Qué es y por qué
Terraform permite describir toda la infraestructura, como redes, contenedores, caja fuerte y almacenamiento, en archivos de texto, y crearla, cambiarla o borrarla con comandos. Lo usamos porque es repetible, es decir, la misma infraestructura se levanta igual cada vez; porque se revisa como código, de modo que un cambio en la nube pasa por el mismo proceso de revisión que el resto; y porque permite levantar y tumbar todo en minutos, que es lo que se establece como requerimiento para la evaluación.

### 2.2 Cómo está organizado
Hay un módulo por servicio, con su contenedor, su identidad y sus secretos, y un módulo compartido con la red, el entorno de Container Apps, Key Vault, Blob Storage y los registros. El estado de Terraform, que es el registro de lo que ya existe en la nube, se guarda en un almacenamiento compartido de Azure para que todo el equipo y el proceso automático vean lo mismo y no se pisen.

### 2.3 Los cuatro pasos
Cada paso es un proceso automático de GitHub que se lanza a mano, para que nada se cree ni se borre por accidente. El primero crea la infraestructura: corre los comandos de inicio, plan y aplicación y deja la estructura vacía, con la red, el entorno de contenedores, la caja fuerte, el almacenamiento y los registros, pero todavía sin programas corriendo. El segundo despliega: construye la imagen de cada servicio, la publica, crea o actualiza los contenedores con el número de versión y termina con una prueba de humo que llama a la ruta de estado de cada servicio por HTTPS y falla si alguno no responde. El tercero elimina lo desplegado, apagando y quitando los contenedores y dejando la infraestructura vacía. El cuarto elimina la infraestructura con el comando de destrucción y borra todo, excepto la base de datos gratuita.

### 2.4 La base de datos gratuita
El nivel gratuito de DocumentDB es único por suscripción, de modo que si se borrara no se podría volver a crear. Por eso Terraform importa el clúster que ya existe en lugar de crearlo, y el paso final lo excluye del borrado.

### 2.5 Permisos
El proceso automático entra a Azure con una identidad federada, de modo que no guarda ninguna contraseña, y su rol es el mínimo necesario y solo sobre el grupo de recursos de Knock. Cada servicio lee sus secretos de Key Vault con una identidad administrada propia, que solo puede leer los secretos de ese servicio. En las computadoras del equipo se instala el CLI de Azure para consultar y operar desde la terminal, y los permisos se revisan cada semana junto con el consumo.

## 3. Que crezca: dos copias y un balanceador

### 3.1 Qué se pide
El backend debe correr en al menos dos copias, llamadas instancias, de modo que se pueda pasar a tres, cuatro o más si crece el uso. Esto es escalado y no autoescalado: nosotros fijamos cuántas copias hay y no existen reglas que las suban o bajen solas. Los requerimientos lo aclaran: el autoescalado detecta el tráfico y abre servidores automáticamente, y eso no se pide.

### 3.2 El balanceador
Un balanceador de carga recibe las peticiones y las reparte entre las copias de cada servicio; en nuestro caso lo da la entrada integrada de Container Apps. Reparte para que ninguna copia se sature, saca de rotación a una copia enferma dejando de mandarle peticiones, y termina el cifrado, ya que es quien presenta el certificado HTTPS.

El backend aprovecha el balanceador de tres maneras, y eso es lo que significa su uso desde el backend. Primero, cada servicio expone una ruta de vida y otra de disponibilidad: la de vida dice si el servicio está encendido y la de disponibilidad comprueba que puede hablar con la base de datos y con Redis, y el balanceador usa esta última para decidir si le manda tráfico. Segundo, detrás del balanceador el servidor ve la dirección del balanceador y no la de la persona, por lo que lee los encabezados que el balanceador agrega con la dirección original y el protocolo original; esto es necesario para que el límite de peticiones cuente por persona y para que la política de HTTPS funcione bien. Tercero, ningún servicio guarda nada importante en su propia memoria, así que cualquier copia puede atender a cualquier persona en cualquier petición.

### 3.3 Qué cambia al tener varias copias
Con una sola copia, guardar algo en memoria funciona, pero con dos no, porque la segunda copia no lo ve. Por eso los tokens no dependen de la copia: se firman con una clave compartida guardada en Key Vault y cualquier copia los acepta. La lista de tokens cancelados, que sirve para el cierre de sesión, vive en Redis, que las copias comparten. La caché también vive en Redis y no en la memoria de cada copia, porque una caché en memoria daría respuestas distintas según la copia. Los contadores del límite de peticiones viven en Redis, porque si fueran en memoria cada copia contaría por separado y una persona podría hacer el doble de peticiones. El chat reparte los mensajes entre copias por medio de Redis, ya que si dos personas conversan y cada una está conectada a una copia distinta, el mensaje debe llegar de una a otra. Y los procesos con tiempo, que se explican en la sección 4.3, usan una reserva atómica en la base de datos para que solo una copia ejecute cada tarea.

### 3.4 Redis
Redis es nuestra segunda base NoSQL, de tipo clave-valor: guarda pares de llave y valor en memoria, y por eso es muy rápida. Se usa para los tokens cancelados, para la caché de lecturas, para los contadores del límite de peticiones y para repartir los mensajes del chat entre copias. Corre como un contenedor propio de una sola copia dentro de Container Apps, con contraseña guardada en la caja fuerte, y su consumo cuenta contra el crédito de estudiante (no es gratis). La versión administrada de Azure no tiene nivel gratuito: su plan más barato cuesta cerca de 0.022 dólares por hora, unos 16 dólares al mes encendido siempre y unos 6 si solo se enciende en la ventana de evaluación, y queda como alternativa si el contenedor propio no basta.

Hay un riesgo que aceptamos. Si Redis se reinicia, pierde la lista de tokens cancelados, y como un token dura solo 15 minutos, uno ya cerrado podría servir hasta 15 minutos más. Además, los tokens de renovación se guardan en MongoDB, que sí persiste, y ahí la cancelación sí es permanente.

## 4. Cómo está hecha la API

### 4.1 Estilo: REST con OpenAPI
Usamos REST, en el que las funciones se piden con direcciones y verbos estándar para consultar, crear, modificar o borrar. Un archivo llamado OpenAPI describe todas las rutas, qué reciben y qué devuelven. De ese archivo se genera automáticamente el código que usa la app, de manera que si alguien cambia la API sin avisar, la app deja de compilar en lugar de fallar en producción, y además permite una consola de pruebas para demostrar cada endpoint. Las rutas llevan la versión, por ejemplo /api/v1, y si algún día hay un cambio que rompe lo anterior se publica /api/v2 y la versión vieja sigue funcionando un tiempo. Los errores usan un formato estándar llamado ProblemDetails: siempre traen un título, un código y un detalle, y nunca revelan información interna.

Elegimos REST y no GraphQL, gRPC u otro estilo por varias razones. Tenemos un solo tipo de cliente, la app y el panel, y GraphQL es muy útil cuando hay muchos clientes con necesidades distintas, pero aquí agregaría complejidad sin beneficio. REST es más fácil de probar con herramientas estándar, de poner en caché por la propia estructura de HTTP y de documentar. GraphQL tiene problemas conocidos de consultas repetidas, límites de costo difíciles de controlar y caché complicada. gRPC es rápido, pero necesita un intermediario para el panel web y complica subir fotos, así que se reserva para una eventual comunicación interna. Para el chat en vivo usamos SignalR, y para los proveedores externos, webhooks firmados.

### 4.2 Estructura de cada servicio
Cada servicio está organizado por capas. El controlador recibe la petición, comprueba que sea válida y responde, sin lógica de negocio ni manejo de errores propio, porque los errores los atrapa una pieza común. El servicio de aplicación aplica las reglas de negocio y coordina. El dominio contiene las entidades y sus reglas, por ejemplo qué estados puede tener una solicitud. La capa de datos habla con MongoDB y con Redis.

Cada función completa, es decir, cada historia de usuario, atraviesa las cuatro capas en lo que llamamos una rebanada vertical: se construye de punta a punta, con sus pruebas, en lugar de construir todas las capas por separado. Dentro de cada servicio los módulos tienen fronteras estrictas, y unas pruebas de arquitectura fallan si un módulo usa algo que no debe. La tecnología es ASP.NET Core 11 con controladores, validación de datos de entrada con FluentValidation y registros con Serilog.

### 4.3 Cómo se hablan los servicios
Cuando un servicio necesita algo de otro, por ejemplo Mercado necesita saber si un técnico está verificado, se lo pide por una interfaz acordada y descrita en la biblioteca de contratos, y nunca lee su base de datos.

Cuando algo importante pasa, como "se eligió una cotización", el aviso se guarda en la misma operación que el cambio de estado, en una colección llamada outbox, o bandeja de salida, y un proceso de fondo lo entrega después. Así el aviso nunca se pierde si el servicio se reinicia justo en ese momento, ni se avisa algo que no llegó a ocurrir.

Hay plazos que Knock debe vigilar: ampliar la búsqueda de técnicos a los 15 minutos si hay menos de dos cotizaciones, vencer una solicitud a las 48 horas, avisar al verificador a las 24 horas, borrar las fotos de verificación a los siete días, ejecutar la eliminación de una cuenta tras siete días de gracia, y cerrar el periodo para opinar a los 30 días y para reportar "no se terminó" a los siete. Cada uno se guarda como una tarea programada, con la fecha en que debe ejecutarse, y un proceso de fondo revisa periódicamente cuáles ya llegaron. Como hay dos copias, cada tarea se reserva con una operación atómica en la base de datos: la primera copia que la toma la ejecuta y la otra la ve ocupada.

Las llamadas a servicios externos, como Firebase, la IA y la moderación, reintentan si fallan por un momento y tienen un cortacircuitos: si el servicio externo lleva varias fallas seguidas, se deja de llamarlo un rato para no acumular esperas. Así la falla de un tercero no tira a Knock, y la IA además tiene una respuesta de respaldo.

Por último, como la app se usa con señal irregular y puede repetir una petición, las operaciones que crean algo, como una solicitud o una cotización, son idempotentes: la app manda un identificador único de la operación, y si el servidor ya la procesó devuelve el resultado original en lugar de crear un duplicado.

## 5. Las bases de datos

### 5.1 MongoDB
Es la base principal, de tipo NoSQL documental: en lugar de tablas con filas, guarda documentos con forma de JSON dentro de colecciones. Cada servicio tiene su propia base lógica y ninguno lee la de otro.

La elegimos porque cumple el requerimiento de usar una base NoSQL y tiene nivel gratuito en Azure. Un documento guarda el objeto completo, de modo que una solicitud lleva dentro sus fotos, su descripción y su estado y se lee con una sola consulta en lugar de unir varias tablas. Permite búsquedas por ubicación: con el índice geoespacial llamado 2dsphere se resuelve en qué colonia está una persona, que es un punto dentro de un polígono, y qué técnicos hay cerca, que es una distancia. Permite transacciones, que necesitamos porque las solicitudes y las citas tienen estados que no deben quedar a medias, por ejemplo al elegir una cotización y cerrar los demás hilos. Y en cuanto a la integridad, una base relacional obliga a que los datos estén ligados entre sí a la fuerza, pero en este proyecto eso no es la prioridad según los requerimientos y lo cuida la aplicación; a cambio ganamos velocidad y flexibilidad.

Antes de migrar se hace una prueba técnica sobre el clúster gratuito para confirmar que funcionen la consulta de punto dentro de polígono con las colonias de Tula, la cercanía con 2dsphere, una transacción real que cree una solicitud y su aviso en la bandeja de salida, el índice de texto con idioma español, las conexiones desde los tres servicios y la pausa por inactividad. Si algo no se puede resolver, se pasa al plan B.

El plan B es MongoDB Atlas, un servicio externo gratuito, ya que los requerimientos permiten una base de datos externa. Sus límites son estrechos: 0.5 GB de almacenamiento, 500 conexiones, 100 operaciones por segundo y 10 GB de transferencia a la semana. Solo se usa si DocumentDB no cumple.

El nivel gratuito no hace respaldos, así que se programa una copia periódica con mongodump a un almacenamiento de Azure y se prueba periódicamente que se pueda restaurar.

### 5.2 No usamos una base de grafos
Los requerimientos ponen como ejemplo una red social con relaciones entre amigos, donde conviene una base de grafos además de la documental. En Knock las relaciones son listas simples por persona: los técnicos de confianza que guardó, sus contactos de confianza y su lista de bloqueados. No hay recorridos de varios saltos, como los amigos de amigos, así que una tercera base no aporta y sí cuesta.

### 5.3 Qué guarda cada servicio
El servicio de Identidad y confianza usa la base identidad. Guarda a los usuarios, con su celular único, nombre, foto, colonia y código postal, los consentimientos con su versión y fecha, el estado de la cuenta y la marca de eliminación programada. Guarda las direcciones, con calle, número, colonia y referencias, cifradas a nivel de campo; los autos y los datos de la casa; los contactos de confianza, con el nombre y el celular de las personas a las que se avisa de una visita; las sesiones, una por dispositivo, con el token de renovación guardado como huella y su fecha de vencimiento; las verificaciones, con el técnico, el nivel, el estado, quién decidió, el motivo, las fechas y la huella de la CURP, sin las fotos, que viven cifradas en un almacenamiento privado; los reportes, con quién reporta, a quién, el motivo, la gravedad, la evidencia, la decisión y la apelación; y los bloqueos.

El servicio de Mensajería y notificaciones usa la base mensajeria. Guarda las conversaciones, una por solicitud y técnico, con las dos personas, el estado y el último mensaje; los mensajes, de texto o foto, con remitente, hora, enviado y leído; los avisos que ve la persona dentro de la app; las preferencias de avisos; y los dispositivos, con el identificador de notificaciones de cada teléfono.

El servicio de Mercado usa la base mercado. Guarda los oficios, con su modalidad de taller o domicilio, y los oficios pedidos por quienes escribieron "Otro oficio"; las colonias, con su nombre, código postal y el polígono de cada una, tomado de SEPOMEX; los técnicos, con su negocio, oficios, forma de atención, radio, ubicación aproximada, descripción, años de experiencia, horarios, disponibilidad, estado de verificación, nivel y datos de reputación; los servicios de cada técnico, con nombre, detalle y precio de referencia; las solicitudes, con el cliente, categoría, descripción, fotos, día y franja, colonia o dirección, estado y vencimiento; las cotizaciones, con la solicitud, el técnico, el tipo de precio, el precio, el tope, el día y la hora propuestos, la nota y el estado; las visitas y citas, con la cotización elegida, el código de llegada guardado como huella y de un solo uso, la hora de llegada, el estado y, al terminar, las fotos y la descripción; los ajustes de precio, con su motivo y la decisión; las opiniones, con la calificación general y por partes, el comentario, las fotos y la respuesta del técnico; los técnicos de confianza de cada cliente; y la lista de espera de quienes están fuera de cobertura.

El módulo Asistente, que vive dentro del servicio de Mercado, guarda en la base mercado las bitácoras, que son los resúmenes que el técnico guardó para verlos al iniciar el trabajo, y el uso de IA, que registra cuánto ha consumido cada persona para aplicar el límite de gasto. En todos los servicios existen además las colecciones outbox, con los eventos por entregar, y tareas programadas, con los plazos por ejecutar.

### 5.4 Qué índices llevan
Una regla de la constitución es que todo campo por el que se filtra u ordena lleva índice. En usuarios, el celular es único. Las sesiones y los tokens llevan un índice con vencimiento automático, llamado TTL, para que la base borre sola lo vencido. Las colonias llevan un índice geoespacial sobre el polígono y otro por código postal. Los técnicos llevan un índice geoespacial sobre la ubicación, un índice compuesto por oficio, disponibilidad y verificación, y un índice de texto en español para el autocompletado. Las solicitudes se indexan por cliente, estado y fecha, y por vencimiento para el proceso que las vence. Las cotizaciones, por solicitud y por técnico y estado. Las opiniones, por técnico y fecha. Los mensajes, por conversación y fecha. Los reportes y las verificaciones, por estado, gravedad y fecha, para las colas del panel. Y las tareas programadas y la bandeja de salida, por estado y fecha de ejecución. Las hojas de cálculo de endpoints dan, para cada pantalla, las tablas reales, sus campos, la llave y el campo indexado.

### 5.5 Entrega del proyecto
La evaluación es el 2 de diciembre de 2026. Se entrega el proyecto comprimido en formato .rar, junto con el script de la base de datos, que es un archivo que crea las colecciones, los índices y los datos de ejemplo para poder armar la base desde cero. Ese script se genera y se prueba durante el desarrollo y no al final. Quien no ayude en el proyecto debe hacerlo por su cuenta.

## 6. Reglas de negocio
Estas son las reglas que el backend aplica. Casi todas vienen de decisiones del equipo sobre cómo debe funcionar Knock.

### 6.1 A quién llega una solicitud
Una solicitud general, la que se crea desde el mapa, Explorar o el asistente, se reparte por oleadas. La primera llega a hasta cinco técnicos que tengan "Disponible hoy" activado: los tres mejor ordenados, un técnico nuevo, para que quienes empiezan también reciban solicitudes, y uno de los más cercanos. Si pasan 15 minutos y hay menos de dos cotizaciones, la solicitud se amplía a los siguientes técnicos. El cliente ve como máximo cinco cotizaciones.

Si la solicitud nace en el perfil de un técnico, o en "Volver a contratar", va solo a esa persona, con un interruptor "También pedir a otros técnicos cerca" que activa el reparto por oleadas. Si el técnico elegido no responde, la app ofrece ampliar. Una solicitud vence a las 48 horas, y el cliente puede cancelarla antes. Además lleva el día y la franja, que puede ser mañana, tarde o noche, que el cliente prefiere.

### 6.2 Precio, tope y visita previa
Cada cotización puede ser de precio fijo, de rango o de visita previa, que se llama "Ver primero". La cotización dice "No pasará de tanto sin tu aprobación", y ese tope se calcula así: para un rango, su extremo alto más 20 por ciento en electricidad y 25 por ciento en plomería y mecánica; para un precio fijo, más 10 por ciento. Nunca se pide aprobación por una diferencia menor a 100 pesos.

Si el técnico descubre que hace falta cobrar más que el tope, propone el cambio con un motivo, en cuanto detecta el problema y no al terminar. El cliente lo aprueba o lo rechaza. Si lo rechaza, el trabajo se cancela sin costo, o, si ya hubo visita, solo se paga la visita.

Cuando el técnico no puede estimar a distancia, cotiza "Ver primero". La visita cuesta hasta 250 pesos y se descuenta si el cliente contrata. La visita se cobra aunque el cliente rechace la cotización final, pero solo si la visita se hizo y se entregó la cotización final; no se cobra si el técnico no llegó o no cotizó. Después de la visita el técnico envía la cotización final, que el cliente aprueba en la app.

El monto máximo de la visita, los topes por categoría y el piso de 100 pesos no son valores fijos en el código, sino configuración que el equipo puede ajustar por ciudad y categoría, con historial de cambios. Como la app no maneja pagos, la visita se paga directo, y Knock deja constancia con el código de llegada y la cotización final.

### 6.3 Elegir una cotización
Al elegir una cotización, la cita queda confirmada con el día y la hora que propuso el técnico. Se revelan el teléfono y, según el caso, la dirección exacta del taller o se comparte la dirección del cliente con ese técnico. Se cierran las conversaciones con los demás técnicos, con un aviso, y cada chat muestra arriba la solicitud y la cotización de esa persona. Si es a domicilio, se genera el código de visita.

### 6.4 Visita y cita
El código de visita es de cuatro dígitos, aleatorio, de un solo uso y con vencimiento, y se guarda como huella y no en claro. El cliente se lo pide al técnico en la puerta, el técnico lo escribe en su app y el servidor lo compara. Un código incorrecto bloquea tras varios intentos, y si no coincide la app avisa "El código no coincide, confirma la dirección". Al validar el código se registra la hora real de llegada. En la cita en taller no hay código: se muestra el día, la hora y la dirección del taller con "Cómo llegar".

Para reprogramar, cualquiera de las dos partes propone otra fecha y la otra confirma. El cliente o el técnico pueden cancelar, con motivo obligatorio. Con más de 24 horas de anticipación es gratis; con menos queda como cancelación tardía en el historial y se avisa de inmediato a la otra parte. El reloj se calcula en el servidor con la hora real de la cita. Si el técnico llegó y esperó y el cliente no estaba, lo registra sin penalizar al cliente, y queda como evidencia por si hay un reporte; solo se puede registrar dentro de la ventana de la cita.

Para terminar, el técnico sube fotos obligatorias, de al menos una, y describe lo que hizo. No hay confirmación del cliente ni autoconfirmación a las 72 horas. Las fotos del trabajo pasan al perfil del técnico y a Explorar solo con permiso del cliente. Si el trabajo no quedó terminado, el cliente puede reportar "no se terminó" hasta siete días después.

### 6.5 Opiniones y reputación
Solo puede opinar quien tuvo un trabajo terminado, y hasta 30 días después. La opinión lleva una calificación general de uno a cinco, que es obligatoria, y opcionalmente puntualidad, calidad, precio justo y trato, cada una de uno a cinco, además de un comentario y hasta tres fotos. Sale como "Servicio verificado", y el técnico puede responderla.

Los niveles del técnico dependen de los trabajos terminados por Knock: Nuevo con cero, Confiable con uno, Experto Knock con cinco y Maestro Knock con quince.

Cuando un técnico no sabe cuánto cobrar, ve la referencia del mercado, que es el rango de lo que cobran otros técnicos por trabajos parecidos. Se calcula con las cotizaciones aceptadas en los últimos 90 días, de la misma ciudad, categoría y tipo de trabajo, de forma anónima y agregada, del percentil 25 al 75 y la mediana. Solo se muestra si hay al menos diez cotizaciones; si no, se usa el rango general de la categoría. Nunca se muestran datos de un técnico en particular. El orden "Recomendadas" lo calcula el aprendizaje automático según el cumplimiento del técnico, como se explica en la sección 11.

### 6.6 Verificación de identidad
Es opcional y tiene dos niveles: básica, con el frente del INE y la selfie, y completa, con el reverso. Se ofrece la primera vez que el técnico va a enviar una cotización, empezar un trabajo o ponerse disponible, y siempre desde Mi cuenta, en Mi negocio; puede elegir "Seguir sin verificar". Los estados son sin verificar, en revisión y verificado, o "no se pudo verificar", con el motivo y la posibilidad de reintentar.

La revisa una persona desde el panel. Si no la revisa en 24 horas, se alerta al verificador. Si pasan siete días, se borran las fotos y el estado vuelve a "sin verificar" con un aviso para reenviarlas, sin marcarlo como rechazado. Como reglas contra el fraude, el nombre del INE debe coincidir con el del registro y una misma CURP no puede estar en dos cuentas, lo que se comprueba comparando su huella. La selfie se trata como dato sensible por prudencia.

### 6.7 Reportes y sanciones
Cualquier persona puede reportar, y los técnicos pueden reportar a los clientes. La respuesta depende de la gravedad. Un caso crítico, de seguridad, como violencia, acoso, robo o suplantación de identidad, lleva suspensión preventiva inmediata y revisión el mismo día. Un caso alto, como fraude, cobrar distinto a lo acordado, pedir pago por adelantado o que llegue otra persona, deja al técnico oculto de nuevas solicitudes hasta revisar, con un plazo objetivo de 24 horas. Un caso medio, como no presentarse, un trabajo mal hecho o mala comunicación, lleva una advertencia y registro, con un plazo objetivo de 72 horas. Un caso bajo, como un retraso menor o un desacuerdo de precio, lleva mediación sin sanción, con un plazo objetivo de cinco días. Se avisa a las dos partes con el motivo, sin revelar los datos de la otra, y hay una apelación, revisada por otra persona. Se sanciona a quien haga reportes falsos. Si hay peligro inmediato, la app indica llamar al 911.

### 6.8 Bloqueos
Una persona puede bloquear a otra desde el chat, con la opción de deshacer. El bloqueo impide nuevos mensajes y nuevas solicitudes entre ambas.

### 6.9 Cuenta, cambio de número y eliminación
Un celular corresponde a una cuenta. El registro y el inicio de sesión son el mismo flujo: si es la primera vez, se crea la cuenta.

Para cambiar de número, se verifica el número nuevo con un código y se avisa al número anterior; las sesiones anteriores se cierran. Si la persona ya no tiene su número, puede recuperar el acceso: un técnico verificado repite su selfie y se compara con la que ya se validó, y un cliente pasa por soporte, que lo revisa con datos de sus servicios. Si no coincide, se rechaza y se ofrece soporte.

Para eliminar la cuenta, la app muestra lo pendiente y ofrece "Cancelar lo pendiente y eliminar". Se confirma con un código por SMS y hay siete días de gracia para arrepentirse. Al terminar se borran el perfil, el historial y los mensajes. Las opiniones se anonimizan como "Usuario eliminado" en lugar de borrarse, y los reportes se conservan el tiempo que obligue la ley, con acceso restringido. Las citas pendientes se cancelan avisando a la otra parte, y si es un técnico se cierra su perfil público.

### 6.10 Estados de una solicitud
La solicitud pasa por varios estados, y la API no permite saltos no válidos. Empieza enviada, cuando se crea y se reparte a los técnicos. Pasa a cotizando cuando llega la primera cotización. Cuando el cliente elige una cotización pasa a visita, si es a domicilio, o a cita, si es en taller. Cuando el técnico termina el trabajo pasa a terminado, y cuando el cliente deja su opinión, a calificado. Puede pasar a cancelado si una de las partes cancela o si se rechaza un ajuste de precio, y a vencida si pasan 48 horas sin elegir. Para el técnico, la misma solicitud se ve como nueva, luego descartada o en proceso cuando envía su cotización, después aceptada y finalmente terminada.

## 7. Qué hace la API
Todas las rutas van bajo /api/v1, entran por la puerta de entrada y piden sesión, salvo las que se indican. Una lista es una respuesta paginada que admite filtros. El detalle de cada ruta, con sus campos, validaciones y respuestas, está en la hoja de cálculo de endpoints.

### 7.1 Servicio de Identidad y confianza
Para la sesión, iniciar sesión no requiere sesión previa: recibe el token que entregó Firebase tras comprobar el código SMS, lo valida y entrega la sesión propia de Knock, con un token de acceso y uno de renovación; si es la primera vez, crea la cuenta. Renovar la sesión cambia un token de renovación por uno nuevo y descarta el anterior, y si alguien presenta uno que ya se usó, se cierran todas las sesiones de esa cuenta. Cerrar sesión cancela el token de acceso en el servidor, anotándolo en Redis hasta que venza, y cierra la sesión de ese dispositivo. También se pueden ver y cerrar las sesiones de cada dispositivo.

Para la cuenta, se puede ver y editar el perfil con nombre, foto, colonia y código postal; cambiar el número de celular, pidiendo el código y confirmándolo; recuperar el acceso si se perdió el número; descargar los datos propios, que se entregan en 48 horas, y corregirlos; consultar y cambiar los consentimientos opcionales, con la versión del aviso y la fecha; y eliminar la cuenta, viendo lo pendiente, confirmando con SMS, programando la eliminación y cancelándola durante los siete días de gracia. Los datos del cliente, como direcciones, autos, datos de la casa y contactos de confianza, se pueden crear, ver, editar y borrar.

Para la verificación, el técnico envía las fotos con su consentimiento y consulta el estado. El personal interno, con el rol de verificador y doble verificación, ve la cola de pendientes, con filtros por estado y antigüedad, abre las fotos por un enlace de cinco minutos, y cada apertura queda registrada, y decide aprobar o rechazar con un motivo.

Para los reportes y bloqueos, se puede crear un reporte con motivo, texto y evidencia, ver los reportes propios y apelar. El personal de moderación ve la cola de reportes, ordenada por gravedad, decide y registra la sanción. Cualquiera puede bloquear y desbloquear a otra persona.

### 7.2 Servicio de Mensajería y notificaciones
Las conversaciones se listan con su último mensaje y los mensajes sin leer, una por solicitud y técnico. Los mensajes de una conversación se consultan con paginación. Se puede enviar un mensaje de texto o con foto, que antes de guardarse pasa por la moderación, y marcarlo como leído. Hay un canal de chat en tiempo real con mensajes al instante, "escribiendo…", presencia, con "En línea" y su tiempo de respuesta, y las marcas de enviado y leído. Los avisos se listan y se pueden marcar uno o todos como leídos, y se puede elegir cuáles recibir. Se puede registrar o quitar el identificador de notificaciones del teléfono, y el servicio envía las notificaciones push de los procesos importantes, que se detallan en Aplicación móvil, sección 4.

### 7.3 Servicio de Mercado
Entre los catálogos, se pueden consultar los oficios con su modalidad, registrar un "otro oficio" pedido y, para administración, crear y editar oficios. Se pueden consultar las colonias, con filtro por código postal, y resolver la colonia de un punto o de un código postal. También se puede registrar a alguien fuera de cobertura en la lista de espera.

Para la búsqueda y el descubrimiento, se buscan técnicos con filtros por oficio, colonia, distancia o radio, "Disponible hoy" y solo verificados, con texto libre y orden por distancia, calificación o "Recomendadas". El autocompletado de oficios y servicios responde mientras se escribe. El mapa devuelve los técnicos de la zona visible, ya agrupados para no mandar todos de golpe. Explorar devuelve los bloques de la pestaña, respetando el filtro elegido. El perfil de un técnico devuelve solo el resumen a quien no ha iniciado sesión, y con sesión devuelve además los servicios y precios, las opiniones, las colonias que atiende, las fotos de trabajos y los horarios. Las opiniones y las fotos de trabajos de un técnico se consultan en listas paginadas.

Para el perfil y la cuenta del técnico, se puede registrar como técnico y editar el perfil público, cambiar la disponibilidad, administrar horarios y servicios con su precio de referencia, y ver su resumen de trabajo y su nivel. El cliente puede guardar, ver y quitar técnicos de confianza.

Para las solicitudes y cotizaciones, el cliente crea una solicitud con categoría, descripción, fotos, día y franja, y dirección si es a domicilio; ve sus solicitudes y el detalle de cada una; puede cancelarla o ampliarla a más técnicos. El técnico ve las solicitudes nuevas, con la colonia, las fotos, cuántos técnicos ya cotizaron y cuándo vence, y puede descartar una. Cotizar permite enviar precio fijo, rango o "Ver primero". El cliente compara las cotizaciones de una solicitud, con orden y etiquetas, y elige una. El técnico consulta la referencia del mercado. Los cambios de precio los propone el técnico y los aprueba o rechaza el cliente, y después de la visita previa se envía la cotización final, que también se aprueba o se rechaza.

Para las visitas y citas, se puede ver una visita o cita y la agenda del técnico, validar el código de llegada, que el servidor compara, reprogramar proponiendo y respondiendo, cancelar y registrar al cliente ausente. Avisar a alguien de confianza arma el mensaje con quién va, cuándo y la identidad verificada, sin el código. El técnico termina el trabajo con fotos y descripción, y el cliente puede reportar "no se terminó".

Para las opiniones, el cliente deja una opinión sobre un trabajo terminado y el técnico la puede responder. Para los archivos, se obtiene un enlace temporal y firmado para subir una foto de solicitud o de trabajo terminado, y la foto se revisa antes de guardarse.

### 7.4 Módulo Asistente (dentro del servicio de Mercado)
El asistente Knock conversa con el cliente: pregunta qué pasa y desde cuándo, exige una foto, clasifica la categoría y arma la solicitud, y si detecta un riesgo, por ejemplo gas, muestra primero las instrucciones de seguridad. También resume un chat, devolviendo lo que se sabe, lo que falta por confirmar y qué llevar. Prepara el borrador de cotización, con precio, tiempo, día y nota, y explica qué tomó en cuenta. Sugiere un rango de precio con aprendizaje automático y permite guardar el resumen en la bitácora del técnico.

## 8. Seguridad

### 8.1 Inicio de sesión
La persona entra con su celular y un código por SMS, sin contraseña, lo que hace Firebase Authentication. Por eso no existen funciones de contraseña ni de recuperarla. El servicio de Identidad recibe el token que entrega Firebase, lo valida con las claves públicas de Google y emite su propia sesión, así que el backend no depende de dónde corra Firebase.

El token de acceso, un JWT, vive 15 minutos y está firmado con HMAC-SHA256 con una clave de al menos 32 bytes guardada en Key Vault. Se valida el firmante, el destinatario, la firma y el vencimiento, y solo se acepta ese algoritmo. El token de renovación vive 30 días y rota: cada vez que se usa, se entrega uno nuevo y el viejo deja de servir. Se guarda en MongoDB como huella. Si alguien intenta usar uno ya usado, que es señal de robo, se cierran todas las sesiones de esa cuenta. Hay dos tokens porque uno de vida corta limita el daño si lo roban, y el de renovación evita pedir el código SMS a cada rato.

### 8.2 Cerrar sesión en el servidor
Un requerimiento establece que cerrar sesión solo en la app no basta, porque hay que invalidar el JWT en el backend. Cuando la persona sale, la app llama al endpoint de logout. El servidor guarda el identificador del token en Redis, con una vida igual al tiempo que le quedaba. Desde ese momento, la puerta de entrada consulta Redis en cada petición y rechaza los tokens anotados, en cualquiera de las copias. También se cancela el token de renovación de ese dispositivo. Se usa Redis porque la consulta es inmediata, el dato se borra solo al vencer y lo comparten todas las copias.

### 8.3 HTTPS
Todo el tráfico va cifrado. El certificado lo presenta la entrada de Container Apps. Además, el servidor envía el encabezado HSTS, que obliga a los navegadores a usar HTTPS, y redirige cualquier acceso inseguro. La salida del backend es HTTPS desde el inicio y no se agrega al final.

### 8.4 Límite de peticiones
Los servicios internos no se exponen a internet. La puerta de entrada limita por dirección IP antes de identificar a la persona y por persona después, con contadores en Redis para que valga igual en las dos copias. Cada servicio limita por la dirección IP original solo si confía en la puerta de entrada, como segunda defensa. Hay reglas más estrictas para el inicio de sesión y para pedir códigos SMS, para que no se abuse del envío de mensajes, que cuesta dinero. Los valores están en configuración y no en el código. Al exceder el límite, el servidor responde con el error estándar de demasiadas peticiones e indica cuándo reintentar.

### 8.5 Cadena de pasos de cada petición
Cada petición pasa siempre por los mismos pasos, en este orden: lectura de la dirección real, identificador de correlación, encabezados de seguridad, manejo de errores, registro de la petición, HSTS, redirección a HTTPS, CORS, límite de peticiones, límite del tamaño del cuerpo, identificación, autorización y, al final, el controlador. El orden está fijado y una prueba lo vigila.

### 8.6 Permisos
Hay permisos por rol, que son persona usuaria, técnico, verificador, moderador y administrador, y las funciones internas exigen el rol y doble verificación. También hay permisos por recurso: cada consulta comprueba que el recurso es de quien lo pide, de modo que un cliente nunca ve las solicitudes de otro y un técnico nunca ve mensajes ajenos. Esto se prueba con pruebas negativas por cada recurso. La seguridad sigue las guías OWASP Top 10 y OWASP API Security Top 10.

### 8.7 Secretos
Ninguna clave, contraseña ni cadena de conexión se escribe en el código. En ejecución viven en Key Vault, leídos con la identidad administrada de cada servicio. En los procesos automáticos viven en los secretos de GitHub, por ejemplo la clave de firma de la app o las credenciales para entrar a Azure, y los valores que no son secretos en las variables de GitHub. Como los repositorios son públicos, si una clave se filtra se cancela y se cambia de inmediato, y una herramienta automática busca claves olvidadas en cada cambio.

### 8.8 Datos sensibles
Las fotos de INE y selfie se guardan en un contenedor privado y separado, cifradas con una llave propia por verificación. La llave se destruye al decidir o a los siete días, así que las fotos quedan ilegibles aunque existan copias de seguridad, y solo se abren con enlaces de cinco minutos y por quien tiene el permiso de revisión. La CURP nunca se guarda en claro, sino una huella calculada con una clave secreta guardada en Key Vault, porque un hash sin clave se podría adivinar probando combinaciones. La dirección y el teléfono van cifrados, y el técnico solo ve la colonia hasta que el cliente lo elige. La ubicación del teléfono no se guarda: se convierte a colonia y se descarta.

Cada vez que alguien abre una foto sensible queda anotado quién, cuándo y cuál, en un registro que solo se puede agregar y no editar. En desarrollo y en la prueba cerrada no se usan INE ni selfies reales, solo imágenes de ejemplo, y los datos reales solo se tratan en un lanzamiento de verdad, con revisión legal. La IA nunca recibe INE, CURP, dirección ni teléfono, y un filtro previo enmascara o rechaza esos datos.

### 8.9 SMS
El inicio de sesión envía códigos por SMS y cada uno cuesta, así que hay controles. Solo se permiten números de México. Hay un límite de intentos por código, un vencimiento de pocos minutos y un solo uso, además de un límite de envíos por número y por dispositivo. Para enviar SMS reales, Firebase exige un plan de pago por uso, llamado Blaze, con tarjeta. Lo activaremos antes de abrir la prueba cerrada de Google Play, porque los testers son más de los 10 números de prueba permitidos, con presupuesto, alertas de gasto y revisión semanal. Mientras tanto se usan los números de prueba de Firebase, que son máximo diez y no envían mensajes reales.

## 9. Cómo hacemos que todo sea rápido
La app se usa en teléfonos, y en muchos lugares la señal es mala, por eso cada consulta debe ser ligera. Se estableció como regla para la constitución: todo acceso a datos usa caché.

### 9.1 Caché en tres niveles
En el nivel de base de datos, todo acceso a datos pasa por una capa que primero busca en la caché de Redis y solo va a MongoDB si no está. Cuando se guarda o cambia algo, se borra de la caché para que nadie lea datos viejos. Además, cada consulta usa los índices adecuados y trae solo los campos que necesita y no el documento completo.

En el nivel de backend, las respuestas de lectura se guardan en caché y llevan una marca de versión llamada ETag. Cuando la app vuelve a pedir lo mismo, el servidor compara la marca, y si no cambió contesta "sin cambios" sin reenviar el contenido. El nivel de la app se describe en la sección Aplicación móvil.

### 9.2 Qué se cachea y qué no
Se cachea lo que cambia poco: los catálogos, como oficios y colonias, unas 24 horas, los perfiles de técnico, un minuto, y los resultados de búsqueda, unos segundos. Estos valores son iniciales y se ajustan con las pruebas de carga. No se cachea lo personal que cambia al crear algo, como las solicitudes, las cotizaciones y los mensajes, porque una lista cacheada mostraría datos viejos justo después de crear un registro. Para esas listas se confía en los índices.

### 9.3 Paginación
Todos los listados devuelven poco a poco. La página siguiente se pide desde el último elemento visto, por cursor, y no contando desde el principio, porque contar desde el principio se vuelve más lento mientras más profundo se pagina. Cada página trae 20 elementos y nunca más de 50.

### 9.4 Otras técnicas
Los índices cubren todo campo por el que se filtra u ordena, incluidos los geoespaciales. Los serializadores de JSON se generan al compilar, lo que reduce el tiempo de procesador y la memoria frente a los que trabajan en ejecución. Las respuestas JSON de más de 1 KB se comprimen, salvo las de inicio de sesión y las que llevan tokens. Las fotos se reducen en el teléfono antes de subirlas: el avatar a 320 píxeles y las fotos de solicitud a 900. Los filtros, el autocompletado y las búsquedas se resuelven en el servidor con índices y con un tiempo máximo de respuesta. Y un middleware común se ocupa de los errores, el identificador de correlación, la compresión y la caché.

## 10. Imágenes y contenido inapropiado

### 10.1 Subida de imágenes
Las fotos se suben directamente a Blob Storage con un enlace temporal y firmado de cinco minutos, para que el servidor no tenga que cargar con los archivos. Al subirlas se les quitan los metadatos de ubicación y se reducen de tamaño. Las imágenes públicas, como los avatares y las fotos de trabajos terminados, se sirven por la CDN, mientras que las fotos de verificación son privadas y no pasan por ella.

### 10.2 Moderación antes de guardar
Un requerimiento es evaluar con la API de la nube, antes de almacenar, si lo que se sube es inapropiado, y no permitir que se suba si lo es. En Knock, todo texto, como descripciones, mensajes, opiniones y nombres del negocio, y toda imagen, como fotos de solicitudes, de trabajos y avatares, se envía a Azure AI Content Safety antes de guardarse. Si se marca como inapropiado, por ejemplo por violencia, incitación a hacerse daño o a dañar a otros, o groserías, no se guarda y la app muestra un aviso claro. Un cliente de moderación compartido lo invoca desde cada servicio. Si el servicio de moderación no responde, el contenido no se publica hasta poder revisarlo, y no se deja pasar. En esta versión no hay video, y si se agrega pasa por el mismo filtro. Las fotos de INE y selfie no pasan por la revisión automática, porque las revisa una persona y por privacidad no salen a otro servicio.

## 11. Inteligencia artificial y aprendizaje automático

### 11.1 IA generativa
La IA generativa usa Azure OpenAI y vive en el módulo Asistente, dentro del servicio de Mercado. El asistente Knock conversa con el cliente que no sabe qué oficio necesita: pregunta qué pasa y desde cuándo, exige una foto, clasifica la categoría y arma la solicitud, y se rotula "no es una persona". El resumen del chat es para el técnico e indica lo que se sabe, lo que falta confirmar y qué herramientas y materiales llevar, y puede guardarse en su bitácora. El borrador de cotización propone precio, tiempo, día y nota, ajustados a lo conversado, como el tipo de trabajo, la urgencia, si lleva materiales o el día que pidió el cliente; explica qué tomó en cuenta, avisa que puede equivocarse, y el técnico lo edita y lo envía.

### 11.2 Aprendizaje automático
El aprendizaje automático usa ML.NET. Calcula el orden "Recomendadas", que pondera el cumplimiento del técnico, es decir, que llegue a la hora, que termine, sus calificaciones y su respuesta rápida, y sugiere un rango de precio para el borrador de cotización y para comparar. Mientras no haya datos reales suficientes se usan datos de ejemplo y reglas de respaldo.

### 11.3 Reglas de seguridad de la IA
La IA propone y una persona decide: nunca cambia estados de solicitudes ni citas, aprueba verificaciones, resuelve reportes, sanciona cuentas ni envía una cotización por sí sola. Responde en un formato fijo, un JSON con esquema, y no en texto libre. No recibe datos personales innecesarios y nunca INE, CURP, dirección ni teléfono. Se registra lo que se le envió y lo que respondió. Hay un límite de costo por persona para que nadie agote el presupuesto, y una respuesta de respaldo si el servicio falla. Si el texto o la foto sugieren un riesgo, como gas, la respuesta empieza con las instrucciones de seguridad.

### 11.4 Límite real de la suscripción
Hoy la cuota de los modelos de chat de Azure OpenAI es cero en todas las regiones que podemos usar, así que todavía no se puede crear el servicio de chat. Pediremos un aumento de cuota en el portal. Mientras tanto, el asistente, el resumen y el borrador funcionan con plantillas y reglas locales, con la misma interfaz, y cambiarán a Azure OpenAI cuando haya cuota. Eso es solo un puente: la IA generativa es obligatoria para la entrega, así que si tres semanas antes de la evaluación la cuota sigue en cero, se elige otro proveedor generativo, con el mismo filtro de datos personales, y las plantillas solo responden cuando el proveedor falla. La app no se entera del cambio.

## 12. Registro de eventos y errores
La observabilidad es un servicio en la nube que guarda los eventos y errores del sistema. Knock tendrá varias piezas y varios servicios de Azure, y cuando algo falla, buscar el error en cada una sería lento, mientras que con un registro central se ve en un solo lugar dónde falló. Los tres servicios y la puerta de entrada envían sus registros a Application Insights, con Log Analytics detrás.

Cada petición lleva un identificador de correlación, llamado X-Correlation-Id, que se pasa de un servicio a otro, de modo que se puede reconstruir todo su recorrido. Los registros están ordenados, con nivel de gravedad, y no incluyen datos personales. Hay alertas para picos de errores y para el consumo del presupuesto. Se consultan desde el portal o desde la terminal con el CLI de Azure, y se conservan poco tiempo para no consumir la cuota gratuita.

## 13. Pruebas y revisiones de seguridad
En cada cambio corren, de forma automática y sin intervención, las siguientes comprobaciones.

Primero se revisan el formato y el análisis estático, para que el código respete el estilo y no tenga advertencias. Las pruebas unitarias revisan cada pieza por separado, y las de arquitectura comprueban que ningún módulo viole las fronteras, por ejemplo un servicio leyendo la base de otro. Las pruebas de integración comprueban que todo funcione junto, con las bases de datos y el emulador de Firebase de verdad levantados con Testcontainers. Las pruebas de seguridad son casos negativos: entrar sin sesión, acceder a recursos ajenos, enviar datos maliciosos. Las pruebas de contrato comprueban que el archivo OpenAPI regenerado no tenga cambios incompatibles con el anterior, y las de la base de datos verifican los índices, las semillas y las restricciones. La cobertura mínima es de 60 por ciento de líneas y 50 por ciento de ramas, y si baja, el cambio no pasa.

Las pruebas de mutación, hechas con Stryker, cambian el código a propósito para comprobar que las pruebas lo detectan, y se corren sobre lo que cambió. Las pruebas de rendimiento, hechas con k6, miden los tiempos de respuesta con las dos copias y comprueban el balanceo. El fuzzing envía entradas aleatorias o inesperadas a la API para encontrar fallos, y las pruebas de carga y de caos comprueban cómo reacciona el sistema cuando una copia cae.

Además hay revisiones de seguridad. El análisis del código busca patrones inseguros, el de dependencias busca librerías con vulnerabilidades conocidas y revisa licencias, y otra herramienta busca claves olvidadas en el código. Los contenedores se revisan para encontrar vulnerabilidades y malas prácticas en las imágenes, y la infraestructura se revisa en los archivos de Terraform y los procesos automáticos. Un escaneo dinámico ataca el sistema ya desplegado en pruebas. Por último, el gobierno del proceso verifica el nombre de la rama, el formato de los mensajes, la plantilla del cambio con la sección de uso de IA y que cada cambio tenga su especificación.

## 14. Del código a la nube
El requerimiento se resume así: desde el desarrollo en nuestra computadora hasta montar el endpoint y la aplicación, todo lo más automático posible.

En la integración continua, en cada solicitud de cambio el proceso corre las comprobaciones de la sección anterior. Detecta qué servicio cambió y construye y prueba solo ese, para ahorrar tiempo. En la entrega continua, cuando el cambio se aprueba y se fusiona, el proceso construye la imagen del servicio, la publica en el registro, la despliega a producción en Azure, el único entorno, desde la rama principal. Las pruebas completas corren antes en GitHub Actions, y una rama de estabilización recibe los cambios antes de la rama principal. Después de cada despliegue se hace una prueba de humo que llama a las rutas de estado y a un par de funciones básicas. Además hay una forma de volver a la versión anterior si algo sale mal, que se prueba antes de la evaluación.

Cada despliegue aplica la numeración por puntos de la descripción general. La creación y el borrado de la infraestructura se hacen con los cuatro pasos de Terraform de la sección 2. Y los secretos del proceso viven en los secretos de GitHub, nunca en el código.

---

# APLICACIÓN MÓVIL

## 1. Con qué la hacemos

### 1.1 Tecnología
La app se hace con Flutter y está pensada para Android. La evaluación empieza descargando la app desde Google Play. Elegimos Flutter porque con un solo código se obtiene una interfaz consistente, porque tiene buenos paquetes para mapas, cámara, notificaciones y almacenamiento seguro, y porque encaja con los requerimientos de aplicaciones móviles.

### 1.2 Cómo está organizada
Cada función de la app, que son Inicio, Explorar, Actividad, Trabajo y Cuenta, se organiza en tres capas. La capa de presentación contiene las pantallas y los controladores que preparan lo que se ve; cada pantalla tiene un estado claro, que puede ser cargando, con datos, vacía o con error, y solo se dibuja a partir de él. La capa de dominio contiene las entidades y las reglas, escritas en Dart puro, sin depender de pantallas ni de la red, y se prueban sin abrir la app. La capa de datos contiene los repositorios, que hablan con la API y con la caché local. Se organiza así para que las reglas se prueben sin pantallas y las pantallas cambien sin tocar las reglas.

### 1.3 Piezas comunes
El código que llama a la API se genera automáticamente desde el archivo OpenAPI del backend y nadie lo escribe a mano, de modo que si el backend cambia, la app deja de compilar hasta ajustarse. Un componente común de sesión agrega el token a cada petición, lo renueva cuando vence y cierra la sesión si ya no es válido. Un manejo común de errores traduce los errores de la API a mensajes entendibles en español. Todos los textos viven en archivos de traducción, en español e inglés, desde el inicio, y no escritos en las pantallas. El tema puede ser claro, oscuro o automático, con la paleta de la maqueta. Los tokens de sesión se guardan en el almacenamiento seguro del teléfono y no en archivos normales. Un enrutador común maneja las pestañas y los avisos, y si la persona no ha iniciado sesión y toca algo que lo exige, la app explica por qué y, al entrar, regresa a lo que estaba haciendo.

## 2. Las pantallas
La app reproduce la maqueta. Solo se pueden agregar funciones y nunca quitar. Un técnico es un cliente con una pestaña más. Sin cuenta no hay barra de pestañas: se navega desde el mapa y un botón "Mapa" regresa al inicio.

### 2.1 Entrada y cuenta

Explorar sin cuenta. Se puede ver el mapa y los técnicos sin registrarse. El perfil de un técnico se muestra resumido, con foto, verificación, calificación, experiencia y una descripción corta. Los servicios y precios, las opiniones, las colonias o la ubicación y las fotos aparecen difuminados con contenido de relleno, detrás del mensaje "Iniciar sesión para ver más". El servidor no envía ese detalle a quien no ha iniciado sesión, de modo que el difuminado no es solo visual. Las acciones que comprometen datos piden entrar y explican por qué, por ejemplo "Para pedir tu cotización, entra con tu celular": pedir cotización, escribir a un técnico, guardar un técnico de confianza y ver la actividad. Después de entrar, la app continúa justo donde se quedó.

Inicio de sesión y registro. Son el mismo flujo y tiene seis pasos. Primero se escribe el celular, de diez dígitos con prefijo +52, y al continuar se acepta el aviso de privacidad y los términos, que se pueden consultar antes. Luego se escribe el código por SMS, de seis dígitos, con un botón para reenviar con cuenta regresiva; si el código es incorrecto, se muestran los intentos restantes. Después se pide el nombre, solo la primera vez, con mínimo dos caracteres y solo letras. Sigue la zona, que se puede omitir con "Ahora no". Luego una foto de perfil opcional, que sin foto se sustituye con las iniciales como avatar y que se reduce a 320 píxeles. Al final se muestra "¡Listo, ya entraste!" y la app retoma lo que se estaba haciendo. La dirección no se pide al registrarse, sino la primera vez que se solicita un servicio a domicilio, y queda guardada.

Zona. La persona puede usar su ubicación, escribir su código postal o saltar el paso; el campo del código postal está oculto tras "O escribir mi código postal" para que la opción principal sea la ubicación. Con la ubicación, la app pide el permiso, calcula la colonia más cercana y la muestra para confirmar; si la persona está fuera de Tula de Allende, se avisa y se ofrece una zona de ejemplo. Con el código postal, si no empieza con el prefijo de la zona se avisa "solo Tula de Allende y alrededores", si es conocido se muestra su colonia, y si es de la zona pero desconocido se ofrece una zona aproximada. Solo se guarda la colonia, y el código postal si se escribió, nunca las coordenadas. Fuera de cobertura se ofrece la lista de espera.

Cambiar de número y recuperar el acceso. Desde Mis datos, en "Cambiar mi número", se escribe el número nuevo y se recibe un código; al confirmar se avisa al número anterior y se cierran las sesiones anteriores. Al entrar, el enlace "¿Ya no tienes ese número?" abre la recuperación. Un técnico verificado repite su selfie, que se compara con la que ya se validó; un cliente pasa por soporte, que lo revisa con datos de sus servicios. Si no coincide, se rechaza y se ofrece soporte.

Registrarse como técnico. Se hace desde Mi cuenta, en "Ofrece tus servicios en Knock", o desde el inicio de sesión, en "¿Ofreces servicios?". Se pide el nombre y apellido, solo con letras, y el celular, que si ya es de un cliente se reutiliza. Luego se elige qué hace: mecánica, plomería, electricidad u "Otro oficio" con texto libre, y con elegir uno basta. Después se elige cómo atiende: en taller, lo que pide la dirección del taller de al menos ocho caracteres, o a domicilio, lo que pide hasta dónde va, de 3, 5, 8 o 12 kilómetros; la dirección de quien atiende a domicilio no se muestra a los clientes. Hay una parte opcional y plegada llamada "Personalizar mi perfil", con el nombre del negocio, los años de experiencia, de cero a 60, la descripción de hasta 300 caracteres y los servicios con precio de referencia. Se acepta la casilla obligatoria de términos, se confirma con un código por SMS y queda creada la cuenta de técnico, tras lo cual se ofrece "Ver solicitudes cercanas" o "Completar mi perfil".

Verificar la identidad. Se ofrece la primera vez que el técnico va a enviar una cotización, empezar un trabajo o ponerse disponible, y siempre desde Mi cuenta, en Mi negocio, en Verificación de identidad. Puede elegir "Seguir sin verificar" y la acción continúa. Antes de pedir las fotos se muestra, en lenguaje sencillo, qué se hace con los datos y una vista previa del aviso de privacidad. Hay una casilla de consentimiento propia, sin marcar de antemano y separada de los términos, que dice: "Doy mi consentimiento para que Knock guarde cifradas mi INE y mi selfie hasta que una persona autorizada del equipo las revise (máximo 7 días) y las use solo para verificar mi identidad". Se toman las fotos del frente del INE, del reverso en el nivel completo y la selfie, con la cámara, vista previa y la opción de repetir. La app revisa la calidad en el teléfono y rechaza fotos muy pequeñas, oscuras, con reflejo, borrosas o con el INE en vertical, con un mensaje claro. Después muestra el estado: sin verificar, en revisión con el tiempo estimado, verificado con la opción de subir de nivel agregando el reverso, o "no se pudo verificar" con el motivo y consejos para reintentar.

### 2.2 Las cinco pestañas
Un globo flotante de mensajes con contador aparece sobre las pestañas y abre todas las conversaciones.

Inicio es el mapa. Muestra a los técnicos como pines, cada uno con su calificación, y se puede arrastrar, hacer zoom con pellizco o con botones y volver a la ubicación con un botón. Un panel que dice cuántos técnicos hay cerca de la persona tiene tres alturas: colapsado, que solo muestra el título y una flecha, medio y completo, e incluye el acceso a Explorar, el selector de colonia y la lista. Hay filtros por categoría, que son Todos, Mi auto, Agua y tuberías, y Luz y contactos. La búsqueda por texto, por ejemplo "boiler" o "frenos", detecta la categoría y, si no entiende, lo dice y ofrece elegir; si el texto sugiere un riesgo, como gas, muestra primero las instrucciones de seguridad. El botón "No sé cuál" abre al asistente Knock y es visible también en pantallas pequeñas. Para el técnico hay además un chip "Mi agenda" que encuadra el mapa sobre sus visitas, con un pin naranja y la hora, y sus solicitudes nuevas, con un pin blanco; esos pines solo marcan la zona aproximada, y la dirección exacta está en la tarjeta de cada visita.

Explorar sirve para mirar sin una necesidad urgente, y los filtros Todo, Mi auto, Agua y Luz afectan todos sus bloques. Los servicios populares son tarjetas de color con "desde" un precio, como cambio de aceite, boiler, contacto sin corriente o frenos, y al tocar una se abre la solicitud con la categoría y el texto ya llenos. Los trabajos reales son una cuadrícula con fotos de trabajos terminados por los técnicos, solo los que el cliente permitió publicar, y al tocar una se abre el perfil del técnico. Hay filas horizontales de técnicos cercanos y de los mejor calificados, y un bloque de ideas para la casa y el auto con consejos de mantenimiento y el botón "Pedir cotización".

Actividad muestra todos los servicios de la persona en una sola lista, con los que siguen en curso primero y los terminados al final. Cada tarjeta lleva su etiqueta de estado, como "3 cotizaciones listas", "Cita confirmada", "Visita confirmada" o "Terminado", y una acción principal: ver y elegir, ver la visita y su código, calificar o volver a contratar. También muestra el último mensaje del técnico con su contador de no leídos.

Trabajo es solo para técnicos y reúne en una pantalla lo que necesitan. La cabecera dice "Hola" con su nombre, sus oficios y modo de atención, e incluye el interruptor "Disponible hoy", necesario para recibir solicitudes. Un aviso de verificación dice "Verifica tu identidad (opcional)" o "No pudimos verificar…" según el estado. Las próximas visitas muestran la hora, el día, el cliente, la dirección exacta, que solo aparece después de que aceptaron su cotización, el precio y los botones "Cómo llegar" y "Ya llegué". Las solicitudes nuevas muestran la descripción, la foto, la colonia, cuándo la quiere, cuántos técnicos ya cotizaron y cuándo vence, y permiten descartar o chatear y cotizar. Más abajo están las cotizaciones enviadas, con su estado, por ejemplo "esperando respuesta", las terminadas en una sección plegada y un resumen con las cotizaciones enviadas y aceptadas, los trabajos terminados y el nivel.

Mi cuenta se describe en la sección 2.5.

### 2.3 Lo que hace el cliente

Ver el perfil de un técnico. Muestra su presentación, sus servicios con precios de referencia, sus opiniones, si llega a la colonia del cliente, sus horarios con abierto o cerrado por día, las fotos de trabajos, cómo atiende, en taller o a domicilio, y si está verificado con su nivel. Desde ahí se puede pedir cotización, escribirle y guardarlo como técnico de confianza, lo que requiere cuenta.

Pedir una cotización. Es un flujo de tres pasos. El primero es la categoría, que puede ser auto, agua o luz, y se salta si viene de un servicio popular, del perfil de un técnico o del asistente. El segundo es qué pasa: un texto de 8 a 500 caracteres, con frases sugeridas por categoría, y de una a cinco fotos obligatorias, en JPEG, PNG o WebP, de hasta 5 MB y reducidas a 900 píxeles; la foto es obligatoria porque le dice al técnico qué herramientas y piezas llevar, y en mecánica se piden también los datos del vehículo. El tercero es cuándo y dónde: el día, entre hoy y los próximos cinco, la franja de mañana, tarde o noche y la dirección, que solo se pide si el servicio es a domicilio y no hay una guardada; si es en taller, se avisa que llevará el auto. Desde el perfil de un técnico, la solicitud va solo a esa persona, con el interruptor "También pedir a otros técnicos cerca", y si el técnico no responde, la app ofrece "Pedir también a otros". Después de enviar se muestra la confirmación y se explica lo que sigue.

Esperar y comparar cotizaciones. Las cotizaciones llegan y se avisa con una notificación; se pueden comparar hasta cinco. Cada tarjeta muestra la foto, la calificación, la distancia, etiquetas como "Mejor precio" o "Puede ir más pronto", el tipo de precio, el día y la hora propuestos y la nota del técnico. Para poder compararlas, cada tarjeta explica qué cubre su precio: una visita es solo ir a ver el trabajo; un precio fijo es el arreglo completo con mano de obra, con refacciones aparte; y un aproximado es un estimado del arreglo dentro de un rango. Si las cotizaciones son de tipos distintos aparece un aviso, "Mejor precio" nunca se da a una visita y al ordenar por precio las visitas van al final. Se pueden ordenar por recomendadas, menor precio, mejor calificadas o más pronto, y se puede preguntar al técnico por el chat antes de elegir.

Precio con tope. Cada cotización dice "No pasará de tanto sin tu aprobación", y la comparación usa el máximo posible y no solo el precio inicial. Si el técnico propone un cambio mayor al tope, el cliente recibe una notificación con el motivo y puede aprobar o rechazar; si rechaza, se cancela sin costo, o solo se paga la visita si ya se hizo. Si el técnico cotizó "Ver primero", con una visita de hasta 250 pesos que se descuenta si contrata, tras la visita el cliente recibe la cotización final y la aprueba en la app; si la rechaza, paga solo la visita.

Elegir una cotización. Se confirma quién va, cuándo y el precio, con el tope visible. Al confirmar, la cita queda confirmada, se revela el teléfono y las conversaciones con los demás técnicos se cierran con un aviso.

Visita a domicilio. La app muestra el código de cuatro dígitos y el aviso "Pídele a [el técnico] que te diga este código en la puerta. Si no lo sabe, no abras". El código nunca se dicta por teléfono ni por mensaje. Un bloque llamado Seguridad reúne las acciones de avisar a alguien de confianza, ver la identidad verificada del técnico y reportar. Para avisar a alguien de confianza se elige un contacto o se agrega uno nuevo, con el selector de contactos del sistema o tecleando el número, y se envía por WhatsApp o SMS un mensaje con quién va, cuándo y su identidad verificada, y ese mensaje no incluye el código. También se muestran el precio, el tope, el día y la hora confirmados.

Cita en taller. Se muestran el día, la hora, la dirección del taller y el botón "Cómo llegar", que abre el mapa del teléfono con la ruta.

Reprogramar y cancelar. Para reprogramar la cita, cualquiera de las partes propone otra fecha y la otra confirma. Para cancelar el servicio se pide el motivo; con más de 24 horas de anticipación es gratis y con menos queda como cancelación tardía en el historial y se avisa de inmediato al técnico.

Calificar. Se da una calificación general de uno a cinco, obligatoria, y opcionalmente puntualidad, calidad, precio justo y trato, con un comentario, hasta tres fotos y la opción de guardar al técnico como de confianza. Se puede hasta 30 días después y sale como "Servicio verificado". Si el trabajo no se terminó, el cliente puede reportarlo hasta siete días después. También se puede volver a contratar desde un trabajo terminado o desde los técnicos de confianza.

Reportar y bloquear. Al reportar se elige el motivo: "No se presentó", "Llegó otra persona", "Comportamiento inapropiado", "Cobró distinto a lo acordado" u "Otra cosa"; se cuenta qué pasó y se adjunta evidencia. Si hay peligro, se avisa que se llame al 911, y se explica qué pasará y en cuánto tiempo. El bloqueo se hace desde el menú del chat, con una opción para deshacer.

Chat. Permite mensajes de texto y fotos, con presencia, "En línea" o "Fuera de línea" con su tiempo de respuesta, "escribiendo…" y las marcas de enviado y leído. Arriba muestra la solicitud y la cotización de esa persona, y el aviso de no hacer pagos por adelantado. El número queda oculto hasta elegir una cotización. El menú permite ver el perfil, bloquear y reportar. Después de elegir, "Continuar por WhatsApp" es una opción secundaria, y la conversación y su historial se quedan en la app. Hay una sola conversación por técnico, guardada en la cuenta, que sigue antes y después de cotizar. El asistente Knock es otra cosa: es automático, solo arma la solicitud y se rotula "no es una persona".

### 2.4 Lo que hace el técnico

Registrarse y mantener su perfil. El registro es como se describe en la sección 2.1. El perfil público es editable e incluye foto, nombre, negocio, años de experiencia, oficios, forma de atención, radio, descripción, servicios con precios de referencia y horarios con abierto o cerrado por día, y se ve en el inicio, en el perfil y en la búsqueda del cliente. También tiene preferencias de trabajo para recibir solicitudes más afines.

Recibir y cotizar solicitudes. Las solicitudes nuevas muestran la descripción, las fotos del cliente, la colonia y cuándo la quiere, y solo se ve la colonia hasta que la cotización es aceptada. Al chatear y cotizar, el chat tiene respuestas rápidas como "¿me manda una foto?", "¿cuál es su colonia?", "¿qué día le acomoda?" o "llevo los materiales". El resumen con IA, que es opcional, muestra "lo que sabes", "por confirmar" y "qué llevar", y se puede guardar en la bitácora, que aparece al iniciar el trabajo, o descartar. La cotización con IA es un borrador editable que cambia según lo conversado y explica qué tomó en cuenta; marca "Editada por ti", permite "Volver a la sugerencia" y avisa que la IA puede equivocarse, y el técnico siempre revisa y envía. Si no sabe cuánto cobrar, ve la referencia del mercado, con lo que cobran otros técnicos por trabajos parecidos, el rango, lo más común y de cuántas cotizaciones sale, y un botón "Usar este rango"; es solo una referencia y él decide. La cotización puede ser de precio fijo, de rango o "Ver primero", y la primera vez que envía una cotización se ofrece la verificación, que puede omitir.

Atender el trabajo. Las próximas visitas tienen "Cómo llegar" y "Ya llegué", y los botones de llegada y de ausencia solo funcionan dentro de la ventana de la cita. Para iniciar el trabajo se exige el código de cuatro dígitos que le dicta el cliente; si es correcto muestra "Código correcto" y si no, "El código no coincide, confirma la dirección", y si tiene una bitácora guardada, la ve aquí. Si detecta un problema que obliga a cobrar más que el tope, propone el cambio con un motivo en cuanto lo detecta. Para terminar el servicio sube fotos del trabajo terminado, de al menos una y obligatorias, escribe "¿qué hiciste?", que es obligatorio, y puede dejar una recomendación; esas fotos pasan al perfil público y a Explorar, con permiso del cliente. También puede reprogramar, cancelar la visita con motivo o registrar "El cliente no estaba", lo que no penaliza al cliente y queda como evidencia.

Reputación. El técnico ve su calificación promedio, la distribución por estrellas, las opiniones, que puede responder con el botón "Responder", y su nivel, que es Nuevo con cero trabajos, Confiable con uno, Experto Knock con cinco y Maestro Knock con quince. La insignia verde de identidad solo la tienen los verificados. En el mapa dispone de "Mi agenda", descrita en la sección 2.2.

### 2.5 Mi cuenta
Mis datos permite ver y editar el nombre, la dirección opcional, la colonia y la foto; el celular se cambia con el flujo de cambio de número. En Mi casa y mi auto, el cliente registra su auto con marca, modelo y año y datos de su casa, y el historial del auto sale de sus trabajos reales. Mis técnicos de confianza muestra los guardados con "Volver a contratar". Avisos reúne las cotizaciones, visitas, solicitudes aceptadas y opiniones, y si es técnico se ven juntos con los encabezados "Como cliente" y "Como técnico". Configuración permite elegir el idioma, español o inglés, la apariencia, clara, oscura o automática, y qué avisos recibir. Privacidad y datos permite descargar los datos propios, que se entregan en 48 horas, corregirlos, manejar los consentimientos opcionales de promociones, encuestas y recomendaciones, y ver las sesiones y dispositivos. El Centro de ayuda tiene preguntas frecuentes por tema, como cómo funciona, seguridad y verificación, y la opción "Hablar con soporte". Legal contiene el aviso de privacidad y los términos y condiciones. Mi negocio, para técnicos, reúne el perfil, la verificación de identidad, los horarios y los servicios. Y desde aquí también se cierra la sesión y se elimina la cuenta, como se describe en la sección 3.

## 3. Sesión, cerrar sesión y eliminar la cuenta

### 3.1 La sesión siempre está viva
Un requerimiento es que la sesión permanezca abierta hasta que la persona haga logout. En Knock, la persona entra una vez y, mientras use la app, esta renueva el acceso sola con el token de renovación, sin pedir otro código por SMS. La sesión se pierde únicamente si la persona hace logout, si el servidor detecta algo raro, por ejemplo un token de renovación reutilizado, o si pasan 30 días sin abrir la app. Todo esto ocurre sin que la persona lo note, con la pantalla que ya estaba viendo.

### 3.2 Cerrar sesión en la app y en el backend
La persona toca "Cerrar sesión" y la app pide confirmación, aclarando que sus trabajos y mensajes se conservan para cuando vuelva a entrar. La app llama al servidor, que cancela el token y cierra la sesión de ese dispositivo. Luego la app borra del teléfono los tokens, la caché local y el identificador de notificaciones de ese dispositivo, y regresa a la pantalla de inicio. Si no hay conexión al cerrar, la app igualmente borra lo local y deja pendiente el aviso de cancelación para cuando haya señal.

### 3.3 Eliminar la cuenta
Se hace desde la app, en dos pasos, porque Google Play lo exige. En el primero la pantalla explica qué desaparece: perfil y foto, historial de trabajos, mensajes, casa y auto, y técnicos de confianza, y aclara que los reportes de seguridad se conservan solo el tiempo que obligue la ley. Muestra lo pendiente, como citas y cotizaciones abiertas, y ofrece "Cancelar lo pendiente y eliminar". Pide un motivo opcional y, si la persona solo quiere menos avisos, le ofrece ajustarlos. En el segundo paso se confirma escribiendo "ELIMINAR" o con un código por SMS.

Hay siete días de gracia para arrepentirse: al volver a entrar se puede cancelar la eliminación. Pasado el plazo se borran el perfil, el historial y los mensajes, y las opiniones se anonimizan. Google Play exige además un enlace web para iniciar la solicitud, que está en la landing.

## 4. Notificaciones push
Un requerimiento son las notificaciones para los procesos principales de la app. Se envían con Firebase Cloud Messaging, y la app registra el identificador de cada teléfono al iniciar sesión. Cada notificación abre la pantalla correspondiente.

El técnico recibe una notificación cuando llega una solicitud nueva, cuando el cliente aceptó su cotización, cuando el cliente aprobó o rechazó un cambio de precio, cuando se propone o se confirma una reprogramación o se cancela una cita, cuando recibe una opinión nueva y con el resultado de su verificación. El cliente recibe una notificación cuando llega una cotización nueva, cuando el técnico propone un cambio de precio o envía la cotización final, cuando hay un mensaje nuevo en el chat, como recordatorio de la visita la víspera y una hora antes, cuando hay una reprogramación o cancelación, y cuando el técnico terminó el trabajo y se puede calificar.

Si la persona está dentro del chat, el mensaje llega en vivo y no se envía la notificación. Cada quien elige qué avisos recibir en Configuración. Los textos no incluyen información sensible, como el código de visita o la dirección.

## 5. Permisos del teléfono
La app pide cada permiso justo cuando lo necesita, con una explicación previa de para qué sirve, y funciona sin los opcionales.

El permiso de ubicación, solo mientras se usa la app, sirve para sugerir la colonia y mostrar técnicos cercanos; si se niega, se pide el código postal, y no se guardan coordenadas. El permiso de cámara sirve para las fotos del problema, del trabajo terminado, el INE y la selfie; si se niega, no se pueden tomar fotos, pero se pueden elegir de la galería, salvo la selfie de verificación. El permiso de fotos y archivos sirve para elegir imágenes ya tomadas, y si se niega solo se usa la cámara. El permiso de notificaciones sirve para los avisos de cotizaciones, mensajes y visitas, y si se niega la app funciona, pero hay que abrirla para ver las novedades. El permiso de contactos no se pide: para avisar a alguien de confianza se usa el selector del sistema, que no necesita permiso, o se teclea el número.

## 6. Contenido inapropiado
Todo lo que la persona sube, como descripciones, mensajes, opiniones, nombres y fotos, pasa antes por la revisión de la nube a través del backend. Si se marca como inapropiado, no se guarda, y la app explica por qué con un mensaje claro y sin culpar. Las fotos de INE y selfie no pasan por esa revisión automática, porque las revisa una persona.

## 7. Privacidad y manejo de datos
El aviso de privacidad y los términos están dentro de la app, en Mi cuenta, en Legal, y antes de registrarse, y están enlazados a las páginas de la landing, como exige Google Play. Los consentimientos son separados y se pueden retirar: el biométrico de la selfie, las promociones, las encuestas y las recomendaciones, cada uno con su fecha y la versión del aviso. Cada persona puede ver, descargar, corregir y borrar sus datos, lo que corresponde a los derechos ARCO de acceso, rectificación, cancelación y oposición. Se aplica la minimización: solo se pide lo necesario y se guarda lo menos posible, la ubicación exacta no se guarda y el técnico solo ve la colonia hasta ser elegido. Los textos legales son de demostración y deben revisarse con un abogado antes de un lanzamiento real. El aviso declara que los datos se alojan fuera de México y que la selfie se trata como dato sensible.

## 8. Caché y trabajo sin conexión
Un requerimiento es la caché en el frontend. La app se usa con señal irregular, así que esto se nota.

### 8.1 Caché en la app
Se guardan en el teléfono, por poco tiempo, los catálogos de oficios y colonias, los perfiles ya vistos, los listados recientes y las imágenes. Antes de volver a descargar, la app pregunta al servidor si cambió, usando la marca de versión ETag, y si no cambió usa lo guardado; así gasta menos datos y responde rápido aunque la señal sea mala. Las pantallas muestran primero lo guardado y actualizan en cuanto llegan los datos nuevos. No se guardan en caché las listas personales que cambian al crear algo, para no mostrar datos viejos. Al cerrar sesión se borra toda la caché.

### 8.2 Borradores
Lo que la persona escribe al pedir una cotización, tanto el texto como las fotos elegidas, se conserva si se queda sin señal o cierra la app, y se retoma al volver.

### 8.3 Base local con sincronización, en alcance mínimo
Un requerimiento opcional es guardar información localmente con SQLite y sincronizarla cuando vuelva la conexión. En Knock entra en un alcance mínimo, que refuerza además la caché. Guarda los catálogos, las últimas conversaciones y los mensajes por enviar. Los mensajes y cambios hechos sin conexión quedan en una cola; al regresar la señal, la app los envía en orden y descarga lo nuevo, y una operación repetida no se duplica porque lleva su identificador único. No sincroniza el resto de los datos: las solicitudes y las cotizaciones siempre se consultan en línea, porque dependen de datos que cambian en el servidor. La base local se limpia al cerrar sesión.

## 9. El mapa
Se usa Google Maps para Flutter, con agrupación de pines, de modo que varios técnicos juntos se muestran como un número, y un botón para volver a la ubicación de la persona. La colonia se calcula con nuestra propia tabla de colonias, tomada de SEPOMEX y con sus polígonos, y no con una geocodificación de pago. La clave de Google Maps va en los secretos y nunca en el código, y se restringe a la app. Maps exige una cuenta de facturación de Google, que se crea junto con el plan de Firebase antes de abrir la prueba cerrada de Google Play. Los pines de la agenda del técnico solo marcan la zona aproximada. Solo se evalúa la versión de Android, por lo que no se genera clave para iOS.

## 10. Inteligencia artificial y aprendizaje automático en la app
Un requerimiento es al menos una función de aprendizaje automático y una de IA generativa en el móvil. Ambas corren en el backend, en el módulo Asistente del servicio de Mercado, y la app solo las usa.

En la IA generativa, el asistente Knock, al que se llega con "No sé cuál", conversa, pregunta desde cuándo pasa el problema, exige una foto y arma la solicitud con la categoría y el texto ya llenos. Si detecta un riesgo, como gas, empieza por las instrucciones de seguridad. Dice siempre que no es una persona. El resumen del chat y el borrador de cotización editable son para el técnico. En todos los casos la IA propone y la persona decide y envía, y nunca recibe INE, CURP, dirección ni teléfono.

En el aprendizaje automático, el orden "Recomendadas" ordena las cotizaciones al compararlas, y el rango de precio sugerido aparece al cotizar.

## 11. Pruebas, CI/CD y versiones

### 11.1 Pruebas
Las pruebas unitarias cubren las reglas de la capa de dominio y los controladores. Las pruebas de pantallas, llamadas de widgets, comprueban cada pantalla en sus estados de cargando, con datos, vacía y con error. Las pruebas de integración, hechas con integration_test, recorren flujos completos, como iniciar sesión o pedir una cotización. La cobertura mínima es de 50 por ciento. Para las pruebas de mutación se usa mutation_test, una herramienta de Dart que cambia el código a propósito para comprobar que las pruebas lo detectan, y se corre sobre lo que cambió. Para el rendimiento, hay pruebas de integración con el perfilador de Flutter que miden el tiempo de arranque y la fluidez del mapa y las listas.

En seguridad, se hace un análisis estático del código con Semgrep, se buscan dependencias vulnerables y se revisan licencias, y se detectan claves olvidadas; los tokens se guardan en almacenamiento seguro y no hay claves en el código. El análisis de la calidad del código es estricto, con advertencias tratadas como errores, y el formato es obligatorio. Por último, el cliente generado se regenera y se compara con el contrato del backend.

### 11.2 CI/CD y publicación en Google Play
En la integración continua, en cada cambio un proceso automático en GitHub revisa el formato, el análisis, las pruebas y la cobertura, y arma el paquete de la app. En la entrega continua, se establece como requerimiento desplegar desde GitHub a Google Play, y así lo hacemos: un proceso automático construye el paquete firmado, llamado AAB, y lo publica por la API de Google Play.

La firma de la app y las credenciales de Google Play viven en los secretos de GitHub, que guardan un nombre y un valor en el repositorio, y nunca se escriben en el código. Cuando el proceso se ejecuta, las lee de ahí. La publicación pasa por etapas: primero la pista interna, para el equipo; luego una prueba cerrada con al menos 12 testers durante 14 días seguidos, que es requisito de Google Play para cuentas nuevas (reclutamos 20 compañeros de la universidad por si alguno se sale); y después producción. Cada etapa se versiona.

### 11.3 Versiones
La app sigue la numeración por puntos de la descripción general. El número que ve la gente es X.Y.Z, y el número interno que exige Google Play, llamado versionCode, se calcula como X por 1,000,000, más Y por 10,000, más Z por 100, más n (el número de subida de esa versión, de 0 a 99), de modo que siempre crece.

---

# LANDING PAGE

## 1. Para qué sirve
Es la página de aterrizaje a donde llega toda la estrategia de marketing, ya sea Facebook, Instagram, X, YouTube o las demás redes, y desde ahí se redirige a la gente a descargar la app. Usamos una sola página por tres razones. Concentra el tráfico en un punto, lo que permite medir de dónde viene cada visita. Aloja los avisos legales, de privacidad y de eliminación de cuenta, que Google Play exige y a los que enlaza la app. Y es la cara pública de Knock antes de que alguien instale nada.

## 2. Qué contiene, sección por sección
El encabezado lleva el logotipo, el lema "Técnicos de confianza, a un toque" y un botón visible "Descargar en Google Play". Sigue una explicación de qué es Knock, en dos o tres frases, que cuenta el problema de buscar técnicos en grupos sin saber en quién confiar y la solución. Después viene cómo funciona en tres pasos: describe tu problema con fotos, compara cotizaciones de varios técnicos y elige con confianza.

Una sección de por qué confiar explica que los técnicos tienen identidad verificable, que las opiniones son solo de servicios reales, que el teléfono y la dirección quedan ocultos hasta elegir, y que existen el código de visita y el precio con tope. Una sección para técnicos los invita a registrarse y a recibir solicitudes de su zona sin pagar publicidad. Otra sección, "Dónde estamos", aclara que Knock empieza en Tula de Allende, Hidalgo, e invita a quienes estén fuera a apuntarse a la lista de espera. Hay un apartado de preguntas frecuentes cortas. El pie de página tiene los enlaces al aviso de privacidad, a los términos y condiciones, a la página para eliminar la cuenta y al contacto.

La página se ve bien en el teléfono, con un diseño adaptable pensado primero para pantallas pequeñas, usa la misma identidad visual y paleta que la app y tiene buen contraste y textos alternativos en las imágenes.

## 3. Redirección a la tienda
El botón principal lleva a la ficha de la app en Google Play. Desde un dispositivo que no es Android, la página explica que por ahora solo hay versión Android y muestra el botón de todos modos. Los enlaces de cada campaña llevan una marca, llamada parámetros UTM, que dice de qué red social y de qué publicación vienen, para medir qué estrategia funciona, y esa marca se conserva hasta el botón de Google Play.

## 4. Páginas de privacidad y de la cuenta
La app las enlaza y la landing las aloja. El aviso de privacidad y manejo de datos dice qué datos se tratan y para qué, cuáles son sensibles, que son el INE, la selfie y la CURP, qué finalidades necesitan consentimiento y cuáles no, quiénes reciben datos, que son Azure, Firebase y Google Maps, que los datos se alojan fuera de México, cuánto tiempo se conservan y cómo ejercer los derechos de acceso, rectificación, cancelación y oposición. Puede ser una página o varias, por ejemplo una resumida y otra completa. Los términos y condiciones explican que Knock es intermediario y no presta el servicio, las reglas de conducta, la política de reportes y sanciones y el derecho de apelación. La página para eliminar la cuenta explica cómo hacerlo desde la app y ofrece un formulario web para quien ya no tenga la app; es el enlace que Google Play exige además del botón dentro de la app. Hay también una página de contacto. Los textos legales son de demostración y deben revisarse con un abogado antes de un lanzamiento real.

## 5. Google Analytics
Con el correo único del proyecto se crea la cuenta de Google Analytics. Google entrega un fragmento de código en JavaScript, que se pega en la página, y con él se ve de dónde llegan las visitas, cuántas son y qué hacen. Se vincula a la propiedad con el correo del proyecto para que el equipo comparta el acceso.

Como es analítica, la página muestra un aviso de cookies que se puede rechazar: "Usamos cookies de analítica para saber de qué red social llegas y mejorar Knock. Puedes aceptarlas o rechazarlas; si las rechazas, la página funciona igual y no se mide tu visita". El código de seguimiento solo se carga si la persona acepta.

## 6. Dónde vive y cómo se publica
Los requerimientos dan libertad para elegir el hosting. Elegimos Railway, donde el líder del proyecto ya tiene un espacio contratado, así que no genera costo adicional para el proyecto y usa HTTPS. Se descartó Azure Static Web Apps porque sus regiones no coinciden con las cinco que permite la suscripción. La dirección es la que da Railway, y un dominio propio se puede enlazar después sin cambiar la página.

Cada cambio aprobado en la rama principal se publica solo. El proceso revisa enlaces rotos y busca claves olvidadas, igual que los demás repositorios. El HTTPS es obligatorio y la landing lleva su propia versión según la matriz de la descripción general. La landing tiene una fecha límite dura, porque Google Play exige la página de privacidad antes de abrir una prueba cerrada, y por eso lleva dueño y fecha desde el principio y no se deja a elegir.

---

# REGLAS QUE DEBE CUMPLIR TODO EL PROYECTO
Estas reglas resumen lo no negociable y sirven para convertirlo en la constitución. Nada se programa sin una especificación aprobada. Ninguna contraseña, clave o secreto va en el código, sino en Key Vault y en los secretos de GitHub. Todo acceso a datos pasa por una capa con caché, y se borra de la caché al escribir. Todo listado va paginado, con índices en lo que se filtra u ordena. Todo el tráfico sale y entra por HTTPS. Todo servicio funciona con al menos dos copias y no guarda estado en su memoria. El cierre de sesión cancela el token en el backend y borra la sesión en la app. Ningún servicio lee la base de datos de otro.

Todo texto e imagen se revisa con la API de moderación antes de guardarse. La IA propone y una persona decide, y la IA nunca recibe datos personales sensibles. Cada cambio pasa por las pruebas y por la revisión de otro integrante. Cada despliegue lleva un número de versión calculado por puntos. Todo se despliega con Terraform y se puede crear y borrar con comandos. Todos los servicios usan niveles gratuitos y el consumo se revisa cada semana. El teléfono y la dirección exacta nunca se muestran antes de elegir una cotización. Las fotos de identidad se cifran con llave propia y se destruyen al decidir o a los siete días. No se usan datos personales reales en desarrollo ni en pruebas. Y cada operación que crea algo es idempotente.

---

# DÓNDE ESTÁ CADA TEMA ESTABLECIDO COMO REQUERIMIENTO
La descarga de la app desde Google Play para evaluar se explica en Aplicación móvil, secciones 1 y 11, y en la landing, sección 3. Los cinco componentes del proyecto, que son la nube, la base de datos, el backend, el móvil y la landing, se describen en la descripción general, sección 5, y en cada documento. Que el móvil sea como la maqueta, sin quitar funciones, está en Aplicación móvil, sección 2. El Spec-Driven Development y el DevSecOps están en la descripción general, sección 7, y en las secciones de pruebas y de despliegue de ambos documentos. El desarrollo con agentes de IA está en la descripción general, sección 7.3. Las pruebas unitarias, de seguridad, de integración, de mutación y de rendimiento están en Backend, sección 13, y en Aplicación móvil, sección 11.

El CI/CD está en Backend, sección 14, en Aplicación móvil, sección 11, y en la landing, sección 6. La caché en base de datos, backend y app está en Backend, sección 9, y en Aplicación móvil, sección 8. El cierre de sesión en backend y móvil está en Backend, sección 8.2, y en Aplicación móvil, sección 3. La matriz de versionamiento está en la descripción general, sección 8, y el uso de MCP en la sección 7.4. La nube, con servicios, balanceador, Terraform y SSL, está en Backend, secciones 1, 2, 3 y 8.3. Las bases de datos NoSQL están en Backend, sección 5.

El backend con dos copias, costo cero, endpoints rápidos, CDN, observabilidad, JWT con Redis y rendimiento está en Backend, secciones 3 a 12, y en la descripción general, sección 10. En el móvil, la moderación, las notificaciones, la privacidad, eliminar la cuenta, los permisos, la sesión viva, SQLite, el aprendizaje automático y la IA están en Aplicación móvil, secciones 2 a 10. La landing, con redirección, privacidad y Analytics, tiene su propia sección. La cuenta única de GitHub, el correo único y la carpeta compartida están en la descripción general, sección 9. Y la evaluación del 2 de diciembre, con la entrega en .rar y el script de la base, está en Backend, sección 5.5.

---

# GLOSARIO
Estos términos se usan igual en todo el documento.

Del producto. El cliente es la persona que busca un servicio y el técnico es la persona o taller que lo ofrece; no se usa "experto" ni "prestador". La solicitud es lo que el cliente envía describiendo su problema. La cotización es la respuesta de un técnico a una solicitud, con precio, día y hora. El tope es el máximo que una cotización puede llegar a costar sin aprobación del cliente. La visita es el servicio a domicilio y la cita es el servicio en el taller del técnico. El código de visita son los cuatro dígitos que el cliente pide al técnico en la puerta. La opinión es la calificación y el comentario del cliente sobre un trabajo terminado. Un técnico de confianza es un técnico que el cliente guardó para volver a contratarlo. La verificación es la revisión de identidad, con INE y selfie, del técnico. Una oleada es cada grupo de técnicos a los que se envía una solicitud.

De la arquitectura. Un servicio, o microservicio, es un programa pequeño con una sola responsabilidad, con su propia base de datos y su propio despliegue. El gateway, o puerta de entrada, es el único punto por el que entra el tráfico a los servicios. La API es la interfaz por la que la app pide cosas al servidor, REST es su estilo y OpenAPI es el archivo que describe todas sus rutas; un endpoint es una ruta concreta de la API. Un contenedor es un paquete que lleva un programa con todo lo que necesita para correr, y Container Apps es el servicio de Azure que ejecuta contenedores y los balancea. El balanceador de carga reparte las peticiones entre las copias de un servicio. El escalado es tener más copias de un servicio, y el autoescalado es que el sistema las agregue solo, lo cual no se usa.

El JWT es el token firmado que prueba quién es la persona, y aquí dura 15 minutos; el token de renovación es el de larga vida, de 30 días, que se cambia por un JWT nuevo. Redis es una base clave-valor muy rápida que aquí guarda tokens cancelados, caché y contadores. MongoDB es una base documental, que guarda documentos con forma de JSON en colecciones. Un índice TTL hace que la base borre sola un documento al vencer, y 2dsphere es el índice de MongoDB para ubicaciones. El outbox, o bandeja de salida, es un patrón que guarda un aviso junto al cambio que lo provoca para no perderlo. La idempotencia significa que repetir una petición no crea un duplicado. La CDN es una red que sirve imágenes desde un punto cercano a cada persona. Key Vault es la caja fuerte de claves y contraseñas en Azure, y Application Insights es el servicio de Azure que guarda los registros de eventos y errores. Terraform, o IaC, es la herramienta que crea y elimina infraestructura a partir de archivos. FCM es el servicio de notificaciones push de Google, y Firebase Authentication es el servicio que comprueba el código SMS.

De la forma de trabajar. SDD significa que primero se escribe y aprueba una especificación y después se programa. La constitución son las reglas que nunca se rompen. DevSecOps es la seguridad integrada en el desarrollo y el despliegue automático. CI/CD es la integración continua, que prueba cada cambio, y la entrega continua, que despliega. Un PR, o solicitud de cambio, es una propuesta de cambio al código que otro integrante revisa. SAST, SCA y DAST son el análisis del código, el de las dependencias y el del sistema en ejecución. Una prueba de mutación cambia el código a propósito para comprobar que las pruebas lo detectan, y el fuzzing envía entradas aleatorias para encontrar fallos. MCP es el protocolo para que un agente de IA opere herramientas externas. X.Y.Z es el esquema de tres números para las versiones. AAB es el paquete de la app que se sube a Google Play, y el plan Blaze es el plan de Firebase de pago por uso, necesario para enviar SMS reales.
