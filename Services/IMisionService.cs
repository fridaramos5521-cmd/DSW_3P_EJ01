using QuestLog.Models;
namespace QuestLog.Services;

public interface IMisionService
{
    Task<List<Mision>> ObtenerTodasAsync(CancellationToken ct = default);
    Task<Mision?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Mision> CrearAsync(Mision request, CancellationToken ct = default);
    Task ActualizarAsync(int id, Mision request, CancellationToken ct = default);
    Task EliminarAsync(int id, CancellationToken ct = default);
}
