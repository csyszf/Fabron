
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FabronService.FunctionalTests
{
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            Claim[]? claims = new[] { new Claim(ClaimTypes.Name, "Test user") };
            ClaimsIdentity? identity = new(claims, "Test");
            ClaimsPrincipal? principal = new(identity);
            AuthenticationTicket? ticket = new(principal, "Test");

            AuthenticateResult? result = AuthenticateResult.Success(ticket);

            return Task.FromResult(result);
        }
    }

}
