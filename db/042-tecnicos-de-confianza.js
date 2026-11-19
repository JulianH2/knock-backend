// Spec 042 (KNOCK-91): colección `tecnicos_confianza` del servicio de Mercado y su índice único.
// Uso (igual que 040-direcciones-del-cliente.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/042-tecnicos-de-confianza.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_MARKET (por defecto «market»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni documentos existentes.
// La definición debe coincidir con TrustStore.EnsureIndexesAsync. No hay teléfonos ni datos de terceros: solo `clienteId`, `tecnicoId` y `guardadoEn`.
const trustDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(trustDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const trustMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(trustDbName) : db.getSiblingDB(trustDbName);

if (!trustMarket.getCollectionNames().includes("tecnicos_confianza")) {
  trustMarket.createCollection("tecnicos_confianza");
}

trustMarket.tecnicos_confianza.createIndex({ clienteId: 1, tecnicoId: 1 }, { name: "clienteId_1_tecnicoId_1", unique: true });

print(`Índices de tecnicos_confianza listos en «${trustDbName}»`);
