using APISenad.data;
using APISenad.security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configurar servicios
builder.Services.AddRazorPages();
builder.Services.AddControllers();

//para hacer la llamada a la api 
builder.Services.AddHttpClient();


// Configuraci�n de JWT
//var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
//builder.Services.AddAuthentication(x =>
//{
//  x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//  x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(x =>
//{
//  x.RequireHttpsMetadata = false; // true en producci�n
//  x.SaveToken = true;
//  x.TokenValidationParameters = new TokenValidationParameters
//  {
//     ValidateIssuerSigningKey = true,
//    IssuerSigningKey = new SymmetricSecurityKey(key),
//    ValidateIssuer = true,
//   ValidIssuer = builder.Configuration["Jwt:Issuer"], // Aseg�rate de que este valor coincida
//   ValidateAudience = false
// };
//});

//basic auth
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
builder.Services.AddHttpClient();

// Configuraci�n Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Senad ", Version = "v1" });
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

// Registrar el contexto de la base de datos
builder.Services.AddDbContext<SenadContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Senad v1");
        c.RoutePrefix = string.Empty;
    });
}

// Configurar middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Autenticaci�n
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();


app.MapGet("/", () => "API Senad funcionando!");


_ = Task.Run(async () =>
{

    await Task.Delay(5000);
    await autoLlamado(); 
});

await app.RunAsync();

// metodo que hace el llamado al enpoint 
async Task autoLlamado()
{
    using var client = new HttpClient();
    var url = "http://apisenad:8080/api/Senad/123";
    Console.WriteLine($"Llamando a {url}...");

    for (int i = 0; i < 10; i++) // Intentar hasta 10 veces
    {
        try
        {
            var response = await client.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Autollamado exitoso");
                return;
            }
            else
            {
                Console.WriteLine($"Error en la llamada: {response.StatusCode}. Reintentando...");
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Servicio no disponible: {ex.Message}. Reintentando...");
        }
        await Task.Delay(5000); // Esperar 5 segundos antes de reintentar
    }

    Console.WriteLine("El servicio no está disponible después de múltiples intentos.");
}