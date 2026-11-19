// Spec 027: colecciones e índices de búsqueda, sin modificar documentos.
const searchDbName = process.env.KNOCK_DB_MARKET || 'market';
if (!/^[A-Za-z0-9_-]{1,63}$/.test(searchDbName)) {
  print('Nombre de base no válido.');
  quit(2);
}
const searchMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(searchDbName) : db.getSiblingDB(searchDbName);
for (const collection of ['sinonimos', 'catalogo_meta', 'busquedas_sin_resultado', 'tecnicos']) {
  if (!searchMarket.getCollectionNames().includes(collection)) searchMarket.createCollection(collection);
}
searchMarket.sinonimos.createIndex({ termino: 1, oficioClave: 1 }, { unique: true, name: 'termino_1_oficioClave_1' });
searchMarket.sinonimos.createIndex({ oficioClave: 1 }, { name: 'oficioClave_1' });
searchMarket.busquedas_sin_resultado.createIndex({ terminoNormalizado: 1, dia: 1 }, { unique: true, name: 'terminoNormalizado_1_dia_1' });
searchMarket.busquedas_sin_resultado.createIndex({ creadoEn: 1 }, { expireAfterSeconds: 7776000, name: 'creadoEn_1' });
searchMarket.tecnicos.createIndex({ ubicacionBase: '2dsphere' }, { name: 'ubicacionBase_2dsphere' });
searchMarket.tecnicos.createIndex({ oficios: 1, estado: 1, oculto: 1 }, { name: 'oficios_1_estado_1_oculto_1' });
searchMarket.tecnicos.createIndex({ palabrasBusqueda: 1 }, { name: 'palabrasBusqueda_1' });
