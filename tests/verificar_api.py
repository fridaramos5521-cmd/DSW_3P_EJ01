"""Prueba real de HTTP y reinicio. Requiere .NET 10 y Python 3; usa datos temporales."""
import argparse
import concurrent.futures
import json
import os
from pathlib import Path
import socket
import subprocess
import tempfile
import time
import urllib.error
import urllib.request

parser = argparse.ArgumentParser()
parser.add_argument('--dotnet', default='dotnet')
parser.add_argument('--exportar-ejemplo', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]
subprocess.run([args.dotnet, 'build', str(root / 'QuestLog.csproj'), '--nologo'], check=True)
with socket.socket() as sock:
    sock.bind(('127.0.0.1', 0))
    port = sock.getsockname()[1]
url = f'http://127.0.0.1:{port}/api/misiones'
checks = []
def check(condition, description):
    if not condition:
        raise AssertionError(description)
    checks.append(description)

def request(method, path='', body=None, raw=None):
    data = raw if raw is not None else (json.dumps(body).encode() if body is not None else None)
    req = urllib.request.Request(url + path, data=data, method=method,
                                 headers={'Content-Type': 'application/json'})
    try:
        response = urllib.request.urlopen(req, timeout=10)
    except urllib.error.HTTPError as error:
        response = error
    with response:
        content = response.read()
        return response.status, json.loads(content) if content else None, response.headers

with tempfile.TemporaryDirectory(prefix='questlog-test-') as directory:
    data_path = Path(directory) / 'misiones.json'
    env = dict(os.environ, ASPNETCORE_URLS=f'http://127.0.0.1:{port}',
               Persistencia__RutaArchivo=str(data_path), ASPNETCORE_ENVIRONMENT='Production')
    process = None
    log = open(Path(directory) / 'server.log', 'w+')
    def start():
        global process
        process = subprocess.Popen([args.dotnet, str(root / 'bin/Debug/net10.0/QuestLog.dll')],
                                   cwd=root, env=env, stdout=log, stderr=log)
        for _ in range(200):
            if process.poll() is not None:
                log.seek(0)
                raise RuntimeError(log.read())
            try:
                if request('GET')[0] == 200:
                    return
            except OSError:
                pass
            time.sleep(.1)
        raise RuntimeError('La API no inició en 20 segundos.')
    def stop():
        if process is not None and process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
    try:
        start()
        check(request('GET')[:2] == (200, []), 'GET inicial devuelve lista vacía')
        examples = [('Recolectar hierbas', 'Fácil', 100), ('Explorar la cueva', 'Media', 350),
                    ('Derrotar al dragón', 'Difícil', 1000)]
        missions = []
        for name, level, xp in examples:
            status, mission, headers = request('POST', body={
                'nombre': name, 'dificultad': level, 'recompensaXP': xp, 'completada': True})
            check(status == 201 and headers.get('Location', '').endswith('/' + str(mission['id'])),
                  'POST devuelve 201 y Location: ' + name)
            check(mission['completada'] is False, 'Nueva misión inicia sin completar: ' + name)
            missions.append(mission)
        check(len({m['id'] for m in missions}) == 3, 'Identificadores únicos')
        status, saved, _ = request('GET')
        stop()
        start()
        check(request('GET')[:2] == (200, saved), 'Tres misiones idénticas después de reiniciar el proceso')
        if args.exportar_ejemplo:
            # Exportación de los registros creados por POST, comprobados tras el reinicio.
            (root / 'Data/misiones.json').write_text(data_path.read_text(), encoding='utf-8')
        first_id = missions[0]['id']
        check(request('GET', f'/{first_id}')[1] == missions[0], 'GET por identificador')
        valid = {'nombre': 'Misión actualizada', 'dificultad': 'Media', 'recompensaXP': 0, 'completada': True}
        check(request('PUT', f'/{first_id}', valid)[0] == 204, 'PUT devuelve 204')
        check(request('GET', f'/{first_id}')[1]['completada'] is True, 'PUT permite completar una misión')
        for patch in [{'nombre': ''}, {'nombre': '   '}, {'nombre': None},
                      {'dificultad': 'Imposible'}, {'dificultad': None}, {'recompensaXP': -1}]:
            body = valid | patch
            check(request('POST', body=body)[0] == 400, 'POST rechaza ' + repr(patch))
            check(request('PUT', f'/{first_id}', body)[0] == 400, 'PUT rechaza ' + repr(patch))
        check(request('POST', raw=b'{')[0] == 400, 'JSON mal formado devuelve 400')
        check(request('POST', raw=b'null')[0] == 400, 'Cuerpo nulo devuelve 400')
        check(request('GET', '/999999')[0] == 404, 'GET inexistente devuelve 404')
        check(request('PUT', '/999999', valid)[0] == 404, 'PUT inexistente devuelve 404')
        check(request('DELETE', '/999999')[0] == 404, 'DELETE inexistente devuelve 404')
        check(len(request('GET')[1]) == 3, 'Peticiones inválidas no agregan ni eliminan datos')
        with concurrent.futures.ThreadPoolExecutor(max_workers=8) as pool:
            results = list(pool.map(lambda _: request('POST', body=valid), range(12)))
        ids = [r[1]['id'] for r in results]
        check(all(r[0] == 201 for r in results) and len(set(ids)) == 12,
              '12 altas concurrentes sin duplicar identificadores')
        check(len(request('GET')[1]) == 15, 'No se pierden registros con altas concurrentes')
        for mission_id in ids:
            check(request('DELETE', f'/{mission_id}')[0] == 204, 'DELETE devuelve 204')
        check(request('DELETE', f'/{ids[0]}')[0] == 404, 'Segunda eliminación devuelve 404')
        stop()
        start()
        final = request('GET')[1]
        check(len(final) == 3 and final[0]['completada'] is True,
              'Cambios y eliminaciones permanecen tras segundo reinicio')
    finally:
        stop()
        log.close()
print('\n'.join('OK - ' + c for c in checks))
print(f'RESULTADO: {len(checks)} verificaciones correctas.')
