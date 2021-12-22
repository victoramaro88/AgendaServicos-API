using Agenda.DATA.Models;
using JWT.Controllers;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
using System.Xml;

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

        public string InsereLog(object valores, int usuCod, int tipLogCod, DateTime dataHoraTransacao)
        {
            string ret = "";
            try
            {
                StringWriter sw = new StringWriter();
                XmlTextWriter xw = new XmlTextWriter(sw);
                XmlDocument xmlDocument = JsonConvert.DeserializeXmlNode(JsonConvert.SerializeObject(valores), "Valores");
                xmlDocument.WriteTo(xw);
                string objXML = sw.ToString() != "<Valores />" ? sw.ToString() : null;

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
                        //DateTime dataHoraTransacao = DateTime.Now;
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@logDesc", objXML);
                        command.Parameters.AddWithValue("@logDataHora", dataHoraTransacao);
                        command.Parameters.AddWithValue("@usuCod", usuCod);
                        command.Parameters.AddWithValue("@tipLogCod", tipLogCod);

                        command.CommandText = @"
                                                    INSERT INTO " + _bdAgenda + @".dbo.LogSis
                                                    (logDesc, logDataHora, usuCod, tipLogCod)
                                                    VALUES(
                                                    @logDesc,
                                                    @logDataHora,
                                                    @usuCod,
                                                    @tipLogCod
                                                    );
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }

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
                            object obj = JObject.Parse("{\n    \"Login\":\"" + objLogin.usuLogin + "\"\n}");
                            InsereLog(obj, int.Parse(dt.Rows[0].ItemArray[0].ToString()), 1, dataHoraTransacao); //-> Tipo de Log de Acesso = 1.
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

        public string ManterMaquina(MaquinaModel objMaquina)
        {
            string ret = "";
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
                        command.Parameters.Clear();
                        command.Parameters.AddWithValue("@maqCod", objMaquina.maqCod);
                        command.Parameters.AddWithValue("@maqMarca", objMaquina.maqMarca);
                        command.Parameters.AddWithValue("@maqModelo", objMaquina.maqModelo);
                        command.Parameters.AddWithValue("@maqObse", objMaquina.maqObse);
                        command.Parameters.AddWithValue("@maqStatus", objMaquina.maqStatus);
                        command.Parameters.AddWithValue("@diamCod", objMaquina.diamCod);
                        command.Parameters.AddWithValue("@veicCod", objMaquina.veicCod);

                        //-> Se tiver id, faz Update:
                        if (objMaquina.maqCod > 0)
                        {
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.Maquina
                                                        SET 
                                                        maqMarca=@maqMarca, 
                                                        maqModelo=@maqModelo, 
                                                        maqObse=@maqObse, 
                                                        maqStatus=@maqStatus, 
                                                        diamCod=@diamCod, 
                                                        veicCod=@veicCod
                                                        WHERE maqCod=@maqCod;
                                                    ";
                            command.ExecuteNonQuery();
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.Maquina
                                                        (maqMarca, maqModelo, maqObse, maqStatus, diamCod, veicCod)
                                                        VALUES(
                                                        @maqMarca, 
                                                        @maqModelo, 
                                                        @maqObse, 
                                                        @maqStatus, 
                                                        @diamCod, 
                                                        @veicCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        connection.Close();
                        ret = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                ret = ex.Message;
            }

            return ret;
        }
    }
}
