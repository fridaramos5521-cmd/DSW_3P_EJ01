using Microsoft.AspNetCore.Mvc;
using QuestLog.Models;
using QuestLog.Services;

namespace QuestLog.Controllers;

[ApiController]
[Route("api/misiones")]
public class MisionesController(IMisionService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<Mision>>> ObtenerTodas(CancellationToken ct) =>
        Ok(await service.ObtenerTodasAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Mision>> ObtenerPorId(int id, CancellationToken ct)
    {
        var mision = await service.ObtenerPorIdAsync(id, ct);
        if (mision is null) return NotFound(new ProblemDetails
        {
            Status = 404, Title = "Misión no encontrada", Detail = "La misión no existe."
        });
        return Ok(mision);
    }

    [HttpPost]
    public async Task<ActionResult<Mision>> Crear([FromBody] Mision request, CancellationToken ct)
    {
        var mision = await service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(ObtenerPorId), new { id = mision.Id }, mision);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, [FromBody] Mision request, CancellationToken ct)
    {
        await service.ActualizarAsync(id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Eliminar(int id, CancellationToken ct)
    {
        await service.EliminarAsync(id, ct);
        return NoContent();
    }
}
