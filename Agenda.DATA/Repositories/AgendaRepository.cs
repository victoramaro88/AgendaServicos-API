using Agenda.DATA.Models;
using JWT.Controllers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace Agenda.DATA.Repositories
{
    public class AgendaRepository
    {
        private string _ConnAgenda = "";
        private string _bdAgenda = "";

        private IConfiguration Configuration;

        #region CONSTRUTOR
        public AgendaRepository(IConfiguration Configuration)
        {
            this.Configuration = Configuration;
            _ConnAgenda = Configuration.GetValue<string>("Conn_AgendaServicos");
            _bdAgenda = Configuration.GetValue<string>("AgendaServicos");
        }
        #endregion

        public string Teste(string teste)
        {
            return teste += "...";
        }

        public UsuarioModel Login(LoginModel objLogin)
        {
            UsuarioModel objUsuarioRetorno = new UsuarioModel();
            try
            {
                using (SqlConnection connection = new SqlConnection(_ConnAgenda))
                {
                    connection.Open();
                    SqlCommand command = connection.CreateCommand();
                    SqlTransaction transaction;
                    transaction = connection.BeginTransaction("Transaction");
                    command.Connection = connection;
                    command.Transaction = transaction;

                    try
                    {
                        DateTime dataHoraTransacao = DateTime.Now;

                        //-> Validando Login
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@usuLogin", objLogin.usuLogin);
                        command.Parameters.AddWithValue("@usuSenha", objLogin.usuSenha);
                        command.CommandText = @"
                                                SELECT usuCod, usuNome, usuLogin, usuSenha, usuStatus, perfCod
                                                FROM " + _bdAgenda + @".dbo.Usuario
                                                WHERE usuLogin = @usuLogin 
                                                AND usuSenha = @usuSenha;
                                            ";
                        DataTable dt = new DataTable();
                        SqlDataReader reader;
                        reader = command.ExecuteReader();
                        dt.Load(reader);
                        if (dt.Rows.Count > 0)
                        {
                            //-> Se existir, faz insere na tabela de Log.
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@logDesc", "Login de " + objLogin.usuLogin);
                            command.Parameters.AddWithValue("@logDataHora", dataHoraTransacao);
                            command.Parameters.AddWithValue("@usuCod", dt.Rows[0].ItemArray[0]);
                            command.Parameters.AddWithValue("@tipLogCod", 1); //-> Tipo de Log de Acesso.
                            command.CommandText = @"
                                                    INSERT INTO " + _bdAgenda + @".dbo.LogSis
                                                    (logDesc, logDataHora, usuCod, tipLogCod)
                                                    VALUES(
                                                    @logDesc, 
                                                    @logDataHora, 
                                                    @usuCod, 
                                                    @tipLogCod);
                                                ";
                            command.ExecuteNonQuery();
                        }
                        else
                        {
                            objUsuarioRetorno.MensagemErro = "Usuário ou senha inválidos.";
                            return objUsuarioRetorno;
                        }

                        objUsuarioRetorno.usuCod = int.Parse(dt.Rows[0].ItemArray[0].ToString());
                        objUsuarioRetorno.usuNome = dt.Rows[0].ItemArray[1].ToString();
                        objUsuarioRetorno.usuLogin = dt.Rows[0].ItemArray[2].ToString();
                        objUsuarioRetorno.usuSenha = "";
                        objUsuarioRetorno.usuStatus = dt.Rows[0].ItemArray[4].ToString() == "True" ? true : false;
                        objUsuarioRetorno.perfCod = int.Parse(dt.Rows[0].ItemArray[5].ToString());
                        objUsuarioRetorno.MensagemErro = "OK";

                        //-> Se tudo der certo até aqui, gera um token de acesso.
                        AutenticacaoController jwtAutenticacao = new AutenticacaoController();
                        var Token = jwtAutenticacao.GerarToken(JsonConvert.SerializeObject(objUsuarioRetorno));
                        objUsuarioRetorno.tokenAcesso = Token.ToString().Substring(10);
                        objUsuarioRetorno.tokenAcesso = objUsuarioRetorno.tokenAcesso.Substring(0, objUsuarioRetorno.tokenAcesso.Length - 2);

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        objUsuarioRetorno.MensagemErro = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                objUsuarioRetorno.MensagemErro = ex.Message;
            }

            return objUsuarioRetorno;
        }
    }
}
