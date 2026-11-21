// Spec 045 (KNOCK-111, T-01): índices del catálogo de oficios y colección de oficios pedidos.
// La lista de espera ya existe como `listas_espera` (spec 023, db/023-catalog-listas-espera.js): este script no crea otra colección.
// KNOCK_DB_URI=... mongosh --nodb --quiet --file db/045-catalogos-y-lista-de-espera.js
const catalogDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(catalogDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const catalogMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(catalogDbName) : db.getSiblingDB(catalogDbName);
for (const collectionName of ["oficios", "oficios_pedidos"]) {
  if (!catalogMarket.getCollectionNames().includes(collectionName)) {
    catalogMarket.createCollection(collectionName);
  }
}

// OFI-03 a OFI-05: la clave única (db/001_indices.js) ya existe; `activo` sirve para listar solo los oficios activos.
catalogMarket.oficios.createIndex({ activo: 1, clave: 1 }, { name: "activo_1_clave_1" });
// OFP-01 y OFP-02: agrupar y contar por texto normalizado; un mismo técnico no repite el mismo texto (R-21, CA-41).
catalogMarket.oficios_pedidos.createIndex({ textoNormalizado: 1 }, { name: "textoNormalizado_1" });
catalogMarket.oficios_pedidos.createIndex({ usuarioId: 1, textoNormalizado: 1 }, { unique: true, name: "usuarioId_1_textoNormalizado_1" });
print(`Índices de oficios y oficios pedidos listos en «${catalogDbName}»`);
