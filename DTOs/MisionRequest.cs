namespace QuestLog.DTOs;

// El identificador lo asigna el servidor. En POST se ignora Completada.
public class MisionRequest
{
    public string? Nombre { get; set; }
    public string? Dificultad { get; set; }
    public int RecompensaXP { get; set; }
    public bool Completada { get; set; }
}
