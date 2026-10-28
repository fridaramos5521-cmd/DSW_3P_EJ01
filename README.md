# DSW_3P_PR01 – QuestLog (Etapa 1)

API REST de misiones con ASP.NET Core 10, controladores MVC, Repository y Service. Esta entrega implementa exclusivamente persistencia JSON.

## Ejecutar

Requisitos: SDK de .NET 10. Python 3 es opcional, solo para la prueba automática.

Abre una terminal en esta carpeta (la que contiene `QuestLog.csproj`):

```sh
dotnet build
dotnet run
```

La API escucha en `http://localhost:5050`. Puedes abrir `http://localhost:5050/api/misiones` en el navegador para consultar los registros. La raíz `/` no tiene página web ni Swagger. Para las demás operaciones usa el archivo `QuestLog.http` en un cliente compatible, Postman o curl. Detén la aplicación con Ctrl+C.

## Organización y responsabilidades

- `Models/Mision.cs`: entidad con las cinco propiedades solicitadas.
- `DTOs/MisionRequest.cs`: datos de entrada; el servidor asigna el Id.
- `Controllers/MisionesController.cs`: recibe HTTP y devuelve los resultados.
- `Services/IMisionService.cs` y `MisionService.cs`: reglas de negocio.
- `Repositories/IMisionRepository.cs` y `MisionJsonRepository.cs`: contrato y almacenamiento.
- `Filters/BusinessExceptionFilter.cs`: convierte errores de negocio en respuestas 400 o 404.
- `Program.cs`: registra dependencias y rutas de controladores.
- `Data/misiones.json`: tres registros creados mediante POST y comprobados después de reiniciar.
- `tests/verificar_api.py`: verificación real de la API y de su persistencia.
- `VERIFICACION.txt`: salida de las pruebas ejecutadas para esta entrega.

En una API MVC, el controlador devuelve representaciones JSON; no se necesitan vistas HTML.

## Reglas de negocio

El servicio rechaza nombres nulos, vacíos o formados por espacios; guarda los nombres recortados. La dificultad debe coincidir exactamente con `Fácil`, `Media` o `Difícil` (incluye acentos y mayúsculas). RecompensaXP admite cero y valores positivos.

En POST, Completada se establece siempre en false aunque el cliente envíe true. PUT reemplaza los campos editables y permite marcar la misión como completada. El Id proviene de la ruta en PUT y lo genera el repositorio en POST. Una misión inexistente nunca se crea mediante PUT.

## Rutas y códigos HTTP

| Método | Ruta | Resultado |
| --- | --- | --- |
| GET | /api/misiones | 200 y lista (vacía si no hay registros) |
| GET | /api/misiones/{id} | 200 y misión, o 404 |
| POST | /api/misiones | 201, misión creada y cabecera Location; 400 si los datos son inválidos |
| PUT | /api/misiones/{id} | 204 sin cuerpo; 400 si los datos son inválidos; 404 si no existe |
| DELETE | /api/misiones/{id} | 204 sin cuerpo; 404 si no existe |

Un cuerpo nulo o JSON mal formado devuelve 400. Los errores inesperados devuelven 500 mediante el manejador de excepciones, sin exponer el contenido del archivo.

Ejemplo de alta:

```json
{
  "nombre": "Recolectar hierbas",
  "dificultad": "Fácil",
  "recompensaXP": 100,
  "completada": true
}
```

La respuesta de esta alta tendrá `completada: false`.

## Persistencia y comprobación manual

La ruta se configura en `appsettings.json` y se resuelve respecto a la carpeta del proyecto. El archivo se lee en cada operación. No se borra ni se inicializa de nuevo al arrancar. Si se elimina el archivo, la próxima alta creará uno nuevo.

1. Ejecuta `dotnet run` y consulta GET `/api/misiones`. La entrega incluye tres ejemplos.
2. Usa los tres POST de `QuestLog.http` para registrar tus propias misiones; conserva los Id devueltos.
3. Detén la aplicación, revisa `Data/misiones.json` y vuelve a ejecutar `dotnet run`.
4. Repite GET y comprueba que las misiones e identificadores siguen presentes.
5. Prueba PUT y DELETE con los identificadores recibidos y con uno inexistente.

La escritura usa un archivo temporal y reemplaza el original una vez completada la serialización. Un bloqueo compartido protege cada lectura o modificación dentro del proceso. Esta implementación está pensada para una instancia local de la API; varias instancias no deben escribir el mismo archivo. Un JSON dañado produce un error; no se reemplaza silenciosamente por una lista vacía.

## Pruebas automáticas

Desde esta carpeta:

```sh
python tests/verificar_api.py
```

En sistemas donde el comando es `python3`, úsalo en lugar de `python`. El script compila el proyecto, inicia la API en un puerto libre y usa un archivo temporal independiente de los datos de ejemplo. Comprueba las cinco operaciones, entradas inválidas, estado inicial, errores 404, concurrencia y dos reinicios. Detiene su proceso al terminar.

Para indicar una ruta especial de dotnet:

```sh
python tests/verificar_api.py --dotnet /ruta/al/ejecutable/dotnet
```

La opción `--exportar-ejemplo` reemplaza `Data/misiones.json` con los tres registros creados durante la prueba; se utilizó para generar los ejemplos incluidos.

## Git

La carpeta entregada incluye el repositorio local y el commit requerido. Puedes comprobarlo con:

```sh
git log -1 --oneline
git status --short
```

Mensaje del commit:

```text
feat: implementar QuestLog con persistencia JSON
```

Si tu descompresor omite el repositorio Git, puedes inicializar uno nuevo con `git init`, `git add .` y `git commit -m "feat: implementar QuestLog con persistencia JSON"`.

## Preparación de la etapa 2

El controlador recibe IMisionService y el servicio recibe IMisionRepository por constructor. Para la siguiente etapa se implementará el mismo contrato en un repositorio de EF Core, se registrará su DbContext y se sustituirá la implementación de IMisionRepository en Program.cs. No es necesario cambiar MisionesController ni MisionService. Los métodos asíncronos permiten usar las operaciones asíncronas de EF Core.

Referencia oficial: [Inyección de dependencias en controladores de ASP.NET Core](https://learn.microsoft.com/aspnet/core/mvc/controllers/dependency-injection?view=aspnetcore-10.0).
