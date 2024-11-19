using APIOrderUpdate.data;
using APIOrderUpdate.security;
using APIOrderUpdate.services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IOrderCancelService, OrderCancelService>();
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();

builder.Services.AddHttpClient();


//var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Key"]);
//builder.Services.AddAuthentication(x =>
//{
//  x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
// x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//})
//.AddJwtBearer(x =>
//{
//  x.RequireHttpsMetadata = false; // Cambiar a true para producción
// x.SaveToken = true;
// x.TokenValidationParameters = new TokenValidationParameters
// {
//   ValidateIssuerSigningKey = true,
//   IssuerSigningKey = new SymmetricSecurityKey(key),
//   ValidateIssuer = true,
//  ValidIssuer = builder.Configuration["Jwt:Issuer"], 
//  ValidateAudience = false
// };
//});

//basic auth
builder.Services.AddAuthentication("BasicAuthentication")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
builder.Services.AddHttpClient();

// Configuración Swagger
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
                    Id = "Basic"
                }
            },
            new string[] { }
        }
    });
});
// Configuración del DbContext
builder.Services.AddDbContext<OrderUpdateContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Update API v1");
        c.RoutePrefix = string.Empty; // Para acceder a Swagger desde la raíz
    });
}
else
{
    app.UseExceptionHandler("/Error");
  //  app.UseHsts();
}

// Configuración del middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // Autenticación
app.UseAuthorization(); // Autorización

app.MapRazorPages();
app.MapControllers();

app.Run();
