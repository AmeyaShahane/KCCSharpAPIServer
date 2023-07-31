using System;
using System.IO;
using System.Net.Http;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Cryptography.X509Certificates;

namespace CSharpService
{
    public class Program
    {

private const string KeycloakPublicKey = @"
-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEApuKSnkxY4HMW3wNEFhrDLgo5hUa5kNs9MSFl48nbZ4brSmg4J/WzM9rtBgyMvmQN82VC2qu7A0/inUNEPD22vb7U6V8U1KVnCFkPbXCIqjZZsYesfeb5AfiHDuC2dsIdZzLku6ZRL4DkGm55wN68khIEJEIttges+XDubs9vh18UgIPRdXpza0f/BKBlmapYr2trCYCIQKSFXv7AvBW9nLMf2VchPZhqg6P8ePDW+f6aP/xzGvFk/dYw0G3bh9J3orFtNJDFrWSfhSIY0xco3aNDbP+gqtnz272/HRKRSz9Mc3tKXoUN7H+6AdvjNRX+EYc4jYcYJmIRGv9xNsRuAQIDAQAB
-----END PUBLIC KEY-----
";

        private static readonly JwtSecurityTokenHandler _tokenHandler = new JwtSecurityTokenHandler();

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddCors(); // Add this line to enable CORS
            builder.Services.AddRouting();
            builder.Services.AddAuthentication("Bearer") // Add this line to enable authentication
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = "http://localhost:8080/realms/ashahane"; // Update with your Keycloak realm URL
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        IssuerSigningKey = GetIssuerSigningKey(),
                    };
                });

            builder.Services.AddAuthorization(); // Add this line to enable authorization

            var app = builder.Build();

            app.UseCors(builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()); // Add this line to use CORS
            app.UseRouting();
            app.UseAuthentication(); // Add this line to enable authentication
            app.UseAuthorization(); // Add this line to enable authorization

            app.Map("", HomeHandler);
            app.Map("/api/resource", GetResource).RequireAuthorization();

            int port = 8000;
            Console.WriteLine($"Starting server on port {port}...");

            try
            {
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An unhandled exception occurred during application startup:");
                Console.WriteLine(ex.ToString());
                throw;
            }
        }

        private static RsaSecurityKey GetIssuerSigningKey()
        {
            var publicKey = PemEncodingHelper.DecodePemPublicKey(KeycloakPublicKey);
            return new RsaSecurityKey(publicKey);
        }

        private static async Task HomeHandler(HttpContext context)
        {
            Console.WriteLine("Received request for the home page");
            await context.Response.WriteAsync("Hello, this is the home page!");
        }

        private static async Task GetResource(HttpContext context)
        {
            Console.WriteLine("Received request for protected resource");
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"data\": \"This is a protected resource\"}");
        }
    }

    public static class PemEncodingHelper
    {
        public static RSA DecodePemPublicKey(string pem)
        {
            var base64 = pem
                .Replace("-----BEGIN PUBLIC KEY-----", "")
                .Replace("-----END PUBLIC KEY-----", "")
                .Replace("\n", "");
            var keyBytes = Convert.FromBase64String(base64);

            var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(keyBytes, out _);

            return rsa;
        }
    }
}

