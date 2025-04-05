using APIFamilyMaster.security;
using APIFamilyMaster.data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using APIFamilyMaster.services;
using System.Net.Http.Headers;
using System.Net.Http;
using Serilog;
using Serilog.Events;


var builder = WebApplication.CreateBuilder(args);


//agrega el archivo para parametrizar (ExternalProperties.json)        true = archivo opcional o false = obligatorio para compilar
builder.Configuration.AddJsonFile("externalproperties/ExternalProperties.json", optional: false, reloadOnChange: true);


builder.Services.AddHttpClient();
builder.Services.AddRazorPages();
builder.Services.AddControllers();



//basic auth
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
builder.Services.AddHttpClient();


Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Agregar Serilog al host
builder.Host.UseSerilog();



// Configuraciï¿½n Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Family Master", Version = "v1" });
    options.AddSecurityDefinition("basic", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "basic",
        Description = "Autenticacion basica. Ingresa el usuario y la contraseña en el formato 'username:password'."
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

// Configuraciï¿½n del DbContext
builder.Services.AddDbContext<FamilyMasterContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// **Registrar el servicio `FamilyMasterService`**
builder.Services.AddScoped<FamilyMasterService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Family Master v1");
        c.RoutePrefix = string.Empty; // Para acceder a Swagger desde la raï¿½z
    });
}
else
{
    app.UseExceptionHandler("/Error");
 // app.UseHsts();
}




// Configuraciï¿½n del middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Autenticaciï¿½n
app.UseAuthorization(); // Autorizaciï¿½n

app.MapRazorPages();
app.MapControllers();

app.MapGet("/", () => "API FamilyMaster funcionando!");

_ = Task.Run(async () =>
{

    await Task.Delay(5000);
    await autoLlamado();
});

await app.RunAsync();

// metodo que hace el llamado al enpoint 
async Task autoLlamado()
{
    Console.WriteLine("Inicio Autollamado");
    Console.WriteLine("Inicio Autollamado");
    Console.WriteLine("Inicio Autollamado");
    Console.WriteLine("Inicio Autollamado");

    using var client = new HttpClient();

    var url = "http://apifamilymaster:8080/api/FamilyMaster/activarSiguienteTanda?numTandaActual=0";

    

    Console.WriteLine($"Llamando a {url}...");

    var usuario = "senad";
    var contrasena = "S3nad";
    var basicAuth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{usuario}:{contrasena}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);


    for (int i = 0; i < 10; i++)
    {
        try
        {
            var response = await client.PostAsync(url,null);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Autollamado exitoso");
                return;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Error en la llamada: {response.StatusCode}. Detalles: {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Servicio no disponible: {ex.Message}. Reintentando...");
        }
        await Task.Delay(5000);
    }

    Console.WriteLine("El servicio no está disponible después de múltiples intentos.");
}
