// Spec 048: índices adicionales; los índices existentes se crean en 020.
// KNOCK_DB_URI y KNOCK_DB_MARKET siguen el mismo convenio que los scripts anteriores.
const complementsDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(complementsDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const complementsMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(complementsDbName) : db.getSiblingDB(complementsDbName);
complementsMarket.solicitudes.createIndex({ clienteId: 1, estado: 1, creadoEn: -1 }, { name: "clienteId_1_estado_1_creadoEn_-1" });
complementsMarket.solicitudes.createIndex({ coloniaId: 1, oficioClave: 1, estado: 1 }, { name: "coloniaId_1_oficioClave_1_estado_1" });
complementsMarket.cotizaciones.createIndex({ tecnicoUsuarioId: 1, estado: 1, creadoEn: -1 }, { name: "tecnicoUsuarioId_1_estado_1_creadoEn_-1" });
