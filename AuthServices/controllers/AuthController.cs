using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AuthServices.controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _key;
        private readonly string _issuer;

        public AuthController(IConfiguration configuration)
        {
            _key = configuration["Jwt:Key"];
            _issuer = configuration["Jwt:Issuer"]; // Obtener el issuer desde la configuración
        }

        [HttpPost("token")]
        public IActionResult GetToken()
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_key);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, "User") 
                }),
                Expires = DateTime.UtcNow.AddYears(1), //expiracion del token
                Issuer = _issuer, 
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            return Ok(new { Token = tokenString });
        }

        [HttpPost("validate-basic")]
        public IActionResult ValidateBasicAuth([FromHeader] string authorization)
        {
            if (string.IsNullOrEmpty(authorization) || !authorization.StartsWith("Basic "))
                return Unauthorized("Missing or invalid Authorization header");

            // Decodificar credenciales básicas (Formato: 'Basic base64(username:password)')
            var encodedUsernamePassword = authorization.Substring("Basic ".Length).Trim();
            var decodedUsernamePassword = Encoding.UTF8.GetString(Convert.FromBase64String(encodedUsernamePassword)).Split(':');
            var username = decodedUsernamePassword[0];
            var password = decodedUsernamePassword[1];

            // Lógica de autenticación - reemplaza esto con tu lógica de validación de usuario
            if (username == "UsuarioCorrecto" && password == "ContraseñaCorrecta")
            {
                return Ok(); // Respuesta OK si las credenciales son válidas
            }

            return Unauthorized("Invalid credentials");
        }
    }
}
