// Spec 041 (KNOCK-286, R-17 y R-19): índice parcial de `solicitudes` para el historial de un elemento de «Mi casa y mi auto».
// Uso (igual que 020-requests-solicitudes-cotizaciones.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/041-solicitudes-elemento-id.js
// La base se toma de KNOCK_DB_MARKET (por defecto «market»). Es idempotente. El índice solo cubre las solicitudes que traen `elementoId`
// (campo opcional y aditivo de la spec 020): las demás no entran en él. Mercado no crea índices al arrancar: los aplica este script.
const elementDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(elementDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const elementMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(elementDbName) : db.getSiblingDB(elementDbName);
if (!elementMarket.getCollectionNames().includes("solicitudes")) {
  elementMarket.createCollection("solicitudes");
}

elementMarket.solicitudes.createIndex(
  { clienteId: 1, elementoId: 1, estado: 1 },
  { name: "clienteId_1_elementoId_1_estado_1", partialFilterExpression: { elementoId: { $exists: true } } });

print(`Índice de elementoId listo en «${elementDbName}»`);
