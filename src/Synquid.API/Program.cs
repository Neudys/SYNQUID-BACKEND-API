using Microsoft.EntityFrameworkCore;
using Synquid.Application.Interfaces;
using Synquid.Infrastructure.Data;
using Synquid.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Base de datos PostgreSQL
builder.Services.AddDbContext<SynquidDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Servicios de aplicación
builder.Services.AddScoped<IAuditService, AuditService>();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Synquid API", Version = "v1" });
});

// CORS (para que el frontend Next.js pueda conectar)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

// SignalR (tiempo real)
builder.Services.AddSignalR();

var app = builder.Build();

// Swagger siempre activo (en desarrollo)
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Synquid API v1"));

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Aplicar migraciones autom�ticamente al arrancar
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SynquidDbContext>();
    db.Database.Migrate();
}

app.Run();