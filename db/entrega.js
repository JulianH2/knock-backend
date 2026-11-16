// Script de entrega de la base de datos (spec 014, R-02 y CA-01 + spec 010, R-02): deja una instancia VACÍA lista para el sistema, en un solo paso:
//   1. aplica en orden los scripts numerados de db/ (001_indices.js, 002_… y los que se agreguen): colecciones e índices;
//   2. carga los datos de ejemplo del catálogo de Mercado (db/seeds/catalog/oficios.json y colonias.json).
// Es idempotente: volver a correrlo sobre una base ya cargada no duplica ni cambia nada (los datos se cargan con upsert por clave natural).
//
// Uso (mongosh, desde la raíz del repositorio y sin la URI en la línea de comandos):
//   KNOCK_DB_URI=... KNOCK_REDIS_HOST=... mongosh --nodb --quiet --file db/entrega.js
// Variables de entorno:
//   KNOCK_DB_URI      cadena de conexión; en Azure, desde Key Vault. Obligatoria. Nunca se imprime.
//   KNOCK_DB_MARKET   base del servicio de Mercado (por defecto «market»); las demás las toman los scripts numerados.
//   KNOCK_DB_DIR      carpeta db/ si el script no se corre desde la raíz del repositorio (por defecto «db»).
//   KNOCK_REDIS_HOST  host de Redis para invalidar la caché del catálogo (spec 010, R-02). Obligatoria cuando el script escribe:
//                     no se asume que Redis está vacío porque se ejecuta en una instancia nueva.
//   KNOCK_REDIS_PORT  puerto de Redis (por defecto «6379»).
//   KNOCK_REDIS_PASSWORD  contraseña de Redis. Se envía como comando AUTH (RESP), nunca interpolada en un comando shell.
//   KNOCK_REDIS_TLS   «1» para TLS (por defecto sin TLS). KNOCK_REDIS_CACERT  ruta al CA cuando se usa TLS.
//   KNOCK_REDIS_DB    base lógica de Redis (por defecto «0», la misma que usa el servicio).
// Solo datos de ejemplo ficticios: nada de personas reales.

const fs = require('fs');

async function deliver() {
  const env = process.env;
  if (!env.KNOCK_DB_URI) {
    print('Define KNOCK_DB_URI; la URI no se acepta como argumento.');
    quit(2);
  }

  const dir = (env.KNOCK_DB_DIR || 'db').replace(/[\\/]+$/, '');
  const marketName = env.KNOCK_DB_MARKET || 'market';
  if (!/^[A-Za-z0-9_-]{1,63}$/.test(marketName)) {
    print("Nombre de base no válido (letras, dígitos, '_' o '-').");
    quit(2);
  }

  // 1. Scripts numerados, en orden, con load(): pasa por el evaluador de mongosh (que vuelve síncronas las llamadas del shell). Corren en el
  //    mismo ámbito global, así que sus nombres de nivel superior (const) no deben repetirse entre scripts numerados.
  const scripts = fs.readdirSync(dir).filter((name) => /^\d{3}[_-].+\.js$/.test(name)).sort();
  if (scripts.length === 0) {
    print(`No hay scripts numerados en ${dir}.`);
    quit(2);
  }

  for (const name of scripts) {
    print(`> ${name}`);
    load(`${dir}/${name}`);
  }

  // 2. Datos de ejemplo del catálogo (upsert por clave natural, con los _id fijos de las semillas).
  const read = (file) => EJSON.parse(fs.readFileSync(`${dir}/seeds/catalog/${file}`, 'utf8'));
  const market = new Mongo(env.KNOCK_DB_URI).getDB(marketName);

  // Comparación estable por contenido: serializa con claves ordenadas para que el orden de campos de la base y del archivo no
  // produzca falsos positivos y la subida de `version` quede igual que la de CatalogSeeder.UpsertAsync (solo sube si cambia el contenido).
  const stable = (value) => {
    if (Array.isArray(value)) {
      return '[' + value.map(stable).join(',') + ']';
    }
    if (value && typeof value === 'object') {
      return '{' + Object.keys(value).sort().map((k) => JSON.stringify(k) + ':' + stable(value[k])).join(',') + '}';
    }
    return JSON.stringify(value);
  };

  const seed = (collectionName, documents, naturalKey) => {
    const collection = market.getCollection(collectionName);
    const written = [];
    for (const document of documents) {
      const { _id, ...content } = document;
      const filter = {};
      for (const field of naturalKey) {
        filter[field] = document[field];
      }

      const existing = collection.findOne(filter);
      if (!existing) {
        collection.insertOne({ _id, version: 1, ...content });
        written.push(document);
        continue;
      }

      const current = { ...existing };
      delete current._id;
      delete current.version;
      if (stable(current) !== stable(content)) {
        collection.replaceOne(filter, { _id, ...content, version: (existing.version || 0) + 1 });
        written.push(document);
      }
    }

    print(`${marketName}.${collectionName}: ${collection.countDocuments({})} documentos`);
    return written;
  };

  // Cobertura de invalidación igual a la siembra (CatalogSeeder): el CP anterior y los pares anteriores y actuales, no solo lo escrito.
  const snapshot = () => {
    const docs = market.getCollection('colonias').find({}, { cp: 1 }).toArray();
    return {
      postalCodes: [...new Set(docs.map((d) => d.cp))],
      pairs: docs.map((d) => ({ id: d._id.toString(), cp: d.cp })),
    };
  };
  const before = snapshot();

  const writtenOficios = seed('oficios', read('oficios.json'), ['clave']);
  const writtenColonias = seed('colonias', read('colonias.json'), ['municipio', 'cp', 'nombre']);

  const after = snapshot();
  const postalCodes = new Set([...before.postalCodes, ...after.postalCodes, ...writtenColonias.map((d) => d.cp)]);
  const pairKeys = new Map();
  for (const p of [...before.pairs, ...after.pairs]) {
    pairKeys.set(`${p.id}:${p.cp}`, p);
  }
  for (const d of writtenColonias) {
    const id = d._id.toString();
    pairKeys.set(`${id}:${d.cp}`, { id, cp: d.cp });
  }

  const invalidate = [['catalog:oficios', 'catalog:gen:oficios']];
  for (const cp of postalCodes) {
    invalidate.push([`catalog:colonias:cp:${cp}`, `catalog:gen:colonias:cp:${cp}`]);
  }
  for (const { id, cp } of pairKeys.values()) {
    invalidate.push([`catalog:colonias:existe:${id}:${cp}`, `catalog:gen:colonias:existe:${id}:${cp}`]);
  }

  // Con KNOCK_REDIS_HOST siempre se invalida, aunque no haya escrituras: reaplicar repara una caché que una corrida anterior dejó vieja.
  // Sin KNOCK_REDIS_HOST solo se exige Redis si esta corrida escribió algo; si no, avisa por si una corrida anterior terminó en 2 o 3.
  const writes = writtenOficios.length + writtenColonias.length;
  if (!env.KNOCK_REDIS_HOST && writes === 0) {
    print('Sin cambios en MongoDB: no hace falta invalidar Redis. Si una corrida anterior terminó con código 2 o 3, córrelo con KNOCK_REDIS_HOST.');
    print('Entrega de la base lista.');
    return;
  }

  if (!env.KNOCK_REDIS_HOST) {
    print(`Falta KNOCK_REDIS_HOST: el script escribió ${writes} documento(s) y ${invalidate.length} clave(s) requieren invalidación; no puede garantizarla sin Redis.`);
    print('Define KNOCK_REDIS_HOST (y KNOCK_REDIS_PORT / KNOCK_REDIS_PASSWORD / KNOCK_REDIS_TLS) y vuelve a correrlo.');
    quit(2);
  }

  try {
    await invalidateRedisKeys(invalidate, env);
  } catch (error) {
    print(`No se pudo invalidar Redis (${error.message}); MongoDB ya fue modificado: vuelve a correr el script con Redis disponible.`);
    quit(3);
  }
  print('Entrega de la base lista.');
}

async function invalidateRedisKeys(pairs, env) {
  const net = require('net');
  const tls = require('tls');
  const host = env.KNOCK_REDIS_HOST;
  const port = parseInt(env.KNOCK_REDIS_PORT || '6379', 10);
  const useTls = env.KNOCK_REDIS_TLS === '1';
  const db = parseInt(env.KNOCK_REDIS_DB || '0', 10);

  const connect = () => new Promise((resolve, reject) => {
    const options = { host, port };
    if (useTls) {
      options.ca = env.KNOCK_REDIS_CACERT ? fs.readFileSync(env.KNOCK_REDIS_CACERT) : undefined;
      options.rejectUnauthorized = true;
    }
    const socket = useTls ? tls.connect(options) : net.createConnection(options);
    socket.once('connect', () => resolve(socket));
    socket.once('error', reject);
    socket.once('secureConnect', () => resolve(socket));
  });

  const socket = await connect();
  let buffer = '';

  const encodeArg = (s) => `$${Buffer.byteLength(s)}\r\n${s}\r\n`;
  const send = (parts) => new Promise((resolve, reject) => {
    const msg = `*${parts.length}\r\n` + parts.map(encodeArg).join('');
    let timer;
    // Cada envío quita sus propios listeners al terminar (respuesta, error o tiempo agotado): sin eso se acumulan y Node avisa
    // MaxListenersExceededWarning con cientos de claves.
    const cleanup = () => {
      clearTimeout(timer);
      socket.removeListener('data', onData);
      socket.removeListener('error', onError);
    };
    const onError = (error) => {
      cleanup();
      reject(error);
    };
    const onData = (chunk) => {
      buffer += chunk.toString('utf8');
      const end = buffer.indexOf('\r\n');
      if (end === -1) {
        return;
      }
      const line = buffer.slice(0, end);
      buffer = buffer.slice(end + 2);
      cleanup();
      if (line.startsWith('-')) {
        reject(new Error(`Redis respondió error: ${line.slice(1)}`));
      } else {
        resolve(line);
      }
    };
    socket.on('data', onData);
    socket.on('error', onError);
    timer = setTimeout(() => {
      cleanup();
      reject(new Error('Tiempo agotado esperando respuesta de Redis'));
    }, 10000);
    socket.write(msg);
  });

  try {
    if (env.KNOCK_REDIS_PASSWORD) {
      await send(['AUTH', env.KNOCK_REDIS_PASSWORD]);
    }
    if (db !== 0) {
      await send(['SELECT', String(db)]);
    }
    // Misma operación atómica que RedisCatalogCache: INCR de generación + DEL de datos en un solo script Lua.
    const script = "redis.call('INCR', KEYS[2]); redis.call('DEL', KEYS[1]); return 1";
    for (const [dataKey, genKey] of pairs) {
      const reply = await send(['EVAL', script, '2', dataKey, genKey]);
      if (reply !== ':1') {
        throw new Error(`Invalidación no confirmada para ${dataKey} (respuesta: ${reply})`);
      }
    }
  } finally {
    socket.end();
  }
}

deliver().then(
  () => {},
  (error) => {
    print(`La entrega falló (${error.message}). Revisa el mensaje; si MongoDB ya se modificó, vuelve a correr el script.`);
    quit(1);
  },
);
