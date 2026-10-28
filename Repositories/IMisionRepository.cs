using QuestLog.Models;
namespace QuestLog.Repositories;

public interface IMisionRepository
{
    Task<List<Mision>> ObtenerTodasAsync(CancellationToken ct = default);
    Task<Mision?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    Task<Mision> CrearAsync(Mision mision, CancellationToken ct = default);
    Task<bool> ActualizarAsync(Mision mision, CancellationToken ct = default);
    Task<bool> EliminarAsync(int id, CancellationToken ct = default);
}
