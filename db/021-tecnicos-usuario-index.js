// Spec 020 R-11: índice de búsqueda de técnicos por usuarioId. Idempotente.
const technicianIndexDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(technicianIndexDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}
const technicianIndexDb = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(technicianIndexDbName) : db.getSiblingDB(technicianIndexDbName);
if (!technicianIndexDb.getCollectionNames().includes("tecnicos")) {
  technicianIndexDb.createCollection("tecnicos");
}
technicianIndexDb.tecnicos.createIndex({ usuarioId: 1 }, { name: "usuarioId_1" });
