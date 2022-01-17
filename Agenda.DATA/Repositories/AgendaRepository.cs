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
                                                FROM " + _bdAgenda + @".dbo.Usuario WITH(NOLOCK)
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

        #region MANTER INFORMAÇÕES
        public string ManterVeiculo(VeiculoModel objVeiculo, int usuCod)
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
                        command.Parameters.AddWithValue("@veicCod", objVeiculo.veicCod);
                        command.Parameters.AddWithValue("@veicMarca", objVeiculo.veicMarca);
                        command.Parameters.AddWithValue("@veicModelo", objVeiculo.veicModelo);
                        command.Parameters.AddWithValue("@veicAno", objVeiculo.veicAno);
                        command.Parameters.AddWithValue("@veicPlaca", objVeiculo.veicPlaca.ToUpper());
                        command.Parameters.AddWithValue("@veicObse", objVeiculo.veicObse);
                        command.Parameters.AddWithValue("@veicStatus", objVeiculo.veicStatus);
                        command.Parameters.AddWithValue("@tipVeicCod", objVeiculo.tipVeicCod);

                        //-> Se tiver id, faz Update:
                        if (objVeiculo.veicCod > 0)
                        {
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.Veiculo
                                                        SET 
                                                            veicMarca=@veicMarca, 
                                                            veicModelo=@veicModelo, 
                                                            veicAno=@veicAno, 
                                                            veicPlaca=@veicPlaca, 
                                                            veicObse=@veicObse, 
                                                            veicStatus=@veicStatus, 
                                                            tipVeicCod=@tipVeicCod
                                                        WHERE veicCod=@veicCod;
                                                    ";
                            command.ExecuteNonQuery();
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.Veiculo
                                                        (veicMarca, veicModelo, veicAno, veicPlaca, veicObse, veicStatus, tipVeicCod)
                                                        VALUES(
                                                            @veicMarca,
                                                            @veicModelo,
                                                            @veicAno,
                                                            @veicPlaca,
                                                            @veicObse,
                                                            @veicStatus,
                                                            @tipVeicCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objVeiculo, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
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

        public string AlteraStatusVeiculo(int veicCod, bool veicStatus, int usuCod)
        {
            veicStatus = !veicStatus;
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
                        command.Parameters.AddWithValue("@veicCod", veicCod);
                        command.Parameters.AddWithValue("@veicStatus", veicStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.Veiculo
                                                    SET 
                                                        veicStatus=@veicStatus
                                                    WHERE veicCod=@veicCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"veicCod\":\"" + veicCod.ToString() + "\",\n    \"veicStatus\": \"" + veicStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCod, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
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

        public string AlteraStatusMaquina(int maqCod, bool maqStatus, int usuCod)
        {
            maqStatus = !maqStatus;
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
                        command.Parameters.AddWithValue("@maqCod", maqCod);
                        command.Parameters.AddWithValue("@maqStatus", maqStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.Maquina
                                                    SET 
                                                        maqStatus=@maqStatus
                                                    WHERE maqCod=@maqCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"maqCod\":\"" + maqCod.ToString() + "\",\n    \"maqStatus\": \"" + maqStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCod, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
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

        public string AlteraStatusEquipe(int equipCod, bool equipStatus, int usuCod)
        {
            equipStatus = !equipStatus;
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
                        command.Parameters.AddWithValue("@equipCod", equipCod);
                        command.Parameters.AddWithValue("@equipStatus", equipStatus);

                        command.CommandText = @"
                                                    UPDATE " + _bdAgenda + @".dbo.Equipe
                                                    SET 
                                                        equipStatus=@equipStatus
                                                    WHERE equipCod=@equipCod;
                                                ";
                        command.ExecuteNonQuery();

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        object obj = JObject.Parse("{\n    \"equipCod\":\"" + equipCod.ToString() + "\",\n    \"equipStatus\": \"" + equipStatus.ToString() + "\"\n}");
                        InsereLog(obj, usuCod, 2, dataHoraTransacao); //-> Tipo de Log de Transação = 2.
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

        public string ManterMaquina(MaquinaModel objMaquina, int usuCod)
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

                        InsereLog(objMaquina, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
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

        public string ManterEquipe(UsuarioEnvioModel objEquipe, int usuCod)
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
                        command.Parameters.AddWithValue("@equipCod", objEquipe.objEnvioEquipe.equipCod);
                        command.Parameters.AddWithValue("@equipDesc", objEquipe.objEnvioEquipe.equipDesc);
                        command.Parameters.AddWithValue("@maqCod", objEquipe.objEnvioEquipe.maqCod);
                        command.Parameters.AddWithValue("@apNavCod", objEquipe.objEnvioEquipe.apNavCod);
                        command.Parameters.AddWithValue("@equipStatus", objEquipe.objEnvioEquipe.equipStatus);

                        //-> Se tiver id, faz Update:
                        if (objEquipe.objEnvioEquipe.equipCod > 0)
                        {
                            //-> Altera primeiro a equipe (Equipe)
                            command.CommandText = @"
                                                        UPDATE " + _bdAgenda + @".dbo.Equipe
                                                        SET equipDesc=@equipDesc, 
                                                        equipStatus=@equipStatus, 
                                                        apNavCod=@apNavCod, 
                                                        maqCod=@maqCod
                                                        WHERE equipCod=@equipCod;
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Apaga os integrantes que estavam relacionados (UsuarioEquipe)
                            command.CommandText = @"
                                                        DELETE FROM " + _bdAgenda + @".dbo.UsuarioEquipe
                                                        WHERE equipCod=@equipCod;
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Insere os novos usuários na tabela de relacionamento (UsuarioEquipe)
                            foreach (var itmUsr in objEquipe.objEnvioListaUsuario)
                            {
                                command.Parameters.Clear();
                                command.Parameters.AddWithValue("@equipCod", objEquipe.objEnvioEquipe.equipCod);
                                command.Parameters.AddWithValue("@usuCod", itmUsr.usuCod);
                                command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.UsuarioEquipe
                                                        (equipCod, usuCod)
                                                        VALUES(
                                                        @equipCod, 
                                                        @usuCod
                                                        );
                                                    ";
                                command.ExecuteNonQuery();
                            }
                        }
                        //-> Senão, insere.
                        else
                        {
                            command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.Equipe
                                                        (equipDesc, equipStatus, apNavCod, maqCod)
                                                        VALUES(
                                                        @equipDesc, 
                                                        @equipStatus, 
                                                        @apNavCod, 
                                                        @maqCod
                                                        );
                                                    ";
                            command.ExecuteNonQuery();

                            //-> Retornando o Id que foi inserido da equipe.
                            command.CommandText = @"
                                                        SELECT MAX(equipCod) FROM " + _bdAgenda + @".dbo.Equipe WITH(NOLOCK);
                                                    ";
                            int lastEquipCod = 0;
                            DataTable dt = new DataTable();
                            SqlDataReader reader;
                            reader = command.ExecuteReader();
                            dt.Load(reader);
                            if (dt.Rows.Count > 0)
                            {
                                lastEquipCod = int.Parse(dt.Rows[0].ItemArray[0].ToString());
                            }

                            if (lastEquipCod > 0)
                            {
                                //-> Insere os novos usuários na tabela de relacionamento (UsuarioEquipe)
                                foreach (var itmUsr in objEquipe.objEnvioListaUsuario)
                                {
                                    command.Parameters.Clear();
                                    command.Parameters.AddWithValue("@equipCod", lastEquipCod);
                                    command.Parameters.AddWithValue("@usuCod", itmUsr.usuCod);
                                    command.CommandText = @"
                                                        INSERT INTO " + _bdAgenda + @".dbo.UsuarioEquipe
                                                        (equipCod, usuCod)
                                                        VALUES(
                                                        @equipCod, 
                                                        @usuCod
                                                        );
                                                    ";
                                    command.ExecuteNonQuery();
                                }
                            }
                            else
                            {
                                ret = "falha ao realizar a operação.";
                                transaction.Rollback();
                                connection.Close();
                            }
                        }

                        //-> Finaliza a transação.
                        transaction.Commit();
                        ret = "OK";

                        InsereLog(objEquipe, usuCod, 2, dataHoraTransacao); //-> Tipo de Log 2: Transação.
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
        #endregion

        //--------------------------------------------------------------------------------------------------------------------------------

        #region PESQUISAS
        public List<DiametroFuroModel> ListaDiametroFuro(int diamCod)
        {
            List<DiametroFuroModel> listaRetorno = new List<DiametroFuroModel>();
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
                        command.Parameters.AddWithValue("@diamCod", diamCod);

                        command.CommandText = @"
                                                    SELECT diamCod, diamDesc, diamMax, diamMin, diamStatus
                                                    FROM " + _bdAgenda + @".dbo.DiametroFuro WITH(NOLOCK)
                                                ";

                        if (diamCod > 0)
                        {
                            command.CommandText += " WHERE diamCod = @diamCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            DiametroFuroModel objItem;
                            while (reader.Read())
                            {
                                objItem = new DiametroFuroModel();

                                objItem.diamCod = int.Parse(reader["diamCod"].ToString());
                                objItem.diamDesc = reader["diamDesc"].ToString();
                                objItem.diamMax = int.Parse(reader["diamMax"].ToString());
                                objItem.diamMin = int.Parse(reader["diamMin"].ToString());
                                objItem.diamStatus = bool.Parse(reader["diamStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<VeiculoModel> ListaVeiculo(int veicCod)
        {
            List<VeiculoModel> listaRetorno = new List<VeiculoModel>();
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
                        command.Parameters.AddWithValue("@veicCod", veicCod);

                        command.CommandText = @"
                                                    SELECT veicCod, veicMarca, veicModelo, veicAno, veicPlaca, veicObse, veicStatus, tipVeicCod
                                                    FROM " + _bdAgenda + @".dbo.Veiculo WITH(NOLOCK)
                                                ";

                        if (veicCod > 0)
                        {
                            command.CommandText += " WHERE veicCod = @veicCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            VeiculoModel objItem;
                            while (reader.Read())
                            {
                                objItem = new VeiculoModel();

                                objItem.veicCod = int.Parse(reader["veicCod"].ToString());
                                objItem.veicMarca = reader["veicMarca"].ToString();
                                objItem.veicModelo = reader["veicModelo"].ToString();
                                objItem.veicAno = int.Parse(reader["veicAno"].ToString());
                                objItem.veicPlaca = reader["veicPlaca"].ToString();
                                objItem.veicObse = reader["veicObse"].ToString();
                                objItem.veicStatus = bool.Parse(reader["veicStatus"].ToString());
                                objItem.tipVeicCod = int.Parse(reader["tipVeicCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<TipoVeiculoModel> ListaTipoVeiculo(int tipVeicCod)
        {
            List<TipoVeiculoModel> listaRetorno = new List<TipoVeiculoModel>();
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
                        command.Parameters.AddWithValue("@veicCod", tipVeicCod);

                        command.CommandText = @"
                                                    SELECT tipVeicCod, tipVeicDesc, tipVeicStatus
                                                    FROM " + _bdAgenda + @".dbo.TipoVeiculo WITH(NOLOCK);
                                                ";

                        if (tipVeicCod > 0)
                        {
                            command.CommandText += " WHERE tipVeicCod = @tipVeicCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            TipoVeiculoModel objItem;
                            while (reader.Read())
                            {
                                objItem = new TipoVeiculoModel();

                                objItem.tipVeicCod = int.Parse(reader["tipVeicCod"].ToString());
                                objItem.tipVeicDesc = reader["tipVeicDesc"].ToString();
                                objItem.tipVeicStatus = bool.Parse(reader["tipVeicStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<MaquinaModel> ListaMaquina(int maqCod)
        {
            List<MaquinaModel> listaRetorno = new List<MaquinaModel>();
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
                        command.Parameters.AddWithValue("@maqCod", maqCod);

                        command.CommandText = @"
                                                    SELECT maqCod, maqMarca, maqModelo, maqObse, maqStatus, diamCod, veicCod
                                                    FROM " + _bdAgenda + @".dbo.Maquina WITH(NOLOCK)
                                                ";

                        if (maqCod > 0)
                        {
                            command.CommandText += " WHERE maqCod = @maqCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            MaquinaModel objItem;
                            while (reader.Read())
                            {
                                objItem = new MaquinaModel();

                                objItem.maqCod = int.Parse(reader["maqCod"].ToString());
                                objItem.maqMarca = reader["maqMarca"].ToString();
                                objItem.maqModelo = reader["maqModelo"].ToString();
                                objItem.maqObse = reader["maqObse"].ToString();
                                objItem.maqStatus = bool.Parse(reader["maqStatus"].ToString());
                                objItem.diamCod = int.Parse(reader["diamCod"].ToString());
                                objItem.veicCod = int.Parse(reader["veicCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<AparelhoNavegacaoModel> ListaAparelhoNavegacao(int apNavCod)
        {
            List<AparelhoNavegacaoModel> listaRetorno = new List<AparelhoNavegacaoModel>();
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
                        command.Parameters.AddWithValue("@apNavCod", apNavCod);

                        command.CommandText = @"
                                                    SELECT apNavCod, apNavMarcMod, apNavObse, apNavStatus
                                                    FROM " + _bdAgenda + @".dbo.AparelhoNavegacao WITH(NOLOCK)
                                                ";

                        if (apNavCod > 0)
                        {
                            command.CommandText += " WHERE apNavCod = @apNavCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            AparelhoNavegacaoModel objItem;
                            while (reader.Read())
                            {
                                objItem = new AparelhoNavegacaoModel();

                                objItem.apNavCod = int.Parse(reader["apNavCod"].ToString()); ;
                                objItem.apNavMarcMod = reader["apNavMarcMod"].ToString(); ;
                                objItem.apNavObse = reader["apNavObse"].ToString(); ;
                                objItem.apNavStatus = bool.Parse(reader["apNavStatus"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<EquipeModel> ListaEquipe(int equipCod)
        {
            List<EquipeModel> listaRetorno = new List<EquipeModel>();
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
                        command.Parameters.AddWithValue("@equipCod", equipCod);

                        command.CommandText = @"
                                                    SELECT equipCod, equipDesc, equipStatus, apNavCod, maqCod
                                                    FROM " + _bdAgenda + @".dbo.Equipe WITH(NOLOCK)
                                                ";

                        if (equipCod > 0)
                        {
                            command.CommandText += " WHERE equipCod = @equipCod";
                        }

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            EquipeModel objItem;
                            while (reader.Read())
                            {
                                objItem = new EquipeModel();

                                objItem.equipCod = int.Parse(reader["equipCod"].ToString());
                                objItem.equipDesc = reader["equipDesc"].ToString();
                                objItem.equipStatus = bool.Parse(reader["equipStatus"].ToString());
                                objItem.apNavCod = int.Parse(reader["apNavCod"].ToString());
                                objItem.maqCod = int.Parse(reader["maqCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<UsuarioTbModel> ListaUsuario(int usuCod)
        {
            List<UsuarioTbModel> listaRetorno = new List<UsuarioTbModel>();
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
                        command.Parameters.AddWithValue("@usuCod", usuCod);

                        command.CommandText = @"
                                                    SELECT usuCod, usuNome, usuLogin, usuSenha, usuStatus, perfCod
                                                    FROM " + _bdAgenda + @".dbo.Usuario WITH(NOLOCK)
                                                ";

                        if (usuCod > 0)
                        {
                            command.CommandText += " WHERE usuCod = @usuCod";
                        }

                        command.CommandText += " ORDER BY usuNome";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            UsuarioTbModel objItem;
                            while (reader.Read())
                            {
                                objItem = new UsuarioTbModel();


                                objItem.usuCod = int.Parse(reader["usuCod"].ToString());
                                objItem.usuNome = reader["usuNome"].ToString();
                                objItem.usuLogin = reader["usuLogin"].ToString();
                                objItem.usuSenha = "";// reader["usuSenha"].ToString();
                                objItem.usuStatus = bool.Parse(reader["usuStatus"].ToString());
                                objItem.perfCod = int.Parse(reader["perfCod"].ToString());

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<UsuarioTbModel> ListaUsuariosDisponiveis()
        {
            List<UsuarioTbModel> listaRetorno = new List<UsuarioTbModel>();
            try
            {
                List<UsuarioTbModel> listaUsuarios = new List<UsuarioTbModel>();
                List<EquipeUsuarioModel> listaEquipeUsuario = new List<EquipeUsuarioModel>();

                listaUsuarios = ListaUsuario(0);
                foreach (var item in listaUsuarios)
                {
                    UsuarioTbModel objUsr = new UsuarioTbModel();
                    objUsr = item;
                    listaRetorno.Add(objUsr);
                }
                listaEquipeUsuario = ListaEquipeUsuario(0);

                foreach (var itmUsr in listaUsuarios)
                {
                    foreach (var itmEquiUsr in listaEquipeUsuario)
                    {
                        if (itmUsr.usuCod == itmEquiUsr.usuCod)
                        {
                            listaRetorno.Remove(itmUsr);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }

        public List<EquipeUsuarioModel> ListaEquipeUsuario(int equipCod)
        {
            List<EquipeUsuarioModel> listaRetorno = new List<EquipeUsuarioModel>();
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
                        command.Parameters.AddWithValue("@equipCod", equipCod);

                        command.CommandText = @"
                                                    SELECT 
	                                                    Equ.equipCod, Equ.equipDesc, Equ.equipStatus 
	                                                    , Usr.usuCod, Usr.usuNome, Usr.usuStatus 
	                                                    , Perf.perfCod , Perf.perfDesc 
                                                    FROM " + _bdAgenda + @".dbo.UsuarioEquipe AS UsrEqu WITH(NOLOCK)
                                                    INNER JOIN " + _bdAgenda + @".dbo.Equipe AS Equ WITH(NOLOCK) ON Equ.equipCod = UsrEqu.equipCod 
                                                    INNER JOIN " + _bdAgenda + @".dbo.Usuario AS Usr WITH(NOLOCK) ON Usr.usuCod = UsrEqu.usuCod 
                                                    INNER JOIN " + _bdAgenda + @".dbo.Perfil AS Perf WITH(NOLOCK) ON Perf.perfCod = Usr.perfCod 
                                                ";

                        if (equipCod > 0)
                        {
                            command.CommandText += " WHERE Equ.equipCod = @equipCod";
                        }

                        command.CommandText += " ORDER BY Equ.equipDesc, Usr.usuNome";

                        SqlDataReader reader = null;
                        reader = command.ExecuteReader();
                        if (reader != null && reader.HasRows)
                        {
                            EquipeUsuarioModel objItem;
                            while (reader.Read())
                            {
                                objItem = new EquipeUsuarioModel();

                                objItem.equipCod = int.Parse(reader["equipCod"].ToString());
                                objItem.equipDesc = reader["equipDesc"].ToString();
                                objItem.equipStatus = bool.Parse(reader["equipStatus"].ToString());
                                objItem.usuCod = int.Parse(reader["usuCod"].ToString());
                                objItem.usuNome = reader["usuNome"].ToString();
                                objItem.usuStatus = bool.Parse(reader["usuStatus"].ToString());
                                objItem.perfCod = int.Parse(reader["perfCod"].ToString());
                                objItem.perfDesc = reader["perfDesc"].ToString();

                                listaRetorno.Add(objItem);
                            }
                        }

                        reader.Close();

                        //-> Finaliza a transação.
                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        transaction.Rollback();
                        connection.Close();

                        throw;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }

            return listaRetorno;
        }
        #endregion
    }
}
