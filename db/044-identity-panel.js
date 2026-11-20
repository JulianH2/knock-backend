// Spec 033 (KNOCK-75): colecciones del panel interno en la base del servicio de Identidad y sus índices.
// Uso (igual que 043-bloqueos.js):
//   KNOCK_DB_URI=... mongosh --nodb --quiet --file db/044-identity-panel.js
// Con `mongosh "<cadena>" --file …` (sin KNOCK_DB_URI) usa la conexión abierta.
// La base se toma de KNOCK_DB_IDENTITY (por defecto «identity»). Es idempotente: crea lo que falta y nunca borra ni cambia colecciones, índices ni documentos existentes.
// La definición debe coincidir con los EnsureIndexesAsync de PanelCuentasStore, PanelSesionesStore, PanelActivacionesStore y PanelAuditoriaStore.
// No hay semillas ni secretos: la cuenta de prueba se crea con la herramienta interna (`panel alta --secreto-de-prueba`) solo en Development.
const panelDbName = process.env.KNOCK_DB_IDENTITY || "identity";
if (!/^[A-Za-z0-9_-]{1,63}$/.test(panelDbName)) {
  print("Nombre de base no válido (letras, dígitos, '_' o '-').");
  quit(2);
}

const panelIdentity = process.env.KNOCK_DB_URI ? new Mongo(process.env.KNOCK_DB_URI).getDB(panelDbName) : db.getSiblingDB(panelDbName);

for (const name of ["panel_cuentas", "panel_sesiones", "panel_activaciones", "panel_auditoria"]) {
  if (!panelIdentity.getCollectionNames().includes(name)) {
    panelIdentity.createCollection(name);
  }
}

// panel_cuentas: una cuenta por `sub` (el usuarios.sub de la app).
panelIdentity.panel_cuentas.createIndex({ sub: 1 }, { name: "sub_1", unique: true });

// panel_sesiones: huella vigente única, huellas usadas (multiclave), conteo y revocación por cuenta, y vencimiento (TTL) en `venceEn`.
panelIdentity.panel_sesiones.createIndex({ huellaRenovacion: 1 }, { name: "huellaRenovacion_1", unique: true });
panelIdentity.panel_sesiones.createIndex({ huellasUsadas: 1 }, { name: "huellasUsadas_1" });
panelIdentity.panel_sesiones.createIndex({ sub: 1, revocada: 1, creadaEn: 1 }, { name: "sub_1_revocada_1_creadaEn_1" });
panelIdentity.panel_sesiones.createIndex({ venceEn: 1 }, { name: "venceEn_1", expireAfterSeconds: 0 });

// panel_activaciones: huella única y una invitación por cuenta (la nueva reemplaza a la anterior); se borra a las 24 horas de crearse.
panelIdentity.panel_activaciones.createIndex({ huella: 1 }, { name: "huella_1", unique: true });
panelIdentity.panel_activaciones.createIndex({ cuentaId: 1 }, { name: "cuentaId_1", unique: true });
panelIdentity.panel_activaciones.createIndex({ creadaEn: 1 }, { name: "creadaEn_1", expireAfterSeconds: 86400 });

// panel_auditoria: solo inserción para el servicio; vence a los 365 días; consultas de revisión por persona y por evento.
panelIdentity.panel_auditoria.createIndex({ fecha: 1 }, { name: "fecha_1", expireAfterSeconds: 31536000 });
panelIdentity.panel_auditoria.createIndex({ sub: 1, fecha: -1 }, { name: "sub_1_fecha_-1" });
panelIdentity.panel_auditoria.createIndex({ evento: 1, fecha: -1 }, { name: "evento_1_fecha_-1" });

print(`Colecciones e índices del panel listos en «${panelDbName}»`);
