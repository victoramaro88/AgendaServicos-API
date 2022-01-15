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

        private object ParametrosToken(int parametroArray)
        {
            List<string> parametros = new List<string>();
            var accessToken = Request.Headers[HeaderNames.Authorization];
            AutenticacaoController jwtAutenticacao = new AutenticacaoController();
            var ambienteToken = (List<string>)jwtAutenticacao.ValidaTokenRequisicao(accessToken.ToString().Replace("Bearer ", ""));

            if (parametroArray == 999)
            {
                return ambienteToken;
            }
            else
            {
                return ambienteToken[parametroArray];
            }
        }

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
            if (objLogin != null && (objLogin.usuLogin.Length > 0 && objLogin.usuSenha.Length > 0))
            {
                objLogin.usuSenha = _crypto.GerarHashString(objLogin.usuSenha);
                var ret = _agendaRepo.Login(objLogin);

                if (ret.MensagemErro == "OK")
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

        #region MANTER INFORMAÇÕES
        [HttpPost]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ManterVeiculo([FromBody] VeiculoModel objVeiculo)
        {
            if (objVeiculo != null)
            {
                var param = ParametrosToken(0); //-> 0: usuCod.
                int usuCod = int.Parse(param.ToString().Split(":")[1]);

                var ret = _agendaRepo.ManterVeiculo(objVeiculo, usuCod);

                if (ret == "OK")
                {
                    return Ok(ret);
                }
                else
                {
                    return BadRequest("Erro: " + ret);
                }
            }
            else
            {
                return BadRequest("Parâmetros inválidos.");
            }
        }

        [HttpPost]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ManterMaquina([FromBody] MaquinaModel objMaquina)
        {
            if (objMaquina != null)
            {
                var param = ParametrosToken(0); //-> 0: usuCod.
                int usuCod = int.Parse(param.ToString().Split(":")[1]);

                var ret = _agendaRepo.ManterMaquina(objMaquina, usuCod);

                if (ret == "OK")
                {
                    return Ok(ret);
                }
                else
                {
                    return BadRequest("Erro: " + ret);
                }
            }
            else
            {
                return BadRequest("Parâmetros inválidos.");
            }
        }

        [Route("{veicCod}/{veicStatus}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult AlteraStatusVeiculo(int veicCod, bool veicStatus)
        {
            if (veicCod > 0)
            {
                var param = ParametrosToken(0); //-> 0: usuCod.
                int usuCod = int.Parse(param.ToString().Split(":")[1]);

                var ret = _agendaRepo.AlteraStatusVeiculo(veicCod, veicStatus, usuCod);

                if (ret == "OK")
                {
                    return Ok(ret);
                }
                else
                {
                    return BadRequest("Erro: " + ret);
                }
            }
            else
            {
                return BadRequest("Parâmetros inválidos.");
            }
        }

        [Route("{maqCod}/{maqStatus}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult AlteraStatusMaquina(int maqCod, bool maqStatus)
        {
            if (maqCod > 0)
            {
                var param = ParametrosToken(0); //-> 0: usuCod.
                int usuCod = int.Parse(param.ToString().Split(":")[1]);

                var ret = _agendaRepo.AlteraStatusMaquina(maqCod, maqStatus, usuCod);

                if (ret == "OK")
                {
                    return Ok(ret);
                }
                else
                {
                    return BadRequest("Erro: " + ret);
                }
            }
            else
            {
                return BadRequest("Parâmetros inválidos.");
            }
        }

        [Route("{equipCod}/{equipStatus}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult AlteraStatusEquipe(int equipCod, bool equipStatus)
        {
            if (equipCod > 0)
            {
                var param = ParametrosToken(0); //-> 0: usuCod.
                int usuCod = int.Parse(param.ToString().Split(":")[1]);

                var ret = _agendaRepo.AlteraStatusEquipe(equipCod, equipStatus, usuCod);

                if (ret == "OK")
                {
                    return Ok(ret);
                }
                else
                {
                    return BadRequest("Erro: " + ret);
                }
            }
            else
            {
                return BadRequest("Parâmetros inválidos.");
            }
        }
        #endregion

        //--------------------------------------------------------------------------------------------------------------------------------

        #region PESQUISAS
        [Route("{diamCod?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ListaDiametroFuro(int diamCod = 0)
        {
            try
            {
                var resp = _agendaRepo.ListaDiametroFuro(diamCod);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("{veicCod?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ListaVeiculo(int veicCod = 0)
        {
            try
            {
                var resp = _agendaRepo.ListaVeiculo(veicCod);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("{tipVeicCod?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ListaTipoVeiculo(int tipVeicCod = 0)
        {
            try
            {
                var resp = _agendaRepo.ListaTipoVeiculo(tipVeicCod);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("{maqCod?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ListaMaquina(int maqCod = 0)
        {
            try
            {
                var resp = _agendaRepo.ListaMaquina(maqCod);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("{apNavCod?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ListaAparelhoNavegacao(int apNavCod = 0)
        {
            try
            {
                var resp = _agendaRepo.ListaAparelhoNavegacao(apNavCod);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("{equipCod?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ListaEquipe(int equipCod = 0)
        {
            try
            {
                var resp = _agendaRepo.ListaEquipe(equipCod);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("{usuCod?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ListaUsuario(int usuCod = 0)
        {
            try
            {
                var resp = _agendaRepo.ListaUsuario(usuCod);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [Route("{equipCod?}")]
        [HttpGet]
        [Authorize("Bearer")]
        [Produces("application/json")]
        public IActionResult ListaEquipeUsuario(int equipCod)
        {
            try
            {
                var resp = _agendaRepo.ListaEquipeUsuario(equipCod);
                return Ok(resp);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        #endregion
    }
}
