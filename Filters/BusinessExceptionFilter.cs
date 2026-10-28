using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuestLog.Filters;

// Traduce errores del servicio a HTTP sin acoplar el servicio a MVC.
public class BusinessExceptionFilter : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        int status;
        if (context.Exception is ValidationException) status = StatusCodes.Status400BadRequest;
        else if (context.Exception is KeyNotFoundException) status = StatusCodes.Status404NotFound;
        else return;
        context.Result = new ObjectResult(new ProblemDetails
        {
            Status = status,
            Title = status == 400 ? "Datos inválidos" : "Misión no encontrada",
            Detail = context.Exception.Message
        }) { StatusCode = status };
        context.ExceptionHandled = true;
    }
}
