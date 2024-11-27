using APIWaveRelease.data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Newtonsoft.Json;
using APIWaveRelease.security;
using Microsoft.AspNetCore.Authentication;



var builder = WebApplication.CreateBuilder(args);


//agrega el archivo para parametrizar (ExternalProperties.json)        true = archivo opcional o false = obligatorio para compilar
builder.Configuration.AddJsonFile("externalproperties/ExternalProperties.json", optional: false, reloadOnChange: true);


// Configurar servicios
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver();
    });
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
//   x.TokenValidationParameters = new TokenValidationParameters
//  {
//      ValidateIssuerSigningKey = true,
//      IssuerSigningKey = new SymmetricSecurityKey(key),
//      ValidateIssuer = true,
//      ValidIssuer = builder.Configuration["Jwt:Issuer"], // Aseg�rate de que este valor coincida
//     ValidateAudience = false
//  };
// });

//basic auth
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
builder.Services.AddHttpClient();

// Configuraci�n Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Wave Release", Version = "v1" });
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
builder.Services.AddDbContext<WaveReleaseContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthorization();

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
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Autenticaci�n
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();
