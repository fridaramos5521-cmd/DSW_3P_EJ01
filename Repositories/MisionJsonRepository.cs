using System.Text.Json;
using QuestLog.Models;

namespace QuestLog.Repositories;

public sealed class MisionJsonRepository : IMisionRepository
{
    private readonly string _ruta;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };

    public MisionJsonRepository(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _ruta = Path.GetFullPath(Path.Combine(environment.ContentRootPath,
            configuration["Persistencia:RutaArchivo"] ?? "Data/misiones.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(_ruta)!);
    }

    private async Task<List<Mision>> LeerAsync(CancellationToken ct)
    {
        if (!File.Exists(_ruta)) return new List<Mision>();
        await using var stream = File.OpenRead(_ruta);
        // Un archivo corrupto no se interpreta como vacío para evitar pérdida de datos.
        return await JsonSerializer.DeserializeAsync<List<Mision>>(stream, _options, ct)
            ?? throw new InvalidDataException("El archivo de misiones no contiene una lista JSON.");
    }

    private async Task GuardarAsync(List<Mision> misiones, CancellationToken ct)
    {
        var temporal = _ruta + ".tmp";
        try
        {
            await using (var stream = File.Create(temporal))
                await JsonSerializer.SerializeAsync(stream, misiones, _options, ct);
            File.Move(temporal, _ruta, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporal)) File.Delete(temporal);
        }
    }

    public async Task<List<Mision>> ObtenerTodasAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try { return await LeerAsync(ct); }
        finally { _lock.Release(); }
    }

    public async Task<Mision?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var misiones = await ObtenerTodasAsync(ct);
        return misiones.FirstOrDefault(m => m.Id == id);
    }

    public async Task<Mision> CrearAsync(Mision mision, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var misiones = await LeerAsync(ct);
            mision.Id = checked(misiones.Select(m => m.Id).DefaultIfEmpty(0).Max() + 1);
            misiones.Add(mision);
            await GuardarAsync(misiones, ct);
            return mision;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> ActualizarAsync(Mision mision, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var misiones = await LeerAsync(ct);
            var index = misiones.FindIndex(m => m.Id == mision.Id);
            if (index < 0) return false;
            misiones[index] = mision;
            await GuardarAsync(misiones, ct);
            return true;
        }
        finally { _lock.Release(); }
    }

    public async Task<bool> EliminarAsync(int id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var misiones = await LeerAsync(ct);
            if (misiones.RemoveAll(m => m.Id == id) == 0) return false;
            await GuardarAsync(misiones, ct);
            return true;
        }
        finally { _lock.Release(); }
    }
}
