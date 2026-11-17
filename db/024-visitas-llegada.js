// Spec 022 (KNOCK-85): índices que la visita segura agrega a `visitas` (la colección y sus primeros índices los crea db/022-visitas.js).
// Uso (igual que 022-visitas.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/024-visitas-llegada.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_MARKET (por defecto «market»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni documentos
// existentes. Las definiciones deben coincidir con las de VisitStore (spec 022, §6).
const arrivalDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(arrivalDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const arrivalMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(arrivalDbName) : db.getSiblingDB(arrivalDbName);

if (!arrivalMarket.getCollectionNames().includes("visitas")) {
  arrivalMarket.createCollection("visitas");
}

// El proceso que vence códigos recorre por estado y vencimiento; la agenda del técnico, por estado y día.
arrivalMarket.visitas.createIndex({ estado: 1, codigoVenceEn: 1 }, { name: "estado_1_codigoVenceEn_1" });
arrivalMarket.visitas.createIndex({ tecnicoUsuarioId: 1, estado: 1, dia: 1 }, { name: "tecnicoUsuarioId_1_estado_1_dia_1" });

print(`Índices de llegada de visitas listos en «${arrivalDbName}»`);
