#!/usr/bin/env bash
set -euo pipefail

# Medición secuencial local. La invalidación y sus comprobaciones no se cronometran.
cd "$(dirname "$0")/../.."
python3 - <<'PY'
import collections
import json
import os
import pathlib
import subprocess
import sys
import tempfile
from urllib.parse import urlsplit

base = os.environ.get('KNOCK_BASE_URL', 'http://localhost:5234')
url = urlsplit(base)
if (url.scheme != 'http' or url.hostname not in ('localhost', '127.0.0.1')
        or url.username is not None or url.password is not None
        or url.path not in ('', '/') or url.query or url.fragment):
    sys.exit('KNOCK_BASE_URL debe ser únicamente http://localhost[:puerto] o http://127.0.0.1[:puerto]')
endpoint = base.rstrip('/') + '/api/v1/oficios'
compose = ['docker', 'compose', '-f', 'deploy/docker-compose.local.yml']
key = 'catalog:oficios'

def run(args):
    return subprocess.run(args, check=True, text=True, capture_output=True).stdout.strip()

# No borrar claves en un daemon remoto ni en un Redis publicado en una interfaz pública.
context = run(['docker', 'context', 'show'])
details = json.loads(run(['docker', 'context', 'inspect', context]))[0]
docker_host = os.environ.get('DOCKER_HOST', details['Endpoints']['docker']['Host'])
if not docker_host.startswith('unix://'):
    sys.exit('Docker debe usar un daemon local por socket Unix')
container = run(compose + ['ps', '-q', 'redis'])
if not container:
    sys.exit('Redis del compose no está iniciado')
inspect = json.loads(run(['docker', 'inspect', container]))[0]
ports = inspect['NetworkSettings']['Ports'].get('6379/tcp') or []
if not ports or any(p['HostIp'] != '127.0.0.1' for p in ports):
    sys.exit('Redis debe estar publicado solo en 127.0.0.1')

def redis(*args):
    return run(compose + ['exec', '-T', 'redis', 'redis-cli', '--raw', *args])

def exists():
    result = redis('EXISTS', key)
    if result not in ('0', '1'):
        raise RuntimeError('Redis no respondió a EXISTS')
    return result == '1'

def request(etag=None):
    with tempfile.TemporaryDirectory() as directory:
        headers = pathlib.Path(directory) / 'headers'
        command = ['curl', '--noproxy', '*', '--silent', '--show-error', '--max-time', '30',
                   '-o', os.devnull, '-D', str(headers), '-w', '%{http_code} %{time_total}']
        if etag:
            command += ['-H', 'If-None-Match: ' + etag]
        status, seconds = run(command + [endpoint]).split()
        fields = {}
        for line in headers.read_text().splitlines():
            if ':' in line:
                name, value = line.split(':', 1)
                fields[name.lower()] = value.strip()
        return int(status), float(seconds) * 1000, fields.get('etag')

def percentile(values, p):
    ordered = sorted(values)
    index = (len(ordered) - 1) * p
    lower = int(index)
    upper = min(lower + 1, len(ordered) - 1)
    return ordered[lower] + (ordered[upper] - ordered[lower]) * (index - lower)

print('| Modo | Caché | p50 (ms) | p95 (ms) | p99 (ms) | n | Estados |', flush=True)
print('|---|---|---:|---:|---:|---:|---|', flush=True)
for mode in (200, 304):
    for temperature in ('fría', 'caliente'):
        # Esta lectura también calienta antes de la pasada caliente.
        status, _, etag = request()
        if status != 200 or not etag or not exists():
            sys.exit('No se obtuvo ETag o Market no pobló el Redis local esperado')
        samples, statuses = [], collections.Counter()
        for _ in range(100):
            if temperature == 'fría':
                redis('DEL', key)
                if exists():
                    sys.exit('La clave no quedó vacía antes de la petición')
            elif not exists():
                sys.exit('La clave caliente desapareció antes de la petición')
            status, duration, response_etag = request(etag if mode == 304 else None)
            if status != mode or response_etag != etag:
                sys.exit(f'Estado o ETag inesperado: se esperaba {mode}, se recibió {status}')
            if not exists():
                sys.exit('La petición no pobló la caché local de oficios')
            samples.append(duration)
            statuses[status] += 1
        states = ', '.join(f'{status}: {count}' for status, count in sorted(statuses.items()))
        print(f'| {mode} | {temperature} | {percentile(samples, .50):.3f} | '
              f'{percentile(samples, .95):.3f} | {percentile(samples, .99):.3f} | '
              f'{len(samples)} | {states} |', flush=True)
PY
