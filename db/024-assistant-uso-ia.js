// Spec 024: registro de uso sin contenido; retención aún no definida, sin TTL.
const assistantDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(assistantDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}
const assistantMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(assistantDbName) : db.getSiblingDB(assistantDbName);
if (!assistantMarket.getCollectionNames().includes("uso_ia")) {
  assistantMarket.createCollection("uso_ia");
}
assistantMarket.uso_ia.createIndex({ usuarioId: 1, creadoEn: -1 }, { name: "usuarioId_1_creadoEn_-1" });
assistantMarket.uso_ia.createIndex({ creadoEn: -1 }, { name: "creadoEn_-1" });
assistantMarket.uso_ia.createIndex({ funcion: 1, origen: 1, creadoEn: -1 }, { name: "funcion_1_origen_1_creadoEn_-1" });
print(`Índices de uso de IA listos en «${assistantDbName}»`);
