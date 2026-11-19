// Spec 040 (KNOCK-285): colección `direcciones` del servicio de Identidad y su índice.
// Uso (igual que 002_identidad_usuarios_sesiones.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/040-direcciones-del-cliente.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_IDENTITY (por defecto «identity»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni
// documentos existentes. La definición debe coincidir con AddressesStore.EnsureIndexesAsync.
// `calleNumero` y `referencias` se guardan cifradas a nivel de campo (nunca en claro); el índice solo sirve para listar las direcciones de una persona.
const addressesDbName = process.env.KNOCK_DB_IDENTITY || "identity";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(addressesDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const addressesIdentity = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(addressesDbName) : db.getSiblingDB(addressesDbName);

if (!addressesIdentity.getCollectionNames().includes("direcciones")) {
  addressesIdentity.createCollection("direcciones");
}

addressesIdentity.direcciones.createIndex({ usuarioId: 1 }, { name: "usuarioId_1" });

print(`Índices de direcciones listos en «${addressesDbName}»`);
