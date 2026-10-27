// P8 (TC-TT-BD-05-08): siembra, compara y borra las bases del spike. Lo invoca backup-restore.sh.
// La URI se lee de process.env para que no aparezca en la línea de comandos; nunca se imprime.
const fs = require('fs');

const env = process.env;
const client = new Mongo(env.SPIKE_BACKUP_URI);
const src = client.getDB(env.SPIKE_P8_SRC);
const dst = client.getDB(env.SPIKE_P8_DST);
const collections = ['colonias', 'solicitudes', 'outbox', 'oficios', 'tokens_revocados'];

function seed() {
  // P2: 8 colonias con polígono del seed versionado e índice 2dsphere.
  const geo = JSON.parse(fs.readFileSync(env.SPIKE_P8_GEOJSON, 'utf8'));
  src.colonias.createIndex({ geom: '2dsphere' });
  src.colonias.insertMany(geo.features.map((f) => ({
    nombre: f.properties.nombre, cp: f.properties.cp, municipio: f.properties.municipio, geom: f.geometry,
  })));

  // P4: solicitud y su evento de outbox (documentos inventados).
  const solicitudId = new ObjectId();
  src.solicitudes.insertOne({ _id: solicitudId, estado: 'Creada' });
  src.outbox.insertOne({ tipo: 'SolicitudCreada', solicitudId });

  // P5: oficios de db/seeds/catalog/oficios.sql con índice de texto en español.
  const sql = fs.readFileSync(env.SPIKE_P8_OFICIOS, 'utf8');
  const oficios = [...sql.matchAll(/\('([a-z]+)',\s*'([^']+)'/g)].map((m) => ({ clave: m[1], nombre: m[2] }));
  src.oficios.createIndex({ nombre: 'text' }, { default_language: 'spanish' });
  src.oficios.insertMany(oficios);

  // P6: tokens revocados con índice TTL; vencen en 1 h para que el conteo no cambie durante la prueba.
  src.tokens_revocados.createIndex({ expiraEn: 1 }, { expireAfterSeconds: 0 });
  const expira = new Date(Date.now() + 3600 * 1000);
  src.tokens_revocados.insertMany([1, 2, 3].map((n) => ({ jti: `spike-${n}`, expiraEn: expira })));
  print(`sembrada: ${collections.map((c) => `${c}=${src.getCollection(c).countDocuments()}`).join(', ')}`);
}

// Solo los campos que definen el índice; se ignoran los que agrega cada motor (v, ns, versiones internas).
function indexes(db, name) {
  return db.getCollection(name).getIndexes()
    .map((i) => JSON.stringify({
      name: i.name, key: i.key, expireAfterSeconds: i.expireAfterSeconds,
      default_language: i.default_language, weights: i.weights,
    }))
    .sort();
}

function compare() {
  let ok = true;
  print('| Colección | Docs origen | Docs restaurados | Índices iguales |');
  print('|---|---|---|---|');
  for (const name of collections) {
    const a = src.getCollection(name).countDocuments();
    const b = dst.getCollection(name).countDocuments();
    const ia = indexes(src, name);
    const ib = indexes(dst, name);
    const sameIndexes = JSON.stringify(ia) === JSON.stringify(ib);
    ok = ok && a === b && sameIndexes;
    print(`| ${name} | ${a} | ${b} | ${sameIndexes ? 'sí' : 'no'} (${ia.map((i) => JSON.parse(i).name).join(', ')}) |`);
    if (!sameIndexes) {
      print(`  origen:     ${ia.join(' ')}`);
      print(`  restaurado: ${ib.join(' ')}`);
    }
  }

  print(ok ? 'P8: PASA (conteos e índices coinciden)' : 'P8: FALLA');
  if (!ok) {
    quit(1);
  }
}

function drop() {
  src.dropDatabase();
  dst.dropDatabase();
}

({ seed, compare, drop })[env.SPIKE_P8_MODE]();
