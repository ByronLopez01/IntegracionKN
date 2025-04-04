using APILPNPicking.data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.IO;
using APILPNPicking.security;
using Microsoft.AspNetCore.Authentication;
using Serilog.Events;
using Serilog;


var builder = WebApplication.CreateBuilder(args);

// Archivo de configuración
builder.Configuration.AddJsonFile("externalproperties/ExternalProperties.json", optional: false, reloadOnChange: true);


// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddHttpClient("apiWaveRelease", client =>
{
    client.BaseAddress = new Uri("http://apiwaverelease:8080");
});

builder.Services.AddHttpClient("apiFamilyMaster", client =>
{
    client.BaseAddress = new Uri("http://apifamilymaster:8080");
});



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


// Configuración cliente HTTP
builder.Services.AddHttpClient("apiLuca", m => { })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Acepta cualquier certificado SSL
        ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
    });

// Configuraci�n Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LPN Picking", Version = "v1" });
    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Autenticacion basica. Ingresa el usuario y la contrase�a en el formato 'username:password'."
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
builder.Services.AddDbContext<LPNPickingContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API LPN Picking v1");
        c.RoutePrefix = string.Empty; // Para acceder a Swagger desde la ra�z
    });
}
else
{
    app.UseExceptionHandler("/Error");
   // app.UseHsts();
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
