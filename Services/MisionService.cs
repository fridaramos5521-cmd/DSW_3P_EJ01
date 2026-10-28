using System.ComponentModel.DataAnnotations;
using QuestLog.DTOs;
using QuestLog.Models;
using QuestLog.Repositories;

namespace QuestLog.Services;

public class MisionService(IMisionRepository repository) : IMisionService
{
    private static Mision ValidarYCrear(MisionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ValidationException("El nombre de la misión es obligatorio.");
        if (request.Dificultad is not ("Fácil" or "Media" or "Difícil"))
            throw new ValidationException("La dificultad debe ser Fácil, Media o Difícil.");
        if (request.RecompensaXP < 0)
            throw new ValidationException("La recompensa XP no puede ser negativa.");
        return new Mision
        {
            Nombre = request.Nombre.Trim(),
            Dificultad = request.Dificultad,
            RecompensaXP = request.RecompensaXP,
            Completada = request.Completada
        };
    }

    public Task<List<Mision>> ObtenerTodasAsync(CancellationToken ct = default) =>
        repository.ObtenerTodasAsync(ct);

    public Task<Mision?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        repository.ObtenerPorIdAsync(id, ct);

    public Task<Mision> CrearAsync(MisionRequest request, CancellationToken ct = default)
    {
        var mision = ValidarYCrear(request);
        mision.Completada = false;
        return repository.CrearAsync(mision, ct);
    }

    public async Task ActualizarAsync(int id, MisionRequest request, CancellationToken ct = default)
    {
        if (await repository.ObtenerPorIdAsync(id, ct) is null)
            throw new KeyNotFoundException("La misión no existe.");
        var mision = ValidarYCrear(request);
        mision.Id = id;
        if (!await repository.ActualizarAsync(mision, ct))
            throw new KeyNotFoundException("La misión no existe.");
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        if (!await repository.EliminarAsync(id, ct))
            throw new KeyNotFoundException("La misión no existe.");
    }
}
