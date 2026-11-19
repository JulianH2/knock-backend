// Spec 039 (KNOCK-284): índice de la eliminación de cuenta en `usuarios` y colección `recuperaciones` del servicio de Identidad.
// KNOCK_DB_URI=... mongosh --nodb --quiet --file db/039-cuenta-y-datos-personales.js
// La base se toma de KNOCK_DB_IDENTITY (por defecto «identity»). Es idempotente. Las definiciones deben coincidir con AccountStore.EnsureIndexesAsync.
const accountDbName = process.env.KNOCK_DB_IDENTITY || process.env.KNOCK_IDENTITY_DB || "identity";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(accountDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}
const accountIdentity = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(accountDbName) : db.getSiblingDB(accountDbName);

for (const accountCollection of ["usuarios", "recuperaciones"]) {
  if (!accountIdentity.getCollectionNames().includes(accountCollection)) {
    accountIdentity.createCollection(accountCollection);
  }
}

// usuarios: el proceso de borrado busca las cuentas con `estado` de eliminación programada cuyo `eliminarEn` ya venció.
accountIdentity.usuarios.createIndex({ estado: 1, eliminarEn: 1 }, { name: "estado_1_eliminarEn_1" });

// recuperaciones: la cola del panel lee los casos por estado y antigüedad; los casos se borran a los 90 días de su última actualización (TTL).
accountIdentity.recuperaciones.createIndex({ estado: 1, creadoEn: 1 }, { name: "estado_1_creadoEn_1" });
accountIdentity.recuperaciones.createIndex({ usuarioId: 1, estado: 1 }, { name: "usuarioId_1_estado_1" });
accountIdentity.recuperaciones.createIndex({ actualizadoEn: 1 }, { expireAfterSeconds: 7776000, name: "actualizadoEn_1" });

print(`Índices de cuenta y recuperaciones listos en «${accountDbName}»`);
