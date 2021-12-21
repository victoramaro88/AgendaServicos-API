using JWT.Model;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Globalization;
using System.Linq;
using System.Dynamic;

namespace JWT.Controllers
{
    public class AutenticacaoController
    {
        public object GerarToken(string json)
        {
            try
            {
                JsonSerializerSettings js = new JsonSerializerSettings();
                js.Culture = CultureInfo.InvariantCulture;

                UsuarioModel objUsuario = JsonConvert.DeserializeObject<UsuarioModel>(json, js);

                if (objUsuario.usuCod > 0)
                {
                    List<Claim> claims = new List<Claim>();
                    claims.Add(new Claim("usuCod", objUsuario.usuCod.ToString()));
                    claims.Add(new Claim("usuNome", objUsuario.usuNome.ToString()));
                    claims.Add(new Claim("usuLogin", objUsuario.usuLogin.ToString()));
                    claims.Add(new Claim("usuStatus", objUsuario.usuStatus.ToString()));
                    claims.Add(new Claim("perfCod", objUsuario.perfCod.ToString()));
                    claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));

                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationManager.AppSetting["JWT:key"]));
                    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


                    // Tempo para expiração do Token.
                    var expiration = DateTime.Now.AddMinutes(30);
                    //var expiration = DateTime.UtcNow.AddMinutes(1);

                    JwtSecurityToken token = new JwtSecurityToken(
                       issuer: null,
                       audience: null,
                       claims: claims,
                       expires: expiration,
                       signingCredentials: creds);



                    return new
                    {
                        Token = new JwtSecurityTokenHandler().WriteToken(token),
                        //Expiration = expiration
                    };
                }
                else
                {
                    return new
                    {
                        Erro = "Falha ao autenticar."
                    };
                }

            }
            catch (Exception e)
            {
                return new
                {
                    Erro = e
                };
            }

        }

        public object VerificaToken(string token)
        {
            var stream = token;
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(stream);
            var tokenS = handler.ReadToken(stream) as JwtSecurityToken;

            //var jti = tokenS.Claims.First(claim => claim.Type == "pesNom").Value;

            UsuarioModel objUsuario = new UsuarioModel();
            objUsuario.usuCod = int.Parse(tokenS.Claims.First(claim => claim.Type == "usuCod").Value);
            objUsuario.usuNome = tokenS.Claims.First(claim => claim.Type == "usuNome").Value;
            objUsuario.usuLogin = tokenS.Claims.First(claim => claim.Type == "usuLogin").Value;
            objUsuario.usuStatus = bool.Parse(tokenS.Claims.First(claim => claim.Type == "usuStatus").Value);
            objUsuario.perfCod = int.Parse(tokenS.Claims.First(claim => claim.Type == "perfCod").Value);

            var novoToken = GerarToken(JsonConvert.SerializeObject(objUsuario));

            return novoToken;
        }

        public object ValidaTokenRequisicao(string token)
        {
            List<string> validacao = new List<string>();
            try
            {
                var stream = token;
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(stream);
                var tokenS = handler.ReadToken(stream) as JwtSecurityToken;

                foreach (var item in tokenS.Claims)
                {
                    validacao.Add(item.Type + ":" + item.Value);
                }
            }
            catch (Exception)
            {
                validacao.Add("Erro");
            }

            return validacao;
        }
    }
}
