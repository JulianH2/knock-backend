// Spec 032 (KNOCK-172, T-03): colecciones `subidas_imagen` y `accesos_privados` del servicio de Mercado y sus índices.
// Uso (igual que 040-direcciones-del-cliente.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/044-market-subidas-imagen.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_MARKET (por defecto «market»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni documentos existentes.
// subidas_imagen: `sub_1_estado_1_confirmarAntesDe_1` cuenta las subidas pendientes vigentes de una persona (máximo 10, R-14); `expiraEn_1` es el TTL (expireAfterSeconds 0):
//   pendiente, rechazada e invalida llevan creadaEn + 7 días y una publicada se fija a 9999-12-31 para que no venza nunca (R-19).
// accesos_privados: registro de acceso a blobs privados (solo inserción desde el código); `emitidoEn_1` vence a los 180 días (15 552 000 s). Guarda el HMAC del `sub`, nunca el `sub`.
const imagesDbName = process.env.KNOCK_DB_MARKET || "market";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(imagesDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const imagesMarket = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(imagesDbName) : db.getSiblingDB(imagesDbName);

for (const name of ["subidas_imagen", "accesos_privados"]) {
  if (!imagesMarket.getCollectionNames().includes(name)) {
    imagesMarket.createCollection(name);
  }
}

imagesMarket.subidas_imagen.createIndex({ sub: 1, estado: 1, confirmarAntesDe: 1 }, { name: "sub_1_estado_1_confirmarAntesDe_1" });
imagesMarket.subidas_imagen.createIndex({ expiraEn: 1 }, { expireAfterSeconds: 0, name: "expiraEn_1" });
imagesMarket.accesos_privados.createIndex({ emitidoEn: 1 }, { expireAfterSeconds: 15552000, name: "emitidoEn_1" });

print(`Índices de imágenes públicas listos en «${imagesDbName}»`);
