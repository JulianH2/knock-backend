// Spec 043 (KNOCK-110): colección `bloqueos` del servicio de Identidad y sus índices.
// Uso (igual que 040-direcciones-del-cliente.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/043-bloqueos.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_IDENTITY (por defecto «identity»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni documentos existentes.
// La definición debe coincidir con BloqueosStore.EnsureIndexesAsync. Solo se guardan los dos identificadores públicos (sub) y la fecha: ningún dato personal.
const blocksDbName = process.env.KNOCK_DB_IDENTITY || "identity";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(blocksDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const blocksIdentity = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(blocksDbName) : db.getSiblingDB(blocksDbName);

if (!blocksIdentity.getCollectionNames().includes("bloqueos")) {
  blocksIdentity.createCollection("bloqueos");
}

blocksIdentity.bloqueos.createIndex({ bloqueaId: 1, bloqueadoId: 1 }, { name: "bloqueaId_1_bloqueadoId_1", unique: true });
blocksIdentity.bloqueos.createIndex({ bloqueadoId: 1 }, { name: "bloqueadoId_1" });

print(`Índices de bloqueos listos en «${blocksDbName}»`);
