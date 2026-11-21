// Spec 025 (KNOCK-79 y KNOCK-80): colecciones `verificaciones` y `consentimientos` del servicio de Identidad y sus índices (§6).
// Uso (igual que 043-bloqueos.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/025-verificaciones.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_IDENTITY (por defecto «identity»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni documentos.
// La definición debe coincidir con VerificacionStore.EnsureIndexesAsync. Ninguna colección guarda la CURP en claro (R-05).
const verificationDbName = process.env.KNOCK_DB_IDENTITY || "identity";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(verificationDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const verificationIdentity = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(verificationDbName) : db.getSiblingDB(verificationDbName);

for (const verificationCollection of ["verificaciones", "consentimientos"]) {
  if (!verificationIdentity.getCollectionNames().includes(verificationCollection)) {
    verificationIdentity.createCollection(verificationCollection);
  }
}

// La más reciente de cada persona (R-14) y la tarea de vencimiento (R-16).
verificationIdentity.verificaciones.createIndex({ usuarioId: 1, creadoEn: -1 }, { name: "usuarioId_1_creadoEn_-1" });
verificationIdentity.verificaciones.createIndex({ estado: 1, expiraEn: 1 }, { name: "estado_1_expiraEn_1" });
// Una CURP vigente por cuenta (R-06, CA-20): único y parcial sobre el booleano `curpVigente`, porque un filtro parcial con $in no es portable a DocumentDB.
verificationIdentity.verificaciones.createIndex(
  { curpHmac: 1 },
  { name: "curpHmac_1_vigente", unique: true, partialFilterExpression: { curpVigente: true } },
);
// Una constancia de consentimiento por verificación (R-09).
verificationIdentity.consentimientos.createIndex({ usuarioId: 1, aceptadoEn: -1 }, { name: "usuarioId_1_aceptadoEn_-1" });
verificationIdentity.consentimientos.createIndex({ verificacionId: 1 }, { name: "verificacionId_1", unique: true });

print(`Índices de verificaciones listos en «${verificationDbName}»`);
