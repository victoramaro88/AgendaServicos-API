using Agenda.DATA.Models;
using Agenda.DATA.Repositories;
using JWT.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiAgendaServicos.Controllers
{
    [Route("api/[controller]/[action]")]
    public class AgendaController : Controller
    {
        private readonly AgendaRepository _agendaRepo;
        private readonly CryptoController _crypto;

        #region CONSTRUTOR
        IConfiguration Configuration;
        private IHostingEnvironment _hostingEnvironment;
        public AgendaController(IConfiguration iConfig, IHostingEnvironment hostingEnvironment)
        {
            Configuration = iConfig;
            _hostingEnvironment = hostingEnvironment;

            _crypto = new CryptoController();
            _agendaRepo = new AgendaRepository(Configuration);
        }
        #endregion

        [Route("{teste?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult Teste(string teste = "-")
        {
            var accessToken = Request.Headers[HeaderNames.Authorization];
            AutenticacaoController jwtAutenticacao = new AutenticacaoController();
            var ambienteToken = (List<string>)jwtAutenticacao.ValidaTokenRequisicao(accessToken.ToString().Replace("Bearer ", ""));

            //var ret = _appMatResgRepo.Teste(teste);
            var ret = _crypto.GerarHashString(teste);
            //var ret = _crypto.GerarSenha(6);
            return Ok(ret);
        }

        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult RenovaToken()
        {
            var accessToken = Request.Headers[HeaderNames.Authorization];
            AutenticacaoController jwtAutenticacao = new AutenticacaoController();
            var novoToken = jwtAutenticacao.VerificaToken(accessToken.ToString().Replace("Bearer ", ""));

            return Ok(novoToken);
        }

        [HttpPost]
        [Produces("application/json")]
        public IActionResult Login([FromBody] LoginModel objLogin)
        {
            if(objLogin != null && (objLogin.usuLogin.Length > 0 && objLogin.usuSenha.Length > 0))
            {
                objLogin.usuSenha = _crypto.GerarHashString(objLogin.usuSenha);
                var ret = _agendaRepo.Login(objLogin);

                if(ret.MensagemErro == "OK")
                {
                    return Ok(ret);
                }
                else
                {
                    return BadRequest("Erro: " + ret.MensagemErro);
                }
            }
            else
            {
                return BadRequest("Parâmetros inválidos.");
            }            
        }
    }
}
