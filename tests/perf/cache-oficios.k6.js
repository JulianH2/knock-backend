// Spec 010, T-07 (R-04): prueba de carga de la caché del catálogo con k6.
// Lee el catálogo público de oficios (lectura repetida GET /api/v1/oficios) y distingue
// la caché fría de la caliente, los 200 de los 304 válidos y la compresión br/gzip.
// Solo hace GET públicos: sin credenciales, sin escrituras y sin tocar Redis.
// Variables de entorno:
//   KNOCK_BASE_URL  Base URL del servicio Market (sin barra final). Defecto: http://localhost:5234
//   KNOCK_VUS       Usuarios virtuales simultáneos. Defecto: 5
//   KNOCK_DURATION  Duración de la ronda caliente (la fría tarda el 20 % de ella). Defecto: 1m
//   KNOCK_RUN_LABEL Etiqueta de la corrida (p. ej. ttl-86400) para comparar vigencias.
//   KNOCK_P95_MS    Umbral p95 en milisegundos. Defecto: 500
//   KNOCK_MODE      Opcional: 200 sin condicional o 304 con ETag de setup; una ronda.
// Requiere un entorno local aislado: nunca correr contra producción ni servicios compartidos.

import http from 'k6/http';
import { check, sleep } from 'k6';
import { Counter, Rate, Trend } from 'k6/metrics';

const BASE_URL = (__ENV.KNOCK_BASE_URL || 'http://localhost:5234').replace(/\/$/, '');
const VUS = Number(__ENV.KNOCK_VUS || '5');
const DURATION = __ENV.KNOCK_DURATION || '1m';
const RUN_LABEL = __ENV.KNOCK_RUN_LABEL || 'sin-etiqueta';
const P95_MS = Number(__ENV.KNOCK_P95_MS || '500');
const MODE = __ENV.KNOCK_MODE || '';
if (MODE && MODE !== '200' && MODE !== '304') {
    throw new Error('KNOCK_MODE debe ser 200 o 304');
}
if (MODE && !/^http:\/\/(localhost|127\.0\.0\.1)(:\d+)?$/.test(BASE_URL)) {
    throw new Error('KNOCK_MODE solo admite Market local por HTTP');
}

// La 304 es una respuesta válida del flujo ETag, no un error.
const ERROR_RATE = new Rate('error_rate');
const COLD_MS = new Trend('cache_cold_duration_ms');
const WARM_MS = new Trend('cache_warm_duration_ms');
const OK_200 = new Counter('etag_200');
const NOT_MODIFIED_304 = new Counter('etag_304');
const OTHER_STATUS = new Counter('otros_estados');
const COMPRESSED = new Counter('compressed_br_or_gzip');
const BYTES_SAVED = new Trend('compressed_body_bytes');

export const options = {
    summaryTrendStats: ['count', 'p(50)', 'p(95)', 'p(99)'],
    scenarios: MODE ? {
        measurement: { executor: 'constant-vus', vus: VUS, duration: DURATION, exec: 'measurement' },
    } : {
        // Ronda fría (arranca primero, dura la quinta parte): primer acceso a la caché.
        cold: {
            executor: 'constant-vus',
            vus: Math.max(1, Math.ceil(VUS / 2)),
            duration: '20s',
            exec: 'cold',
            tags: { stage: 'fria' },
        },
        // Ronda caliente (tras la fría): la caché ya tiene el catálogo y las ETag cacheadas.
        warm: {
            executor: 'constant-vus',
            vus: VUS,
            duration: DURATION,
            startTime: '30s',
            exec: 'warm',
            tags: { stage: 'caliente' },
        },
    },
    thresholds: MODE ? {
        http_req_failed: ['rate<0.01'],
        error_rate: ['rate<0.01'],
        checks: ['rate==1'],
        'http_req_duration{scenario:measurement}': [`p(95)<${P95_MS}`],
    } : {
        http_req_failed: ['rate<0.01'],
        error_rate: ['rate<0.01'],
        [`http_req_duration{scenario:warm}`]: [`p(95)<${P95_MS}`],
        [`http_req_duration{scenario:cold}`]: [`p(95)<${P95_MS}`],
    },
};

let etag = null;

export function setup() {
    if (!MODE) return null;
    const response = http.get(`${BASE_URL}/api/v1/oficios`, { tags: { phase: 'setup' } });
    const currentEtag = response.headers.ETag || response.headers.Etag;
    if (response.status !== 200 || !currentEtag) throw new Error('No se obtuvo una ETag vigente');
    return { etag: currentEtag };
}

export function measurement(data) {
    oficios('medicion', data.etag);
}

export function cold() {
    oficios('fria');
}

export function warm() {
    oficios('caliente');
}

function oficios(stage, setupEtag) {
    const headers = { 'Accept-Encoding': 'gzip, deflate, br' };
    if (MODE === '304' || (!MODE && etag)) {
        headers['If-None-Match'] = MODE ? setupEtag : etag;
    }

    const res = http.get(`${BASE_URL}/api/v1/oficios`, { headers, tags: { runlabel: RUN_LABEL } });

    check(res, {
        'status 200 o 304 válido': (r) => r.status === 200 || r.status === 304,
        'nunca 4xx ni 5xx': (r) => !(r.status >= 400 && r.status < 600),
        'estado esperado del modo': (r) => !MODE || r.status === Number(MODE),
    });

    if (res.status === 200) {
        OK_200.add(1);
        etag = res.headers['ETag'] || res.headers['Etag'] || null;
        const encoding = (res.headers['Content-Encoding'] || '').toLowerCase();
        if (encoding.includes('br') || encoding.includes('gzip')) {
            COMPRESSED.add(1);
            // Tamaño sin comprimir si el cliente no lo reporta; k6 entrega el cuerpo ya decodificado.
            BYTES_SAVED.add((res.body || '').length);
        }
    } else if (res.status === 304) {
        NOT_MODIFIED_304.add(1);
        // Sin cuerpo ni compresión: la ETag se confirma sin gastar ancho de banda.
    } else {
        OTHER_STATUS.add(1, { status: String(res.status) });
    }

    if (!MODE) (stage === 'fria' ? COLD_MS : WARM_MS).add(res.timings.duration);
    ERROR_RATE.add(MODE ? res.status !== Number(MODE) : res.status !== 200 && res.status !== 304);
    sleep(0.1);
}
