// Spec 043 (KNOCK-110): colección `reportes` del servicio de Identidad y sus índices.
// Uso (igual que 043-bloqueos.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/043-reportes.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_IDENTITY (por defecto «identity»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni documentos existentes.
// La definición debe coincidir con ReportesStore.EnsureIndexesAsync. El texto y la evidencia del reporte solo los lee el panel (spec 053).
const reportsDbName = process.env.KNOCK_DB_IDENTITY || "identity";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(reportsDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const reportsIdentity = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(reportsDbName) : db.getSiblingDB(reportsDbName);

if (!reportsIdentity.getCollectionNames().includes("reportes")) {
  reportsIdentity.createCollection("reportes");
}

reportsIdentity.reportes.createIndex({ gravedad: 1, estado: 1, creadoEn: 1 }, { name: "gravedad_1_estado_1_creadoEn_1" });
reportsIdentity.reportes.createIndex({ reportadoId: 1 }, { name: "reportadoId_1" });
reportsIdentity.reportes.createIndex({ reportaId: 1 }, { name: "reportaId_1" });

print(`Índices de reportes listos en «${reportsDbName}»`);
