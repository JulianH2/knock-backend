// Spec 020 R-11 y spec 046 §6: índice único de técnicos por usuarioId. Idempotente.
const technicianIndexDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(technicianIndexDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}
const technicianIndexDb = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(technicianIndexDbName) : db.getSiblingDB(technicianIndexDbName);
if (!technicianIndexDb.getCollectionNames().includes("tecnicos")) {
  technicianIndexDb.createCollection("tecnicos");
}
// Spec 046 (§6, R-39 y CA-69): un perfil de técnico por cuenta. Si quedó el índice anterior sin «unique», se reemplaza.
const technicianUserIndex = technicianIndexDb.tecnicos.getIndexes().find((index) => index.name === "usuarioId_1");
if (technicianUserIndex && technicianUserIndex.unique !== true) {
  technicianIndexDb.tecnicos.dropIndex("usuarioId_1");
}
technicianIndexDb.tecnicos.createIndex({ usuarioId: 1 }, { name: "usuarioId_1", unique: true });
