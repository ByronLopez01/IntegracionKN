using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace APISenad.security
{

    public class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IConfiguration _configuration;

        public BasicAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IConfiguration configuration)
            : base(options, logger, encoder, clock)
        {
            _configuration = configuration;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("Authorization"))
            {
                Logger.LogWarning("Missing Authorization Header");
                return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));
            }

            try
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
                var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
                var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);

                if (credentials.Length != 2)
                {
                    Logger.LogWarning("Invalid Authorization Header");
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization Header"));
                }

                var username = credentials[0];
                var password = credentials[1];

                // Verificar credenciales
                var validUsername = _configuration["BasicAuth:Username"];
                var validPassword = _configuration["BasicAuth:Password"];

                if (username != validUsername || password != validPassword)
                {
                    Logger.LogWarning($"Invalid Username or Password: {username}");
                    return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));
                }

                var claims = new[] { new Claim(ClaimTypes.Name, username) };
                var identity = new ClaimsIdentity(claims, Scheme.Name);
                var principal = new ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, Scheme.Name);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error processing authentication");
                return Task.FromResult(AuthenticateResult.Fail("Error processing authentication"));
            }
        }

    }
}