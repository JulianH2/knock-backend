// Spec 021 (KNOCK-82): colección `visitas` del módulo Requests de Mercado y sus índices.
// Uso (igual que 020-requests-solicitudes-cotizaciones.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/022-visitas.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_MARKET (por defecto «market», la de Mongo:Databases:Requests). Es idempotente: crea lo que falta y nunca borra ni
// cambia colecciones, índices ni documentos existentes. Las definiciones deben coincidir con las de ChoiceStore (spec 021, §6).
const visitsDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(visitsDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const visitsMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(visitsDbName) : db.getSiblingDB(visitsDbName);

if (!visitsMarket.getCollectionNames().includes("visitas")) {
  visitsMarket.createCollection("visitas");
}

// Una cita por solicitud (R-11): la segunda elección choca con este índice único. Después, las citas de un cliente o de un técnico por día
// y el barrido por estado y día (spec 022).
visitsMarket.visitas.createIndex({ solicitudId: 1 }, { unique: true, name: "solicitudId_1" });
visitsMarket.visitas.createIndex({ clienteId: 1, dia: 1 }, { name: "clienteId_1_dia_1" });
visitsMarket.visitas.createIndex({ tecnicoUsuarioId: 1, dia: 1 }, { name: "tecnicoUsuarioId_1_dia_1" });
visitsMarket.visitas.createIndex({ estado: 1, dia: 1 }, { name: "estado_1_dia_1" });

print(`Índices de visitas listos en «${visitsDbName}»`);
