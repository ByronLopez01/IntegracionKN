using APIOrderUpdate.data;
using APIOrderUpdate.security;
using APIOrderUpdate.services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog.Events;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//agrega el archivo para parametrizar (ExternalProperties.json)        true = archivo opcional o false = obligatorio para compilar
builder.Configuration.AddJsonFile("externalproperties/ExternalProperties.json", optional: false, reloadOnChange: true);

builder.Services.AddScoped<IOrderCancelService, OrderCancelService>();
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddHttpClient();

//basic auth
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
builder.Services.AddHttpClient();


// Config Logs
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Agregar Serilog al host
builder.Host.UseSerilog();


// Configuraci�n Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Order Update ", Version = "v1" });
    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Autenticacion basica. Ingresa el usuario y la contrasena en el formato 'username:password'."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "basic"
                }
            },
            new string[] { }
        }
    });
});
// Configuraci�n del DbContext
builder.Services.AddDbContext<OrderUpdateContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Update API v1");
        c.RoutePrefix = string.Empty; // Para acceder a Swagger desde la ra�z
    });
}
else
{
    app.UseExceptionHandler("/Error");
  //  app.UseHsts();
}

// Configuraci�n del middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Autenticaci�n
app.UseAuthorization(); // Autorizaci�n

app.MapRazorPages();
app.MapControllers();

app.Run();
