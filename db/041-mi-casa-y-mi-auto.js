// Spec 041 (KNOCK-286): colección `mi_casa_y_auto` del servicio de Identidad y su índice.
// Uso (igual que 002_identidad_usuarios_sesiones.js, y como lo aplica db/entrega.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/041-mi-casa-y-mi-auto.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_IDENTITY (por defecto «identity»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones,
// índices ni documentos existentes. La definición debe coincidir con HomeAndCarStore.EnsureIndexesAsync.
const homeAndCarDbName = process.env.KNOCK_DB_IDENTITY || process.env.KNOCK_IDENTITY_DB || "identity";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(homeAndCarDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const homeAndCarIdentity = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(homeAndCarDbName) : db.getSiblingDB(homeAndCarDbName);
if (!homeAndCarIdentity.getCollectionNames().includes("mi_casa_y_auto")) {
  homeAndCarIdentity.createCollection("mi_casa_y_auto");
}

// Listar los elementos de una persona, por tipo (spec 041, §6). El prefijo `usuarioId` también sirve para contar y para ver, editar o quitar uno propio.
homeAndCarIdentity.mi_casa_y_auto.createIndex({ usuarioId: 1, tipo: 1 }, { name: "usuarioId_1_tipo_1" });

print(`Índices de mi casa y mi auto listos en «${homeAndCarDbName}»`);
