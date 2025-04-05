using APIWaveRelease.data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Newtonsoft.Json;
using APIWaveRelease.security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Serilog.Events;
using Serilog;



var builder = WebApplication.CreateBuilder(args);

//agrega el archivo para parametrizar (ExternalProperties.json)        true = archivo opcional o false = obligatorio para compilar
builder.Configuration.AddJsonFile("externalproperties/ExternalProperties.json", optional: false, reloadOnChange: true);

//TEST
builder.Services.AddRazorPages(options => {
    options.Conventions.AllowAnonymousToPage("/api/WaveRelease/EnviarWave");
    options.Conventions.AuthorizePage("/api/WaveRelease/EnviarWave", "PublicAccess");
});
///

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader()
               .WithExposedHeaders("WWW-Authenticate");
    });
});

// Configurar Data Protection para persistir claves en Docker
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("APIWaveRelease");


// Configurar servicios
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });

builder.Services.AddHttpClient();


//basic auth
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);

//test
// Política por defecto: requiere autenticación
builder.Services.AddAuthorization(options => {
    // Política por defecto: requiere autenticación
    options.DefaultPolicy = new AuthorizationPolicyBuilder("BasicAuthentication")
        .RequireAuthenticatedUser()
        .Build();

    // Política para rutas públicas (sin autenticación)
    options.AddPolicy("PublicAccess", policy =>
        policy.RequireAssertion(context => true));
});
///



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


// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Wave Release", Version = "v1" });
    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Autenticación básica. Ingresa usuario y contraseña en formato 'username:password'."
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "basic" }
            },
            new string[] { }
        }
    });
});

// Registrar el contexto de la base de datos
builder.Services.AddDbContext<WaveReleaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API WaveRelease v1");
        c.RoutePrefix = string.Empty;
    });
}


// Configurar middleware
//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseCors("AllowAll");

app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
    endpoints.MapControllers();
});

app.Run();