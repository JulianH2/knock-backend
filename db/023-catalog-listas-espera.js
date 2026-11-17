// Spec 023 (KNOCK-47): colección e índices de la lista de espera de Catalog.
// KNOCK_DB_URI=... mongosh --nodb --quiet --file db/023-catalog-listas-espera.js
const waitlistDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(waitlistDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const waitlistMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(waitlistDbName) : db.getSiblingDB(waitlistDbName);
if (!waitlistMarket.getCollectionNames().includes("listas_espera")) {
  waitlistMarket.createCollection("listas_espera");
}
waitlistMarket.listas_espera.createIndex({ usuarioId: 1, cp: 1 }, { unique: true, name: "usuarioId_1_cp_1" });
waitlistMarket.listas_espera.createIndex({ cp: 1, creadoEn: -1 }, { name: "cp_1_creadoEn_-1" });
print(`Índices de listas de espera listos en «${waitlistDbName}»`);
