// Spec 030 (KNOCK-73): colecciones `conversaciones` y `mensajes` del servicio de Mensajería y sus índices.
// Uso (igual que 040-direcciones-del-cliente.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/030-conversaciones.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_MESSAGING (por defecto «messaging»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni documentos existentes.
// La definición debe coincidir con spec 030 §6 (y con los stores de Conversaciones cuando existan). Los mensajes se conservan 180 días desde `creadoEn` (TTL de 15 552 000 s).
const chatDbName = process.env.KNOCK_DB_MESSAGING || "messaging";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(chatDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const chatMessaging = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(chatDbName) : db.getSiblingDB(chatDbName);

for (const collection of ["conversaciones", "mensajes"]) {
  if (!chatMessaging.getCollectionNames().includes(collection)) {
    chatMessaging.createCollection(collection);
  }
}

// conversaciones: una por par (solicitud, técnico) y la lista de cada participante por actividad reciente.
chatMessaging.conversaciones.createIndex({ solicitudId: 1, tecnicoId: 1 }, { name: "solicitudId_1_tecnicoId_1", unique: true });
chatMessaging.conversaciones.createIndex({ clienteId: 1, ultimoMensajeEn: -1, _id: -1 }, { name: "clienteId_1_ultimoMensajeEn_-1__id_-1" });
chatMessaging.conversaciones.createIndex({ tecnicoUsuarioId: 1, ultimoMensajeEn: -1, _id: -1 }, { name: "tecnicoUsuarioId_1_ultimoMensajeEn_-1__id_-1" });

// mensajes: paginación por cursor dentro de la conversación y retención de 180 días.
chatMessaging.mensajes.createIndex({ conversacionId: 1, creadoEn: -1, _id: -1 }, { name: "conversacionId_1_creadoEn_-1__id_-1" });
chatMessaging.mensajes.createIndex({ creadoEn: 1 }, { expireAfterSeconds: 15552000, name: "creadoEn_1" });

print(`Índices de conversaciones y mensajes listos en «${chatDbName}»`);
