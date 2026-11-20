// Spec 036: índices para el resumen de trabajo. La fecha de envío de cotizaciones se guarda como creadoEn.
// KNOCK_DB_URI=... mongosh --nodb --quiet --file db/036-resumen-trabajo.js
const workSummaryDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(workSummaryDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const workSummaryMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(workSummaryDbName) : db.getSiblingDB(workSummaryDbName);
for (const name of ["visitas", "cotizaciones"]) {
  if (!workSummaryMarket.getCollectionNames().includes(name)) {
    workSummaryMarket.createCollection(name);
  }
}

workSummaryMarket.visitas.createIndex(
  { tecnicoUsuarioId: 1, estado: 1, terminadaEn: -1, _id: -1 },
  { name: "tecnicoUsuarioId_1_estado_1_terminadaEn_-1__id_-1" }
);
workSummaryMarket.cotizaciones.createIndex(
  { tecnicoUsuarioId: 1, creadoEn: -1 },
  { name: "tecnicoUsuarioId_1_creadoEn_-1" }
);
print(`Índices de resumen de trabajo listos en «${workSummaryDbName}»`);
