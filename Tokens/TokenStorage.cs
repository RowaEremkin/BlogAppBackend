using BlogAppBackend.DebugConsole;
using Microsoft.AspNetCore.RequestDecompression;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BlogAppBackend.Tokens
{
    public class TokenStorage : ITokenStorage
    {
        private string tempUserKey = "0";
        private readonly IDebugConsole _debugConsole;
        private readonly JwtSecurityTokenHandler _tokenHandler;
        private readonly TokenValidationParameters _tokenValidationParameters;
        public TokenStorage(IDebugConsole debugConsole, TokenValidationParameters tokenValidationParameters)
        {
            _debugConsole = debugConsole;
            _tokenHandler = new JwtSecurityTokenHandler();
            _tokenValidationParameters = tokenValidationParameters;
        }
        public bool TestToken(IHeaderDictionary header, string deviceId)
        {
            if (!header.ContainsKey("authorization")) return false;
            string token = header["authorization"].ToString();
            token = token.Replace("Bearer ", "");
            token = token.Replace("\"", "");
            _debugConsole.Log($"TestToken token: {token} on deviceId: {deviceId}");
            try
            {
                SecurityToken validatedToken;
                _tokenHandler.ValidateToken(token, _tokenValidationParameters, out validatedToken);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        public string GenerateToken(string deviceId)
        {
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                new Claim(ClaimTypes.Name, deviceId)
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                SigningCredentials = new SigningCredentials(_tokenValidationParameters.IssuerSigningKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = _tokenHandler.CreateToken(tokenDescriptor);
            string tokenStr = _tokenHandler.WriteToken(token);
            _debugConsole.Log($"GenerateToken token: {tokenStr} send to deviceId: {deviceId}");
            return tokenStr;
        }
    }
}
