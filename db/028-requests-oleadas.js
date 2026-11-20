// Spec 028: índices de reparto y tareas; no altera documentos existentes.
const wavesDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(wavesDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}
const wavesMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(wavesDbName) : db.getSiblingDB(wavesDbName);
for (const name of ["envios", "tareas_programadas", "tecnicos"]) {
  if (!wavesMarket.getCollectionNames().includes(name)) {
    wavesMarket.createCollection(name);
  }
}
wavesMarket.envios.createIndex({ solicitudId: 1, tecnicoId: 1 }, { unique: true });
wavesMarket.envios.createIndex({ tecnicoUsuarioId: 1, estado: 1, enviadoEn: -1 });
wavesMarket.envios.createIndex({ solicitudId: 1, estado: 1 });
wavesMarket.tareas_programadas.createIndex({ tipo: 1, claveNegocio: 1 }, { unique: true });
wavesMarket.tareas_programadas.createIndex({ actualizadoEn: 1 }, { expireAfterSeconds: 2592000, partialFilterExpression: { estado: "hecha" } });
wavesMarket.tecnicos.createIndex({ oficios: 1, estado: 1, oculto: 1, disponibleHasta: 1 });
