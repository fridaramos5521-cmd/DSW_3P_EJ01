using QuestLog.Filters;
using QuestLog.Repositories;
using QuestLog.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers(options => options.Filters.Add<BusinessExceptionFilter>());
// Una única instancia comparte el bloqueo de acceso al archivo entre peticiones.
builder.Services.AddSingleton<IMisionRepository, MisionJsonRepository>();
builder.Services.AddScoped<IMisionService, MisionService>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();
app.MapControllers();
app.Run();
