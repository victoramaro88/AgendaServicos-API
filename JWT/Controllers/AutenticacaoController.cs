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

                PessoaViewModel pessoaViewModel = JsonConvert.DeserializeObject<PessoaViewModel>(json, js);

                if (pessoaViewModel.pesIdf > 0)
                {
                    List<Claim> claims = new List<Claim>();
                    claims.Add(new Claim("pesIdf", pessoaViewModel.pesIdf.ToString()));
                    claims.Add(new Claim("pesNom", pessoaViewModel.pesNom.ToString()));
                    claims.Add(new Claim("pesSclNom", pessoaViewModel.pesSclNom.ToString()));
                    claims.Add(new Claim("pesTipCod", pessoaViewModel.pesTipCod.ToString()));
                    claims.Add(new Claim("pesNasDat", pessoaViewModel.pesNasDat.ToString()));
                    claims.Add(new Claim("efeMilCbReNum", pessoaViewModel.efeMilCbReNum.ToString()));
                    claims.Add(new Claim("efeMilCbDigReNum", pessoaViewModel.efeMilCbDigReNum.ToString()));
                    claims.Add(new Claim("efeMilCbGueNom", pessoaViewModel.efeMilCbGueNom.ToString()));
                    claims.Add(new Claim("posSgl", pessoaViewModel.posSgl.ToString()));
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

        public string GerarTokenExterno(EscopoViewModel escopoViewModel)
        {
            try
            {
                if (escopoViewModel.nomeEscopo.Length > 0 && escopoViewModel.chaveEscopo.Length > 0)
                {
                    string chaveEscopo = ConfigurationManager.AppSetting["Escopos:" + escopoViewModel.nomeEscopo];
                    if (chaveEscopo != null)
                    {
                        if (escopoViewModel.chaveEscopo == chaveEscopo)
                        {
                            List<Claim> claims = new List<Claim>();
                            claims.Add(new Claim("nomeEscopo", escopoViewModel.nomeEscopo.ToString()));
                            claims.Add(new Claim("chaveEscopo", escopoViewModel.chaveEscopo.ToString()));
                            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));


                            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(ConfigurationManager.AppSetting["JWT:Key"]));
                            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);


                            // Tempo para expiração do Token.
                            var expiration = DateTime.Now.AddMinutes(30);

                            JwtSecurityToken token = new JwtSecurityToken(
                               issuer: null,
                               audience: null,
                               claims: claims,
                               expires: expiration,
                               signingCredentials: creds);

                            return new JwtSecurityTokenHandler().WriteToken(token);
                        }
                        else
                        {
                            return "Erro: Chave de autenticação inválida.";
                        }
                    }
                    else
                    {
                        return "Erro: Escopo inválido.";
                    }
                }
                else
                {
                    return "Erro: Falha ao autenticar.";
                }

            }
            catch (Exception e)
            {
                return e.Message;
            }

        }

        public object VerificaToken(string token)
        {
            var stream = token;
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(stream);
            var tokenS = handler.ReadToken(stream) as JwtSecurityToken;

            //var jti = tokenS.Claims.First(claim => claim.Type == "pesNom").Value;

            PessoaViewModel pessoaViewModel = new PessoaViewModel();
            pessoaViewModel.pesIdf = long.Parse(tokenS.Claims.First(claim => claim.Type == "pesIdf").Value);
            pessoaViewModel.pesNom = tokenS.Claims.First(claim => claim.Type == "pesNom").Value;
            pessoaViewModel.pesSclNom = tokenS.Claims.First(claim => claim.Type == "pesSclNom").Value;
            pessoaViewModel.pesTipCod = short.Parse(tokenS.Claims.First(claim => claim.Type == "pesTipCod").Value);
            pessoaViewModel.pesNasDat = DateTime.Parse(tokenS.Claims.First(claim => claim.Type == "pesNasDat").Value);
            pessoaViewModel.efeMilCbReNum = long.Parse(tokenS.Claims.First(claim => claim.Type == "efeMilCbReNum").Value);
            pessoaViewModel.efeMilCbDigReNum = tokenS.Claims.First(claim => claim.Type == "efeMilCbDigReNum").Value;
            pessoaViewModel.efeMilCbGueNom = tokenS.Claims.First(claim => claim.Type == "efeMilCbGueNom").Value;
            pessoaViewModel.posSgl = tokenS.Claims.First(claim => claim.Type == "posSgl").Value;

            var novoToken = GerarToken(JsonConvert.SerializeObject(pessoaViewModel));

            return novoToken;
        }

        public object VerificaTokenExterno(string token)
        {
            var stream = token;
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(stream);
            var tokenS = handler.ReadToken(stream) as JwtSecurityToken;
            
            EscopoViewModel escopoViewModel = new EscopoViewModel();
            escopoViewModel.nomeEscopo = tokenS.Claims.First(claim => claim.Type == "nomeEscopo").Value;
            escopoViewModel.chaveEscopo = tokenS.Claims.First(claim => claim.Type == "chaveEscopo").Value;

            var novoToken = GerarTokenExterno(escopoViewModel);

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

                validacao.Add(tokenS.Claims.First().Type);
                validacao.Add(tokenS.Claims.First().Value);
            }
            catch (Exception)
            {
                validacao.Add("Erro");
            }
            
            return validacao;
        }
    }
}
